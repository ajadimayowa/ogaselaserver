using MediatR;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Moderation;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.ReportUser;

public sealed class ReportUserCommandHandler : IRequestHandler<ReportUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public ReportUserCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<Guid>> Handle(ReportUserCommand request, CancellationToken cancellationToken)
    {
        var reporterId = _currentUser.UserId!.Value;

        if (reporterId == request.TargetUserId)
        {
            return Result.Failure<Guid>(MessagingErrors.CannotReportSelf);
        }

        var report = Report.Create(
            reporterId, ReportTargetType.User, request.TargetUserId, request.Reason, _dateTime.UtcNow);

        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(report.Id);
    }
}
