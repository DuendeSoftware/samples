// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ScimProvisioning.Directory.Pages;

public sealed class IndexModel(ScimClient scim, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<string> Results { get; private set; } = [];

    [BindProperty]
    public string EmployeeId { get; set; } = "";

    [BindProperty]
    public string Department { get; set; } = "";

    [BindProperty]
    public string ActionName { get; set; } = "";

    public async Task OnPostAsync()
    {
        var ct = HttpContext.RequestAborted;
        var employee = EmployeeDirectory.Employees.SingleOrDefault(employee => employee.ExternalId == EmployeeId);
        if (ActionName is "create" or "update" or "deactivate" or "delete" or "member" or "reconcile" && employee is null)
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
                "group" when EmployeeDirectory.Departments.Contains(Department) => [await scim.CreateGroupAsync(Department, ct)],
                "create" => [await scim.CreateUserAsync(employee!, ct)],
                "update" => [await scim.UpdateUserAsync(employee!, false, ct)],
                "deactivate" => [await scim.UpdateUserAsync(employee!, true, ct)],
                "delete" => [await scim.DeleteUserAsync(employee!, ct)],
                "member" => [await scim.AddMemberAsync(employee!, ct)],
                "reconcile" => [await scim.ReconcileAsync(employee!, ct)],
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
            ModelState.AddModelError("", "The request timed out. Reconcile before retrying a write because it may have committed.");
            logger.LogWarning("Directory operation {Operation} timed out.", ActionName);
        }
    }
}
