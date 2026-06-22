namespace WardrobeManager.Application.Abstractions;

public interface IApplicationDbInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}
