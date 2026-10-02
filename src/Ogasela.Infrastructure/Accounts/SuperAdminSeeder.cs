using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts;

/// <summary>
/// Optionally provisions one SuperAdmin account on startup from configuration (SuperAdminSeed:
/// Name/Phone/Email/Password) - internal roles have no public self-registration path
/// (RegisterUserCommandValidator only allows Buyer/Seller), so this is the intended way to get
/// the very first admin into a fresh environment. Deliberately reads the password from
/// configuration (an env var in practice, via SUPERADMIN_SEED_PASSWORD - see .env.example)
/// rather than ever hardcoding one in source.
///
/// Idempotent and safe to leave wired in permanently:
/// - No-ops if the config section isn't fully populated (Phone/Password both required) - true
///   for every environment except the one deployment meant to own this account.
/// - No-ops if a user already has that phone AND is already SuperAdmin (already seeded).
/// - If the configured phone belongs to an existing non-SuperAdmin account (most likely the
///   intended admin's own prior Buyer/Seller signup, since Phone is globally unique), that
///   account is promoted in place - Role/Name/Email/Password updated - rather than either
///   failing on the Phone unique constraint by inserting a second row, or silently skipping.
/// - If the configured email belongs to a *different* account than the configured phone, this
///   skips rather than guessing which of the two conflicting accounts should win.
/// </summary>
public enum SuperAdminSeedOutcome
{
    NotConfigured,
    Created,
    PromotedExistingAccount,
    AlreadySuperAdmin,
    EmailBelongsToAnotherAccount
}

public static class SuperAdminSeeder
{
    public static async Task<SuperAdminSeedOutcome> SeedAsync(
        IApplicationDbContext dbContext, IPasswordHasher passwordHasher, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var phone = configuration["SuperAdminSeed:Phone"];
        var email = configuration["SuperAdminSeed:Email"];
        var password = configuration["SuperAdminSeed:Password"];
        var name = configuration["SuperAdminSeed:Name"];

        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            return SuperAdminSeedOutcome.NotConfigured;
        }

        var existingByPhone = await dbContext.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);

        if (existingByPhone is not null)
        {
            if (existingByPhone.Role == UserRole.SuperAdmin)
            {
                return SuperAdminSeedOutcome.AlreadySuperAdmin;
            }

            existingByPhone.PromoteToSuperAdmin(name, email, passwordHasher.Hash(password));
            await dbContext.SaveChangesAsync(cancellationToken);
            return SuperAdminSeedOutcome.PromotedExistingAccount;
        }

        var existingByEmail = !string.IsNullOrWhiteSpace(email)
            ? await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            : null;

        if (existingByEmail is not null)
        {
            return SuperAdminSeedOutcome.EmailBelongsToAnotherAccount;
        }

        var user = User.Create(phone, email, passwordHasher.Hash(password), UserRole.SuperAdmin, name);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return SuperAdminSeedOutcome.Created;
    }
}
