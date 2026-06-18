// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Duende.Storage.EntityAttributeValue;
using Duende.Storage.Querying;
using Duende.UserManagement;
using Duende.UserManagement.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserManagementSample.Pages.Admin;

public sealed class UsersModel(IUserProfileAdmin profileAdmin) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public List<UserRow> Results { get; set; } = [];

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        QueryRequest request;

        if (string.IsNullOrWhiteSpace(Filter))
        {
            request = QueryRequest.Empty;
        }
        else
        {
            if (!SearchExpression.TryCreate(Filter, out var searchExpression, out var errors))
            {
                ErrorMessage = string.Join(" ", errors);
                return;
            }

            request = QueryRequest.Create(FilterBy.FromSearchExpression(searchExpression));
        }

        var result = await profileAdmin.QueryAsync(request, HttpContext.RequestAborted);

        Results = result.Items.Select(p => new UserRow
        {
            SubjectId = p.SubjectId,
            Email = p.Attributes.TryGetValue(OidcStandardAttributes.Email.Code, out var email)
                ? email.UntypedValue?.ToString()
                : null,
            Name = p.Attributes.TryGetValue(OidcStandardAttributes.Name.Code, out var name)
                ? name.UntypedValue?.ToString()
                : null
        }).ToList();
    }

    public sealed class UserRow
    {
        public required UserSubjectId SubjectId { get; init; }
        public string? Email { get; init; }
        public string? Name { get; init; }
    }
}
