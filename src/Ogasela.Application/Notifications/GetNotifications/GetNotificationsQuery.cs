using MediatR;
using Ogasela.Domain.Notifications;
using Ogasela.Shared;

namespace Ogasela.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedNotificationsResponse>>;

public sealed record NotificationListItem(
    Guid Id, NotificationChannel Channel, string Type, string Payload, NotificationStatus Status, DateTime CreatedAt);

public sealed record PagedNotificationsResponse(IReadOnlyList<NotificationListItem> Items, int Page, int PageSize, int TotalCount);
