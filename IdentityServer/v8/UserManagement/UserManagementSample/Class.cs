// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.Storage.Pagination;
using Duende.UserManagement;
using Duende.UserManagement.Authentication;
using Duende.UserManagement.Membership;
using Duende.UserManagement.Profiles;

using StorageQueryResult = Duende.Storage.Querying.QueryResult<Duende.UserManagement.Membership.RoleListItem>;

namespace Duende.IdentityServer.UserManagement;

/// <summary>
/// IProfileService implementation that integrates with Duende UserManagement.
/// </summary>
public class MyProfileService : IProfileService
{
    private readonly IUserAuthenticatorsAdmin _authenticatorsAdmin;
    private readonly IUserProfileAdmin _userProfileAdmin;
    private readonly IMembershipAdmin _membershipAdmin;

    /// <summary>
    /// The logger.
    /// </summary>
    protected ILogger<UserManagementProfileService> Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementProfileService"/> class.
    /// </summary>
    public MyProfileService(
        ILogger<UserManagementProfileService> logger,
        IUserAuthenticatorsAdmin authenticatorsAdmin,
        IUserProfileAdmin userProfileAdmin,
        IMembershipAdmin membershipAdmin
        )
    {
        _authenticatorsAdmin = authenticatorsAdmin;
        _userProfileAdmin = userProfileAdmin;
        _membershipAdmin = membershipAdmin;
        Logger = logger;
    }

    /// <inheritdoc/>
    public virtual async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var activity = Tracing.ServiceActivitySource.StartActivity("UserManagementProfileService.GetProfileData");

        context.LogProfileRequest(Logger);

        var sub = context.Subject.GetSubjectId();
        if (sub == null)
        {
            return;
        }

        if (!UserSubjectId.TryCreate(sub, out var subjectId))
        {
            return;
        }

        await GetProfileDataAsync(context, subjectId, ct);
        context.LogIssuedClaims(Logger);
    }

    /// <summary>
    /// Gets profile data for the given subject ID.
    /// </summary>
    protected virtual async Task GetProfileDataAsync(ProfileDataRequestContext context, UserSubjectId subjectId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(subjectId);

        var profile = await FindUserAsync(subjectId, ct);
        if (profile == null)
        {
            return;
        }

        await GetProfileDataAsync(context, profile, ct);
    }

    /// <summary>
    /// Gets profile data for the given user profile.
    /// </summary>
    protected virtual async Task GetProfileDataAsync(ProfileDataRequestContext context, UserProfile profile, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var claims = new List<Claim>();

        foreach (var attribute in profile.Attributes.Values)
        {
            string value;
            string valueType;

            if (attribute.UntypedValue is bool boolValue)
            {
                value = boolValue ? "true" : "false";
                valueType = ClaimValueTypes.Boolean;
            }
            else
            {
                value = attribute.UntypedValue?.ToString() ?? string.Empty;
                valueType = ClaimValueTypes.String;
            }

            claims.Add(new Claim(attribute.Code.ToString(), value, valueType));
        }

        claims.AddRange(await GetRoleClaimsAsync(context, profile.SubjectId, ct));

        context.AddRequestedClaims(claims);
    }

    /// <inheritdoc/>
    public virtual async Task IsActiveAsync(IsActiveContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var activity = Tracing.ServiceActivitySource.StartActivity("UserManagementProfileService.IsActive");

        var sub = context.Subject?.GetSubjectId();
        if (sub == null)
        {
            context.IsActive = false;
            return;
        }

        if (!UserSubjectId.TryCreate(sub, out var subjectId))
        {
            context.IsActive = false;
            return;
        }

        var authenticators = await FindUserAuthenticatorsAsync(subjectId, ct);
        if (authenticators == null)
        {
            context.IsActive = false;
        }
        else
        {
            context.IsActive = await IsUserActiveAsync(subjectId, authenticators, ct);
        }
    }

    /// <summary>
    /// Determines whether the user is active based on their authenticators.
    /// </summary>
    /// <param name="subjectId">The user's subject ID.</param>
    /// <param name="authenticators">The user's authenticators.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Default implementation always returns true.</returns>
    protected virtual Task<bool> IsUserActiveAsync(UserSubjectId subjectId, UserAuthenticators authenticators, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Loads the user profile by subject ID.
    /// </summary>
    protected virtual Task<UserProfile?> FindUserAsync(UserSubjectId subjectId, CancellationToken ct) =>
        _userProfileAdmin.TryGetAsync(subjectId, ct);

    /// <summary>
    /// Loads the user authenticators for a given subject id. This is used to determine if the user is active.
    /// </summary>
    /// <returns></returns>
    protected virtual Task<UserAuthenticators?> FindUserAuthenticatorsAsync(UserSubjectId subjectId, CancellationToken ct) =>
        _authenticatorsAdmin.TryGetAsync(subjectId, ct);

    /// <summary>
    /// Gets role claims for the specified user.
    /// </summary>
    protected virtual async Task<IEnumerable<Claim>> GetRoleClaimsAsync(ProfileDataRequestContext context, UserSubjectId subjectId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.RequestedClaimTypes.Contains(JwtClaimTypes.Role))
        {
            return [];
        }

        var directTask = GetAllRolesAsync((range, token) => _membershipAdmin.GetDirectRolesAsync(subjectId, range, token), ct);
        var transitiveTask = GetAllRolesAsync((range, token) => _membershipAdmin.GetTransitiveRolesAsync(subjectId, range, token), ct);

        _ = await Task.WhenAll(directTask, transitiveTask);

        var direct = await directTask;
        var transitive = await transitiveTask;

        return direct
            .Concat(transitive)
            .DistinctBy(r => r.Id)
            .Select(r => new Claim(JwtClaimTypes.Role, r.Name.ToString()));
    }

    private static async Task<List<RoleListItem>> GetAllRolesAsync(
        Func<DataRange?, CancellationToken, Task<StorageQueryResult>> fetch, CancellationToken ct)
    {
        var all = new List<RoleListItem>();
        var range = DataRange.FromPage((PageNumber)1);
        StorageQueryResult result;

        do
        {
            result = await fetch(range, ct);
            all.AddRange(result.Items);

            if (result.HasMoreData && result.NextToken != null)
            {
                range = DataRange.FromContinuationToken(result.NextToken);
            }
            else
            {
                break;
            }
        } while (result.HasMoreData);

        return all;
    }
}
