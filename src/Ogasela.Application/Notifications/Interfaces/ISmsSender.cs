namespace Ogasela.Application.Notifications.Interfaces;

public interface ISmsSender
{
    /// <summary>
    /// Sends a plain-text SMS. <paramref name="phoneNumber"/> is in the app's normal local
    /// Nigerian format (e.g. "0803...") - the implementation is responsible for converting to
    /// whatever format its provider expects.
    /// </summary>
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken);
}
