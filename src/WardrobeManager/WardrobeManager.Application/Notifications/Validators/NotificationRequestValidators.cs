using FluentValidation;
using WardrobeManager.Application.Notifications.Commands;
using WardrobeManager.Application.Notifications.Queries;

namespace WardrobeManager.Application.Notifications.Validators;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public sealed class GetUnreadCountQueryValidator : AbstractValidator<GetUnreadCountQuery>
{
    public GetUnreadCountQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}

public sealed class MarkAllNotificationsReadCommandValidator : AbstractValidator<MarkAllNotificationsReadCommand>
{
    public MarkAllNotificationsReadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
