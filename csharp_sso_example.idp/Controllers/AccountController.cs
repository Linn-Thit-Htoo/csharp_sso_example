using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace csharp_sso_example.idp.Controllers
{
    public class AccountController : Controller
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        public AccountController(TestUserStore users,
                                 IIdentityServerInteractionService interaction,
                                 IEventService events)
        {
            _users = users;
            _interaction = interaction;
            _events = events;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl) =>
            View(new LoginVm { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (_users.ValidateCredentials(vm.Username, vm.Password))
            {
                var user = _users.FindByUsername(vm.Username);

                // Build claims
                var claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.Subject, user.SubjectId),
                    new Claim(JwtClaimTypes.Name, user.Username)
                };
                claims.AddRange(user.Claims); // keep any extra claims (email, etc.)

                var identity = new ClaimsIdentity(
                    claims,
                    authenticationType: "idsrv",
                    nameType: JwtClaimTypes.Name,
                    roleType: JwtClaimTypes.Role);

                var principal = new ClaimsPrincipal(identity);

                // IMPORTANT: sign in to IdentityServer’s cookie scheme ("idsrv")
                await HttpContext.SignInAsync(
                    IdentityServerConstants.DefaultCookieAuthenticationScheme,
                    principal
                // , new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) }
                );

                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username));

                if (_interaction.IsValidReturnUrl(vm.ReturnUrl))
                    return Redirect(vm.ReturnUrl);
                return Redirect("~/");
            }

            ModelState.AddModelError("", "Invalid credentials");
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            await HttpContext.SignOutAsync();
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
                return Redirect(context.PostLogoutRedirectUri);

            return Redirect("~/");
        }
    }

    public class LoginVm
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
}
