using AElfScanServer.Domain.Shared.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AElfScanServer.Permissions;

public class AElfScanServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AElfScanServerPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(AElfScanServerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AElfScanServerResource>(name);
    }
}
