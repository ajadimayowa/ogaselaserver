using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RegisterUser;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await _dbContext.Users.AnyAsync(
            u => (request.Phone != null && u.Phone == request.Phone) ||
                 (request.Email != null && u.Email == request.Email),
            cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<RegisterUserResponse>(AccountErrors.UserAlreadyExists);
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Phone, request.Email, passwordHash, request.AccountType);
        _dbContext.Users.Add(user);

        if (request.AccountType == UserRole.Seller)
        {
            var sellerProfile = SellerProfile.Create(user.Id, request.BusinessName!, request.RcNumber, request.Nin);
            _dbContext.SellerProfiles.Add(sellerProfile);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await SendWelcomeEmailAsync(request.Email, request.AccountType, cancellationToken);
        }

        return Result.Success(new RegisterUserResponse(user.Id, user.Role));
    }

    private async Task SendWelcomeEmailAsync(string email, UserRole accountType, CancellationToken cancellationToken)
    {
        try
        {
            var roleNoun = accountType == UserRole.Seller ? "seller" : "buyer";
            var html = BrandedEmailTemplate.Render(
                previewText: "Welcome to Ogasela!",
                heading: "Welcome to Ogasela",
                bodyParagraphs:
                [
                    $"Your {roleNoun} account has been created. You're all set to start {(accountType == UserRole.Seller ? "listing on" : "shopping on")} Ogasela.",
                    "If you didn't create this account, please contact our support team right away."
                ],
                eyebrow: "Welcome");

            await _emailSender.SendAsync(
                new EmailMessage(email, ToName: null, Subject: "Welcome to Ogasela", HtmlBody: html),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Registration itself already succeeded; a failed welcome email shouldn't undo it.
            _logger.LogError(ex, "Failed to send welcome email to a newly registered user");
        }
    }
}
