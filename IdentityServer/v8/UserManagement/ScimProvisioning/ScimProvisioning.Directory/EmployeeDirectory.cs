// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ScimProvisioning.Directory;

public static class EmployeeDirectory
{
    public static IReadOnlyList<Employee> Employees { get; } =
    [
        new("employee-100", "alice@example.com", "Alice Example", "Engineering"),
        new("employee-101", "bob@example.com", "Bob Example", "Engineering"),
        new("employee-102", "casey@example.com", "Casey Example", "Research"),
        new("employee-103", "dana@example.com", "Dana Example", "Research")
    ];

    public static IReadOnlyList<string> Departments { get; } = ["Engineering", "Research"];

    public sealed record Employee(string ExternalId, string UserName, string DisplayName, string Department);
}
