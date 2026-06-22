using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public sealed class NotificationTests
{
    private static readonly DateTime Created = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsFields_AndStartsUnread()
    {
        var userId = Guid.NewGuid();

        var n = Notification.Create(userId, "WeatherAlert", "Title", "Message", "{}", "dedup-1", Created);

        Assert.Equal(userId, n.UserId);
        Assert.Equal("WeatherAlert", n.Type);
        Assert.Equal("Title", n.Title);
        Assert.Equal("Message", n.Message);
        Assert.Equal("{}", n.Payload);
        Assert.Equal("dedup-1", n.DedupKey);
        Assert.Equal(Created, n.CreatedAt);
        Assert.False(n.IsRead);
        Assert.Null(n.ReadAt);
    }

    [Fact]
    public void MarkRead_FlipsToRead_AndStampsTime()
    {
        var n = Notification.Create(Guid.NewGuid(), "T", "t", "m", null, null, Created);
        var readAt = Created.AddHours(2);

        var changed = n.MarkRead(readAt);

        Assert.True(changed);
        Assert.True(n.IsRead);
        Assert.Equal(readAt, n.ReadAt);
    }

    [Fact]
    public void MarkRead_IsNoOp_WhenAlreadyRead()
    {
        var n = Notification.Create(Guid.NewGuid(), "T", "t", "m", null, null, Created);
        n.MarkRead(Created.AddHours(1));

        var changed = n.MarkRead(Created.AddHours(5));

        Assert.False(changed);
        Assert.Equal(Created.AddHours(1), n.ReadAt); // unchanged
    }
}
