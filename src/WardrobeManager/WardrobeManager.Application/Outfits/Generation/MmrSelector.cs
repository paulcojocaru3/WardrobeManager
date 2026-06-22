using System;
using System.Collections.Generic;
using System.Linq;

namespace WardrobeManager.Application.Outfits.Generation;

// maximum Marginal Relevance: greedily picks items that are both relevant AND visually distinct from those
public static class MmrSelector
{
    public static List<T> Select<T>(
        IReadOnlyList<(T Item, double Relevance, float[]? Embedding)> candidates,
        int count,
        double lambda = 0.7)
    {
        if (candidates.Count == 0 || count <= 0) return new List<T>();
        if (count >= candidates.Count && candidates.All(c => c.Embedding == null))
        {
            // nothing to diversify on — just return by relevance.
            return candidates.OrderByDescending(c => c.Relevance).Select(c => c.Item).ToList();
        }

        lambda = Math.Clamp(lambda, 0.0, 1.0);

        // min-max normalize relevance so it shares the [0,1] scale of cosine similarity.
        double min = candidates.Min(c => c.Relevance);
        double max = candidates.Max(c => c.Relevance);
        double range = max - min;
        double Rel(int i) => range > 1e-9 ? (candidates[i].Relevance - min) / range : 1.0;

        var remaining = new List<int>(Enumerable.Range(0, candidates.Count));
        var selected = new List<int>(Math.Min(count, candidates.Count));

        // seed with the most relevant item.
        int first = remaining.OrderByDescending(Rel).First();
        selected.Add(first);
        remaining.Remove(first);

        while (selected.Count < count && remaining.Count > 0)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;

            foreach (var i in remaining)
            {
                double maxSim = 0.0;
                foreach (var j in selected)
                {
                    var sim = VectorSimilarity.Cosine(candidates[i].Embedding, candidates[j].Embedding);
                    if (sim > maxSim) maxSim = sim;
                }

                double mmr = lambda * Rel(i) - (1 - lambda) * maxSim;
                if (mmr > bestScore)
                {
                    bestScore = mmr;
                    best = i;
                }
            }

            selected.Add(best);
            remaining.Remove(best);
        }

        return selected.Select(i => candidates[i].Item).ToList();
    }

}
