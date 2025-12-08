using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Dtos;
using AutoMapper.Internal.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.Users;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace AElfScanServer.HttpApi.Service;

public interface IUserAppService
{
    Task RegisterAppAuthentication(string appId, string deployKey);

    Task<IdentityUserDto> GetUserInfoAsync();

    Task ResetPasswordAsync(string userName, string newPassword);

    Task<string> GetClientDisplayNameAsync(string clientId);

    Task<UserResp> CreateUser(UserReq req);

    Task ResetAdminPwd();
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserAppService : IdentityUserAppService, IUserAppService
{
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IOpenIddictApplicationManager _applicationManager;

    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;

    // private readonly IOpenIddictScopeRepository _openIddictScopeRepository;
    // private readonly IOrganizationAppService _organizationAppService;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IIdentityRoleRepository _roleRepository;

    public UserAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IIdentityRoleRepository roleRepository,
        ILookupNormalizer lookupNormalizer,
        IOptions<IdentityOptions> identityOptions,
        IOpenIddictApplicationManager applicationManager,
        // IOrganizationAppService organizationAppService,
        IOrganizationUnitRepository organizationUnitRepository,
        IPermissionChecker permissionChecker, IdentityUserManager identityUserManager,
        IOptionsMonitor<GlobalOptions> globalOptions)
        : base(userManager, userRepository, roleRepository, identityOptions, permissionChecker)
    {
        _organizationUnitRepository = organizationUnitRepository;
        _lookupNormalizer = lookupNormalizer;
        _applicationManager = applicationManager;
        // _organizationAppService = organizationAppService;
        _identityUserManager = identityUserManager;
        _roleRepository = roleRepository;
        _globalOptions = globalOptions;
    }


    public async Task ResetAdminPwd()
    {
        var adminUser = await _identityUserManager.FindByNameAsync("admin");
        if (adminUser != null)
        {
            var adminPassword = _globalOptions.CurrentValue.AdminResetPwd;
            var token = await _identityUserManager.GeneratePasswordResetTokenAsync(adminUser);
            var result = await _identityUserManager.ResetPasswordAsync(adminUser, token, adminPassword);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to set admin password: " + result.Errors.Select(e => e.Description)
                    .Aggregate((errors, error) => errors + ", " + error));
            }
        }

        var normalizedRoleName = _lookupNormalizer.NormalizeName("appAdmin");
        var appAdminRole = await _roleRepository.FindByNormalizedNameAsync(normalizedRoleName);

        if (appAdminRole == null)
        {
            appAdminRole = new IdentityRole(Guid.NewGuid(), "appAdmin")
            {
                IsStatic = true,
                IsPublic = true
            };
            await _roleRepository.InsertAsync(appAdminRole);
        }
    }

    public async Task<UserResp> CreateUser(UserReq req)
    {
        var user = new IdentityUser(GuidGenerator.Create(), req.UserName, req.Email);
        var createResult = await UserManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            throw new UserFriendlyException("Failed to create user. " + createResult.Errors.Select(e => e.Description)
                .Aggregate((errors, error) => errors + ", " + error));
        }


        var normalizedRoleName = _lookupNormalizer.NormalizeName("appAdmin");
        var identityUser = await UserManager.FindByIdAsync(user.Id.ToString());
        var appAdminRole = await RoleRepository.FindByNormalizedNameAsync(normalizedRoleName);

        if (appAdminRole != null)
        {
            await UserManager.AddToRoleAsync(identityUser, appAdminRole.Name);
        }

        return new UserResp()
        {
            UserName = req.UserName
        };
    }


    public async Task RegisterAppAuthentication(string appId, string deployKey)
    {
        if (await _applicationManager.FindByClientIdAsync(appId) != null)
        {
            throw new UserFriendlyException("A app with the same ID already exists.");
        }

        await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = appId,
            ClientSecret = deployKey,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            DisplayName = " Apps",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                OpenIddictConstants.Permissions.Prefixes.Scope + "AElfScanServer",
                OpenIddictConstants.Permissions.ResponseTypes.IdToken
            }
        });
    }

    public async Task<IdentityUserDto> GetUserInfoAsync()
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("CurrentUser is null");
        }

        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }

        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(identityUser);
    }

    public async Task<string> GetClientDisplayNameAsync(string clientId)
    {
        var openIddictApplication = await _applicationManager.FindByClientIdAsync(clientId);
        // return openIddictApplication.DisplayName;

        var displayName = (string)openIddictApplication.GetType().GetProperty("DisplayName")
            ?.GetValue(openIddictApplication);

        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return string.Empty;
    }

    public async Task ResetPasswordAsync(string userName, string newPassword)
    {
        if (CurrentUser == null || CurrentUser.Id == null)
        {
            throw new UserFriendlyException("CurrentUser is null");
        }

        if (CurrentUser.UserName != userName)
        {
            throw new UserFriendlyException("Can only reset your own password");
        }

        var identityUser = await UserManager.FindByIdAsync(CurrentUser.Id.ToString());
        if (identityUser == null)
        {
            throw new UserFriendlyException("user not found.");
        }

        var token = await UserManager.GeneratePasswordResetTokenAsync(identityUser);
        var result = await UserManager.ResetPasswordAsync(identityUser, token, newPassword);
        if (!result.Succeeded)
        {
            throw new UserFriendlyException("reset user password failed." + result.Errors.Select(e => e.Description)
                .Aggregate((errors, error) => errors + ", " + error));
        }
    }
}