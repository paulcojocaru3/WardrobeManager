namespace WardrobeManager.Application.Outfits.Learning;

// one labeled training row: per-evaluator features + a 0/1 label
public record WeightTrainingSample(Dictionary<string, double> Features, int Label);

// weights the ML trainer returns, keyed by feature name (includes "MlSimilarity")
public record LearnedWeights(Dictionary<string, double> Weights, int NSamples);
