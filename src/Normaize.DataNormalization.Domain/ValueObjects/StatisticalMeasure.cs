namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing various statistical measures for numerical data
/// </summary>
public record StatisticalMeasure
{
    public double Mean { get; init; }
    public double Median { get; init; }
    public double StandardDeviation { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Q1 { get; init; }
    public double Q2 { get; init; }
    public double Q3 { get; init; }
    public double Skewness { get; init; }
    public double Kurtosis { get; init; }
    public int OutlierCount { get; init; }

    public StatisticalMeasure(
        double mean,
        double median,
        double standardDeviation,
        double min,
        double max,
        double q1,
        double q2,
        double q3,
        double skewness,
        double kurtosis,
        int outlierCount = 0)
    {
        if (double.IsNaN(mean) || double.IsInfinity(mean))
            throw new ArgumentException("Mean must be a valid number", nameof(mean));

        if (double.IsNaN(median) || double.IsInfinity(median))
            throw new ArgumentException("Median must be a valid number", nameof(median));

        if (standardDeviation < 0)
            throw new ArgumentException("Standard deviation cannot be negative", nameof(standardDeviation));

        if (min > max)
            throw new ArgumentException("Minimum value cannot be greater than maximum value");

        if (outlierCount < 0)
            throw new ArgumentException("Outlier count cannot be negative", nameof(outlierCount));

        Mean = mean;
        Median = median;
        StandardDeviation = standardDeviation;
        Min = min;
        Max = max;
        Q1 = q1;
        Q2 = q2;
        Q3 = q3;
        Skewness = skewness;
        Kurtosis = kurtosis;
        OutlierCount = outlierCount;
    }

    /// <summary>
    /// Creates a StatisticalMeasure for empty or invalid data
    /// </summary>
    public static StatisticalMeasure Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Gets the range of the data (Max - Min)
    /// </summary>
    public double Range => Max - Min;

    /// <summary>
    /// Gets the interquartile range (Q3 - Q1)
    /// </summary>
    public double InterquartileRange => Q3 - Q1;

    /// <summary>
    /// Determines if the data has significant skewness (|skewness| > 1)
    /// </summary>
    public bool IsSignificantlySkewed => Math.Abs(Skewness) > 1.0;

    /// <summary>
    /// Determines if the data has high kurtosis (kurtosis > 3)
    /// </summary>
    public bool IsHighKurtosis => Kurtosis > 3.0;
}