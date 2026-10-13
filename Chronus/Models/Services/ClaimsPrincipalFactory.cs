using Chronus.Data;
using Chronus.Models.MainModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<Employee, Role>
{
    private readonly IDbContextFactory<ChronusDbContext> _dbFactory;

    public CustomUserClaimsPrincipalFactory(
        UserManager<Employee> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<ChronusDbContext> dbFactory)
        : base(userManager, roleManager, optionsAccessor)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Employee user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Verificăm dacă angajatul are un RoleId setat în coloana din tabelă
        if (user.RoleId != null)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var role = await db.Roles.FindAsync(user.RoleId);

            if (role != null && !string.IsNullOrEmpty(role.Name))
            {
                // Adăugăm rolul ca și Claim pentru ca [Authorize(Roles = "Manager,Admin")] să funcționeze
                if (!identity.HasClaim(ClaimTypes.Role, role.Name))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
                }
            }
        }

        return identity;
    }
}