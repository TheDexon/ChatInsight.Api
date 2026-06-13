namespace ChatInsight.Api.Services.Ai;

/// <summary>Простой k-means (Ллойд) над векторами. Векторы лучше нормировать заранее.</summary>
public static class KMeans
{
    public static (int[] Assignments, float[][] Centroids) Cluster(
        IReadOnlyList<float[]> vectors, int k, int maxIter = 25, int seed = 42)
    {
        int n = vectors.Count;
        int dim = vectors[0].Length;
        k = Math.Min(k, n);

        var rnd = new Random(seed);
        var centroids = vectors
            .OrderBy(_ => rnd.Next())
            .Take(k)
            .Select(v => (float[])v.Clone())
            .ToArray();

        var assign = new int[n];

        for (int iter = 0; iter < maxIter; iter++)
        {
            bool changed = false;

            for (int i = 0; i < n; i++)
            {
                int best = 0;
                double bestD = double.MaxValue;
                for (int c = 0; c < k; c++)
                {
                    var d = Dist(vectors[i], centroids[c]);
                    if (d < bestD) { bestD = d; best = c; }
                }
                if (assign[i] != best) { assign[i] = best; changed = true; }
            }

            var sums = new double[k][];
            var counts = new int[k];
            for (int c = 0; c < k; c++) sums[c] = new double[dim];

            for (int i = 0; i < n; i++)
            {
                var c = assign[i];
                counts[c]++;
                var v = vectors[i];
                for (int d = 0; d < dim; d++) sums[c][d] += v[d];
            }

            for (int c = 0; c < k; c++)
            {
                if (counts[c] == 0) continue;
                for (int d = 0; d < dim; d++)
                    centroids[c][d] = (float)(sums[c][d] / counts[c]);
            }

            if (!changed) break;
        }

        return (assign, centroids);
    }

    public static double Dist(float[] a, float[] b)
    {
        double s = 0;
        for (int i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            s += diff * diff;
        }
        return s;
    }

    public static float[] Normalize(float[] v)
    {
        double norm = 0;
        foreach (var x in v) norm += x * x;
        norm = Math.Sqrt(norm);
        if (norm < 1e-8) return v;

        var r = new float[v.Length];
        for (int i = 0; i < v.Length; i++) r[i] = (float)(v[i] / norm);
        return r;
    }
}
