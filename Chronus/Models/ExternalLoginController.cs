using Chronus.Data;
using Chronus.Models.MainModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("ExternalLogin")]
public class ExternalLoginController : Controller
{
    private readonly UserManager<Employee> _userManager;
    private readonly SignInManager<Employee> _signInManager;
    private readonly IDbContextFactory<ChronusDbContext> _dbFactory;

    public ExternalLoginController(
        UserManager<Employee> userManager,
        SignInManager<Employee> signInManager,
        IDbContextFactory<ChronusDbContext> dbFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbFactory = dbFactory;
    }

    [HttpGet("SignInWithGoogle")]
    public IActionResult SignInWithGoogle()
    {
        var redirectUrl = Url.Action(nameof(GoogleResponse), "ExternalLogin");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
        properties.IsPersistent = true; // 🔑 Forțăm persistența de la primul pas
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Redirect("/employees/login?error=GoogleAuthFailed");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Google";
        var surname = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "User";

        if (string.IsNullOrEmpty(email))
        {
            return Redirect("/employees/login?error=EmailNotFound");
        }

        using var db = await _dbFactory.CreateDbContextAsync();
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new Employee
            {
                UserName = email,
                Email = email,
                FirstName = givenName,
                LastName = surname,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                DailyTargetMinutes = 480
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Redirect("/employees/login?error=UserCreationFailed");
            }
        }

        // Asigurăm legătura de login extern dacă nu există
        var existingLogins = await _userManager.GetLoginsAsync(user);
        if (!existingLogins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
        {
            await _userManager.AddLoginAsync(user, info);
        }

        // 🔑 Aici facem Sign-In explicit cu PERSISTENCE TRUE, exact ca în celălalt proiect!
        await _signInManager.SignInAsync(user, isPersistent: true);

        return LocalRedirect("/");
    }
}