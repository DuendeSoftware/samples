// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Querying;
using Duende.UserManagement.Membership;
using Duende.UserManagement.Profiles;
using Duende.UserManagement.Scim;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.IdentityServer.Pages;

public sealed class IndexModel(IUserProfileAdmin profiles, IGroupAdmin groups, IMembershipAdmin membership) : PageModel
{
    public List<UserRow> Users { get; } = [];
    public List<GroupRow> Groups { get; } = [];

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        var users = await profiles.QueryAsync(QueryRequest.Empty, ct);
        foreach (var profile in users.Items)
        {
            Users.Add(new UserRow(profile.SubjectId.ToString(),
                Read(profile, DefaultScimUserSchema.UserName.Code),
                Read(profile, DefaultScimUserSchema.DisplayName.Code),
                Read(profile, DefaultScimUserSchema.Active.Code)));
        }

        var result = await groups.QueryAsync(new QueryRequest<GroupFilter, GroupSortField>(), ct);
        foreach (var group in result.Items)
        {
            var members = await membership.GetMembersInGroupAsync(group.Id, null, ct);
            Groups.Add(new GroupRow(group.Id.ToString(), group.Name.ToString(),
                string.Join(", ", members.Items.Select(member => member.SubjectId))));
        }
    }

    private static string Read(UserProfile profile, AttributeCode code) =>
        profile.Attributes.TryGetValue(code, out var value) ? value.UntypedValue?.ToString() ?? "" : "";

    public sealed record UserRow(string Id, string UserName, string DisplayName, string Active);
    public sealed record GroupRow(string Id, string Name, string Members);
}
