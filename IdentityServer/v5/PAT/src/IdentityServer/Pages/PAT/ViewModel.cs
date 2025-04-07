// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace IdentityServerHost.Pages.PAT
{
    public class ViewModel
    {
        public int LifetimeDays { get; set; } = 365;
        public bool IsReferenceToken { get; set; } = true;

        public bool ForApi1 { get; set; } = true;
        public bool ForApi2 { get; set; }
    }
}
