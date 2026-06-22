namespace WardrobeManager.Application.Abstractions;

// canonical ordering for an unordered item pair, so each pair maps to exactly one key/row.
public static class ItemPair
{
    public static (Guid, Guid) Canonical(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
