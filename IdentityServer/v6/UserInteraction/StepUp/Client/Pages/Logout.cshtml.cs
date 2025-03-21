// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Client;

public class LogoutModel : PageModel
{
    public SignOutResult OnGet()
    {
        return SignOut("cookie", "oidc");
    }
}
