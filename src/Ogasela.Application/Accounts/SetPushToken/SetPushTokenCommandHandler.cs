using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.SetPushToken;

public sealed class SetPushTokenCommandHandler : IRequestHandler<SetPushTokenCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SetPushTokenCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(SetPushTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AccountErrors.UserNotFound);
        }

        user.SetPushToken(request.Token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
