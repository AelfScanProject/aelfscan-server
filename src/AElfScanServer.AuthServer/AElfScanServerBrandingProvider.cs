using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer;

[Dependency(ReplaceServices = true)]
public class AElfScanServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AElfScanServer";
}
