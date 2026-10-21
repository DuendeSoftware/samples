// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.Directory.Pages;

public sealed class IndexModel(ScimClient scim, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<string> Results { get; private set; } = [];

    [BindProperty]
    public string? EmployeeId { get; set; }

    [BindProperty]
    public string? Department { get; set; }

    [BindProperty]
    public string ActionName { get; set; } = "";

    public async Task OnPostAsync()
    {
        var ct = HttpContext.RequestAborted;
        var employee = EmployeeDirectory.Employees.SingleOrDefault(employee => employee.ExternalId == EmployeeId);
        if ((ActionName is "create" or "deactivate") && employee is null)
        {
            ModelState.AddModelError("", "Select a known employee.");
            return;
        }

        try
        {
            Results = ActionName switch
            {
                "discover" => await scim.DiscoverAsync(ct),
                "bulk" => await scim.BulkSyncAsync(ct),
                "group" when Department is not null && EmployeeDirectory.Departments.Contains(Department) => [await scim.CreateGroupWithMembersAsync(Department, ct)],
                "create" => [await scim.CreateUserAsync(employee!, ct)],
                "deactivate" => [await scim.DeactivateUserAsync(employee!, ct)],
                _ => throw new ScimOperationException("Select a supported operation and department.")
            };
        }
        catch (ScimOperationException exception)
        {
            ModelState.AddModelError("", exception.Message);
            logger.LogWarning("Directory operation {Operation} failed: {Reason}", ActionName, exception.Message);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The destination or token endpoint could not be reached. Check the Aspire resources.");
            logger.LogWarning("Directory operation {Operation} failed at the HTTP transport or token endpoint.", ActionName);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            ModelState.AddModelError("", "The request timed out. It may have committed; restart the sample before retrying.");
            logger.LogWarning("Directory operation {Operation} timed out.", ActionName);
        }
    }
}
