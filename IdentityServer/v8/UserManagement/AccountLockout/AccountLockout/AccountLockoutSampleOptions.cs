// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace AccountLockout;

public sealed class AccountLockoutSampleOptions
{
    public const string SectionName = "Sample";

    [Required]
    public string PublicOrigin { get; init; } = string.Empty;

    [Required]
    public string AdminSubjectId { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; init; } = string.Empty;

    [Required]
    public string AdminName { get; init; } = string.Empty;

    [Required]
    public string UserSubjectId { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string UserEmail { get; init; } = string.Empty;

    [Required]
    public string UserName { get; init; } = string.Empty;
}
