// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using static ScimProvisioning.Directory.EmployeeDirectory;

namespace ScimProvisioning.Directory;

public sealed class ScimClient(HttpClient http)
{
    private const string UserSchema = "urn:ietf:params:scim:schemas:core:2.0:User";
    private const string GroupSchema = "urn:ietf:params:scim:schemas:core:2.0:Group";
    private const string PatchSchema = "urn:ietf:params:scim:api:messages:2.0:PatchOp";
    private const string BulkSchema = "urn:ietf:params:scim:api:messages:2.0:BulkRequest";

    public async Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken ct)
    {
        var results = new List<string>();
        foreach (var resource in new[] { "ServiceProviderConfig", "Schemas", "ResourceTypes" })
        {
            var response = await SendAsync(HttpMethod.Get, resource, null, ct);
            results.Add($"{resource}: HTTP {(int)response.Status}. {response.Summary}");
        }
        return results;
    }

    public async Task<string> CreateUserAsync(Employee employee, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Post, "Users", User(employee), ct);
        return $"Created {employee.UserName}. {response.Summary}";
    }

    public async Task<string> ReconcileAsync(Employee employee, CancellationToken ct)
    {
        var id = await FindUserAsync(employee, ct);
        return id is null
            ? await CreateUserAsync(employee, ct)
            : $"Matched {employee.UserName} by userName. Existing SCIM id: {id}. No overwrite was performed.";
    }

    public async Task<string> UpdateUserAsync(Employee employee, bool deactivate, CancellationToken ct)
    {
        var id = await RequireUserAsync(employee, ct);
        object[] operations = deactivate
            ? [new { op = "replace", path = "active", value = (object)false }]
            : [new { op = "replace", path = "displayName", value = (object)$"{employee.DisplayName} (updated)" }];
        var response = await SendAsync(HttpMethod.Patch, $"Users/{Uri.EscapeDataString(id)}",
            new { schemas = new[] { PatchSchema }, Operations = operations }, ct);
        return $"{(deactivate ? "Stored active=false for" : "Updated")} {employee.UserName}. {response.Summary}";
    }

    public async Task<string> DeleteUserAsync(Employee employee, CancellationToken ct)
    {
        var id = await RequireUserAsync(employee, ct);
        await SendAsync(HttpMethod.Delete, $"Users/{Uri.EscapeDataString(id)}", null, ct);
        return $"Deleted {employee.UserName} (SCIM id {id}).";
    }

    public async Task<string> CreateGroupAsync(string department, CancellationToken ct)
    {
        var response = await SendAsync(HttpMethod.Post, "Groups",
            new { schemas = new[] { GroupSchema }, displayName = department, externalId = $"department-{department}" }, ct);
        return $"Created group {department}. {response.Summary}";
    }

    public async Task<string> AddMemberAsync(Employee employee, CancellationToken ct)
    {
        var userId = await RequireUserAsync(employee, ct);
        var filter = Uri.EscapeDataString($"displayName eq {JsonSerializer.Serialize(employee.Department)}");
        var response = await SendAsync(HttpMethod.Get, $"Groups?filter={filter}", null, ct);
        var group = SingleMatch(response.Body, $"department {employee.Department}");
        if (group is null)
        {
            throw new ScimOperationException("Create the employee's department group first.");
        }

        var groupId = RequiredId(group.Value);
        var result = await SendAsync(HttpMethod.Patch, $"Groups/{Uri.EscapeDataString(groupId)}", new
        {
            schemas = new[] { PatchSchema },
            Operations = new[] { new { op = "add", path = "members", value = new[] { new { value = userId } } } }
        }, ct);
        return $"Added {employee.UserName} to {employee.Department}. {result.Summary}";
    }

    public async Task<IReadOnlyList<string>> BulkSyncAsync(CancellationToken ct)
    {
        var employees = Employees.Where(employee => employee.Department == "Research").ToArray();
        var operations = employees.Select((employee, index) => (object)new
        {
            method = "POST", path = "/Users", bulkId = $"employee{index}", data = User(employee)
        }).ToList();
        operations.Add(new
        {
            method = "POST", path = "/Groups", bulkId = "research",
            data = new
            {
                schemas = new[] { GroupSchema }, displayName = "Research", externalId = "department-Research",
                members = employees.Select((_, index) => new { value = $"bulkId:employee{index}" }).ToArray()
            }
        });
        var response = await SendAsync(HttpMethod.Post, "Bulk",
            new { schemas = new[] { BulkSchema }, failOnErrors = 1, Operations = operations }, ct);
        if (response.Body is not { } body || !body.TryGetProperty("Operations", out var results))
        {
            throw new ScimOperationException("Bulk returned no operation results.");
        }

        var summaries = results.EnumerateArray().Select(operation =>
            $"Bulk id {Text(operation, "bulkId")}: HTTP {Text(operation, "status")}.").ToArray();
        var failed = results.EnumerateArray().Any(operation =>
            !int.TryParse(Text(operation, "status"), out var status) || status is < 200 or >= 300);
        if (failed || summaries.Length != operations.Count)
        {
            throw new ScimOperationException($"Bulk did not fully succeed. Earlier operations may have committed; reconcile before retrying. {string.Join(" ", summaries)}");
        }
        return summaries;
    }

    private async Task<string> RequireUserAsync(Employee employee, CancellationToken ct) =>
        await FindUserAsync(employee, ct) ?? throw new ScimOperationException("Provision or reconcile this employee first.");

    private async Task<string?> FindUserAsync(Employee employee, CancellationToken ct)
    {
        var filter = Uri.EscapeDataString($"userName eq {JsonSerializer.Serialize(employee.UserName)}");
        var response = await SendAsync(HttpMethod.Get, $"Users?filter={filter}", null, ct);
        var match = SingleMatch(response.Body, employee.UserName);
        return match is null ? null : RequiredId(match.Value);
    }

    private static JsonElement? SingleMatch(JsonElement? body, string description)
    {
        if (body is not { } value || !value.TryGetProperty("totalResults", out var total)
            || !value.TryGetProperty("Resources", out var resources))
        {
            throw new ScimOperationException("SCIM returned an invalid list response.");
        }
        return total.GetInt32() switch
        {
            0 => null,
            1 when resources.GetArrayLength() == 1 => resources[0],
            _ => throw new ScimOperationException($"The filter for {description} did not return exactly one unambiguous match.")
        };
    }

    private static object User(Employee employee) => new
    {
        schemas = new[] { UserSchema }, userName = employee.UserName, externalId = employee.ExternalId,
        displayName = employee.DisplayName, active = true,
        emails = new[] { new { value = employee.UserName, type = "work", primary = true } }
    };

    private async Task<Reply> SendAsync(HttpMethod method, string path, object? payload, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/scim+json");
        }

        using var response = await http.SendAsync(request, ct);
        // Error bodies can contain user input or sensitive data. Display only status and SCIM error type.
        JsonElement? body = null;
        if (response.StatusCode != HttpStatusCode.NoContent
            && response.Content.Headers.ContentType?.MediaType is "application/scim+json" or "application/json")
        {
            try
            {
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                body = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                throw new ScimOperationException($"SCIM returned invalid JSON (HTTP {(int)response.StatusCode}).");
            }
        }
        if (!response.IsSuccessStatusCode)
        {
            var type = body is { } error ? Text(error, "scimType") : "";
            throw new ScimOperationException($"SCIM {method} failed with HTTP {(int)response.StatusCode}{(type.Length > 0 ? $" ({type})" : "")}. Check the destination diagnostics.");
        }
        var summary = body is { } data
            ? $"id: {Text(data, "id")}; list count: {Text(data, "totalResults")}."
            : "No response body.";
        return new Reply(response.StatusCode, body, summary);
    }

    private static string RequiredId(JsonElement value) =>
        Text(value, "id") is { Length: > 0 } id ? id : throw new ScimOperationException("SCIM returned a resource without an id.");

    private static string Text(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.String or JsonValueKind.Number
            ? property.ToString() : "";

    private sealed record Reply(HttpStatusCode Status, JsonElement? Body, string Summary);
}

public sealed class ScimOperationException(string message) : Exception(message);
