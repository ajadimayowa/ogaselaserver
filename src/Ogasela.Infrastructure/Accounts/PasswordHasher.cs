using Microsoft.AspNetCore.Identity;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;

namespace Ogasela.Infrastructure.Accounts;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(user: null!, password);

    public bool Verify(string password, string hashedPassword) =>
        _hasher.VerifyHashedPassword(user: null!, hashedPassword, password) != PasswordVerificationResult.Failed;
}
