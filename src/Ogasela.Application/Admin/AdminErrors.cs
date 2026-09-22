using Ogasela.Shared;

namespace Ogasela.Application.Admin;

public static class AdminErrors
{
    public static readonly Error UserNotFound = new(
        "Admin.UserNotFound", "The user could not be found.");
}
