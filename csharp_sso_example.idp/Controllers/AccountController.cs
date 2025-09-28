using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using csharp_sso_example.idp.Models;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace csharp_sso_example.idp.Controllers
{
    public class AccountController : Controller
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        public AccountController(
            TestUserStore users,
            IIdentityServerInteractionService interaction,
            IEventService events
        )
        {
            _users = users;
            _interaction = interaction;
            _events = events;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl) => View(new LoginRequestModel { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestModel requestModel)
        {
            if (_users.ValidateCredentials(requestModel.Username, requestModel.Password))
            {
                var user = _users.FindByUsername(requestModel.Username);

                var claims = new List<Claim>
                {
                    new Claim(JwtClaimTypes.Subject, user.SubjectId),
                    new Claim(JwtClaimTypes.Name, user.Username),
                };
                claims.AddRange(user.Claims);

                var identity = new ClaimsIdentity(
                    claims,
                    authenticationType: "idsrv",
                    nameType: JwtClaimTypes.Name,
                    roleType: JwtClaimTypes.Role
                );

                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    IdentityServerConstants.DefaultCookieAuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = System.DateTimeOffset.UtcNow.AddHours(8),
                    }
                );

                await _events.RaiseAsync(
                    new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username)
                );

                if (_interaction.IsValidReturnUrl(requestModel.ReturnUrl))
                    return Redirect(requestModel.ReturnUrl);
                return Redirect("~/");
            }

            ModelState.AddModelError("", "Invalid credentials");
            return View(requestModel);
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
}
