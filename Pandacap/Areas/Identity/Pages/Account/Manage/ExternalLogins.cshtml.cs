// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Areas.Identity.Pages.Account.Manage
{
    public class ExternalLoginsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly PandacapDbContext _context;

        public ExternalLoginsModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IUserStore<IdentityUser> userStore,
            PandacapDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> OtherLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool ShowRemoveButton { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            CurrentLogins = await _userManager.GetLoginsAsync(user);
            OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();

            string passwordHash = null;
            if (_userStore is IUserPasswordStore<IdentityUser> userPasswordStore)
            {
                passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
            }

            ShowRemoveButton = passwordHash != null || CurrentLogins.Count > 1;
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await UpdateTokensAsync(loginProvider, null);

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not removed.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "The external login was removed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Page("./ExternalLogins", pageHandler: "LinkLoginCallback");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var info = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (info == null)
            {
                throw new InvalidOperationException($"Unexpected error occurred loading external login info.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                StatusMessage = "The external login was not added. External logins can only be associated with one account.";
                return RedirectToPage();
            }

            await UpdateTokensAsync(info.LoginProvider, info);

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = "The external login was added.";
            return RedirectToPage();
        }

        private async Task UpdateTokensAsync(string loginProvider, ExternalLoginInfo info)
        {
            if (loginProvider == "DeviantArt")
            {
                if (info != null)
                {
                    var credentials = await _context.DeviantArtCredentials
                        .Where(c => c.Username == info.Principal.Identity.Name)
                        .FirstOrDefaultAsync();
                    if (credentials == null)
                    {
                        credentials = new DeviantArtCredentials
                        {
                            Username = info.Principal.Identity.Name
                        };
                        _context.DeviantArtCredentials.Add(credentials);
                    }
                    credentials.AccessToken = info.AuthenticationTokens
                        .Where(t => t.Name == "access_token")
                        .Select(t => t.Value)
                        .Single();
                    credentials.RefreshToken = info.AuthenticationTokens
                        .Where(t => t.Name == "refresh_token")
                        .Select(t => t.Value)
                        .Single();
                }
                else
                {
                    _context.RemoveRange(_context.DeviantArtCredentials);
                }
                await _context.SaveChangesAsync();
            }
            else if (loginProvider == "Reddit")
            {
                if (info != null)
                {
                    var credentials = await _context.RedditCredentials
                        .Where(c => c.Username == info.Principal.Identity.Name)
                        .FirstOrDefaultAsync();
                    if (credentials == null)
                    {
                        credentials = new RedditCredentials
                        {
                            Username = info.Principal.Identity.Name
                        };
                        _context.RedditCredentials.Add(credentials);
                    }
                    credentials.AccessToken = info.AuthenticationTokens
                        .Where(t => t.Name == "access_token")
                        .Select(t => t.Value)
                        .Single();
                    credentials.RefreshToken = info.AuthenticationTokens
                        .Where(t => t.Name == "refresh_token")
                        .Select(t => t.Value)
                        .Single();
                }
                else
                {
                    _context.RemoveRange(_context.RedditCredentials);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
