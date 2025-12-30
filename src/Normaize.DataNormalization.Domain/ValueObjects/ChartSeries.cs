namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Represents a single data series in a chart.
/// </summary>
public class ChartSeries
{
    public string Name { get; init; }
    public List<object> Data { get; init; }
    public string? Color { get; init; }
    public string? Type { get; init; }

    private ChartSeries(string name, List<object> data, string? color = null, string? type = null)
    {
        Name = name;
        Data = data;
        Color = color;
        Type = type;
    }

    public static ChartSeries Create(string name, List<object> data, string? color = null, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Series name cannot be empty", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(data, nameof(data));

        return new ChartSeries(name, data, color, type);
    }
}
