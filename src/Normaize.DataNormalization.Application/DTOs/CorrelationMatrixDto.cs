namespace Normaize.DataNormalization.Application.DTOs;

/// <summary>
/// Correlation matrix DTO for numeric columns
/// </summary>
public class CorrelationMatrixDto
{
    /// <summary>
    /// Dataset identifier
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Dataset name
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Column names in order
    /// </summary>
    public List<string> ColumnNames { get; set; } = new();

    /// <summary>
    /// Correlation matrix values (square matrix)
    /// </summary>
    public List<List<double>> Matrix { get; set; } = new();

    /// <summary>
    /// When the matrix was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Number of observations used in correlation calculation
    /// </summary>
    public int ObservationCount { get; set; }

    /// <summary>
    /// Correlation pairs with strong relationships
    /// </summary>
    public List<CorrelationPairDto> StrongCorrelations { get; set; } = new();
}

/// <summary>
/// Correlation pair information
/// </summary>
public class CorrelationPairDto
{
    /// <summary>
    /// First column name
    /// </summary>
    public string Column1 { get; set; } = string.Empty;

    /// <summary>
    /// Second column name
    /// </summary>
    public string Column2 { get; set; } = string.Empty;

    /// <summary>
    /// Correlation coefficient
    /// </summary>
    public double Correlation { get; set; }

    /// <summary>
    /// Strength description (e.g., "Strong Positive", "Moderate Negative")
    /// </summary>
    public string Strength { get; set; } = string.Empty;
}