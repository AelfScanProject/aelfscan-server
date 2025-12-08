using System;
using System.Linq;
using AElf.EntityMapping.Elasticsearch;
using AElfScanServer.Common;
using AElfScanServer.Domain.Shared.MultiTenancy;
using AElfScanServer.MongoDB;
using AutoResponseWrapper;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;

namespace AElfScanServer.HttpApi.Host;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AElfScanServerApplicationModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(HttpApiModule),
    typeof(AElfScanServerMongoDbModule),
    typeof(AElfScanCommonModule)
)]
public class HttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IdentityBuilder>(builder => { builder.AddDefaultTokenProviders(); });

        // IdentityBuilderExtensions.AddDefaultTokenProviders(context.Services.AddIdentity<IdentityUser, IdentityRole>());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        ConfigureAuthentication(context, configuration);
        ConfigureGraphQl(context, configuration);
        ConfigureCache(context, configuration);
        ConfigureCors(context, configuration);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<HttpApiHostModule>(); });
        context.Services.AddAutoResponseWrapper();

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(HttpApiHostModule).Assembly);
        });
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    {
        // IdentityBuilderExtensions.AddDefaultTokenProviders(context.Services.AddIdentity<IdentityUser, IdentityRole>());
        context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["AuthServer:Authority"];
                options.RequireHttpsMetadata = Convert.ToBoolean(configuration["AuthServer:RequireHttpsMetadata"]);
                options.Audience = "AElfScanServer";
            });
        context.Services.AddAuthorization(options =>
        {
            options.AddPolicy("OnlyAdminAccess", policy =>
                policy.RequireRole("admin"));
        });
    }

    // private void ConfigureAuthentication(ServiceConfigurationContext context, IConfiguration configuration)
    // {
    //     context.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    //         .AddJwtBearer(options =>
    //         {
    //             options.Authority = configuration["AuthServer:Authority"];
    //             options.RequireHttpsMetadata = configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata");
    //             options.Audience = "AElfScanServer";
    //         });
    //
    //     context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
    //     {
    //         options.IsDynamicClaimsEnabled = true;
    //     });
    // }


    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();
        app.UseCorrelationId();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseAuthorization();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "AElfScanServer API");

            var configuration = context.GetConfiguration();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
            options.OAuthClientSecret(configuration["AuthServer:SwaggerClientSecret"]);
            options.OAuthScopes("AElfScanServer");
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseUnitOfWork();
        app.UseConfiguredEndpoints();
    }


    private void ConfigureGraphQl(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    private void ConfigureCache(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        var multiplexer = ConnectionMultiplexer
            .Connect(configuration["Redis:Configuration"]);
        context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AElfScanServer:"; });
    }
}