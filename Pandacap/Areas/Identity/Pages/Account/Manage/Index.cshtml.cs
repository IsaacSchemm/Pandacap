// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Pandacap.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(UserManager<IdentityUser> userManager) : PageModel
    {
        public string Username { get; set; }

        private async Task LoadAsync(IdentityUser user)
        {
            Username = await userManager.GetUserNameAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }
    }
}
