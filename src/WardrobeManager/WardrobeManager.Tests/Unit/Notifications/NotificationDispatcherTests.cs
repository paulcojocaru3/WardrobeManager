using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Notifications;
using WardrobeManager.Application.Notifications.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Notifications;

[Trait("Category", "Unit")]
public sealed class NotificationDispatcherTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationPushGateway _push = Substitute.For<INotificationPushGateway>();
    private readonly Guid _userId = Guid.NewGuid();

    private NotificationDispatcher Sut() => new(_repository, _push, NullLogger<NotificationDispatcher>.Instance);

    [Fact]
    public async Task DispatchAsync_PersistsAndPushes_WhenNew()
    {
        await Sut().DispatchAsync(_userId, "WeatherAlert", "Title", "Body", new { x = 1 }, dedupKey: null);

        await _repository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.UserId == _userId && n.Type == "WeatherAlert" && n.Payload != null),
            Arg.Any<CancellationToken>());
        await _push.Received(1).PushAsync(_userId, Arg.Any<NotificationDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_SerializesNullPayload_AsNull()
    {
        await Sut().DispatchAsync(_userId, "T", "t", "m", payload: null, dedupKey: null);

        await _repository.Received(1).AddAsync(Arg.Is<Notification>(n => n.Payload == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_Skips_WhenDedupKeyAlreadyExists()
    {
        _repository.ExistsByDedupKeyAsync(_userId, "dup", Arg.Any<CancellationToken>()).Returns(true);

        await Sut().DispatchAsync(_userId, "T", "t", "m", null, dedupKey: "dup");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _push.DidNotReceive().PushAsync(Arg.Any<Guid>(), Arg.Any<NotificationDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_SwallowsPushFailure_AfterPersisting()
    {
        _push.PushAsync(Arg.Any<Guid>(), Arg.Any<NotificationDto>(), Arg.Any<CancellationToken>())
             .Returns(_ => throw new InvalidOperationException("hub down"));

        await Sut().DispatchAsync(_userId, "T", "t", "m", null, null); // must not throw

        await _repository.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
