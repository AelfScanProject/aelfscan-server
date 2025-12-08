using AElfScanServer.Domain.Shared;
using AElfScanServer.Domain.Shared.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElfScanServer;

[DependsOn(
    dependedTypes: new[]
    {
        typeof(AElfScanServerDomainSharedModule),
        typeof(AbpAuditLoggingDomainModule),
        typeof(AbpBackgroundJobsDomainModule),
        typeof(AbpFeatureManagementDomainModule),
        typeof(AbpIdentityDomainModule),
        typeof(AbpOpenIddictDomainModule),
        typeof(AbpPermissionManagementDomainIdentityModule),
        typeof(AbpSettingManagementDomainModule),
        typeof(AbpTenantManagementDomainModule),
        // typeof(AElfIndexingElasticsearchModule)
    }
)]
public class AElfScanServerDomainModule : AbpModule
{
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // ConfigureEsIndexCreation();
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Languages.Add(new LanguageInfo("ar", "ar", "العربية"));
            options.Languages.Add(new LanguageInfo("cs", "cs", "Čeština"));
            options.Languages.Add(new LanguageInfo("en", "en", "English"));
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)"));
            options.Languages.Add(new LanguageInfo("hu", "hu", "Magyar"));
            options.Languages.Add(new LanguageInfo("hr", "hr", "Croatian"));
            options.Languages.Add(new LanguageInfo("pt-BR", "pt-BR", "Português"));
            options.Languages.Add(new LanguageInfo("es", "es", "Español"));
        });

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = MultiTenancyConsts.IsEnabled;
        });
        
        Configure<AbpMultiTenancyOptions>(options => { options.IsEnabled = MultiTenancyConsts.IsEnabled; });
        

    }
    
    // private void ConfigureEsIndexCreation()
    // {
    //     Configure<IndexCreateOption>(x => { x.AddModule(typeof(AElfScanServerDomainModule)); });
    // }
}
