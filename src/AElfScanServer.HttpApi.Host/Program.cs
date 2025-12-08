using System;
using System.IO;
using System.Threading.Tasks;
using AElfScanServer.HttpApi.Host.Extension;
using AElfScanServer.HttpApi.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp.Modularity.PlugIns;

namespace AElfScanServer.HttpApi.Host;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)

#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting BlockChainDataFunction Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("apollo.appsettings.json");
            builder.Configuration.AddJsonFile("secrets.json");
            builder.Services.AddSerilog(loggerConfiguration => {},
                true, writeToProviders: true);
            builder.Host.AddAppSettingsSecretsJson()
                .UseOrleansClient()
                .UseAutofac()
                .UseApollo()
                .UseSerilog();
            
            var combine = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            await builder.AddApplicationAsync<HttpApiHostModule>(options =>
            {
                options.PlugInSources.AddFolder(combine);
            });
  
            string[] files = Directory.GetFiles(combine);
            Log.Information("Plugins path:{0}", combine);
            foreach (var file in files)
            {
                Log.Information("Plugins file name:{0}", file.ToString());
            }

            builder.Services.AddSignalR();
           
            var app = builder.Build();
            app.MapHub<ExploreHub>("api/app/blockchain/explore");

            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}