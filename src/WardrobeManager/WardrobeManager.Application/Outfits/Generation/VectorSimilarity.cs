namespace WardrobeManager.Application.Outfits.Generation;

internal static class VectorSimilarity
{
    public static double Cosine(float[]? a, float[]? b)
    {
        if (a == null || b == null)
        {
            return 0.0;
        }

        var length = Math.Min(a.Length, b.Length);
        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var index = 0; index < length; index++)
        {
            dot += a[index] * (double)b[index];
            normA += a[index] * (double)a[index];
            normB += b[index] * (double)b[index];
        }

        if (normA <= 1e-12 || normB <= 1e-12)
        {
            return 0.0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
