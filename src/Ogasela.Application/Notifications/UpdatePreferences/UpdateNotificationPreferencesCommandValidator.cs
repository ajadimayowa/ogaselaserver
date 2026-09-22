using FluentValidation;

namespace Ogasela.Application.Notifications.UpdatePreferences;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences).NotEmpty();

        RuleForEach(x => x.Preferences).ChildRules(preference =>
        {
            preference.RuleFor(p => p.Category).NotEmpty().MaximumLength(100);
        });
    }
}
