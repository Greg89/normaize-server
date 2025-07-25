using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for performing statistical calculations on datasets.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public interface IStatisticalCalculationService
{
    /// <summary>
    /// Generates a data summary for the given dataset.
    /// </summary>
    /// <param name="dataSet">The dataset to analyze</param>
    /// <param name="data">The processed data from the dataset</param>
    /// <returns>Data summary containing basic statistics</returns>
    DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data);

    /// <summary>
    /// Generates a comprehensive statistical summary for the given dataset.
    /// </summary>
    /// <param name="dataSet">The dataset to analyze</param>
    /// <param name="data">The processed data from the dataset</param>
    /// <returns>Statistical summary containing advanced statistics</returns>
    StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data);

    /// <summary>
    /// Calculates the median of a list of numeric values.
    /// </summary>
    /// <param name="data">List of numeric values</param>
    /// <returns>The median value</returns>
    double CalculateMedian(List<double> data);

    /// <summary>
    /// Calculates the standard deviation of a list of numeric values.
    /// </summary>
    /// <param name="data">List of numeric values</param>
    /// <returns>The standard deviation</returns>
    double CalculateStandardDeviation(List<double> data);

    /// <summary>
    /// Calculates a quartile value for a given percentile.
    /// </summary>
    /// <param name="data">List of numeric values</param>
    /// <param name="percentile">Percentile value (0.0 to 1.0)</param>
    /// <returns>The quartile value</returns>
    double CalculateQuartile(List<double> data, double percentile);

    /// <summary>
    /// Calculates the skewness of a list of numeric values.
    /// </summary>
    /// <param name="data">List of numeric values</param>
    /// <returns>The skewness value</returns>
    double CalculateSkewness(List<double> data);

    /// <summary>
    /// Calculates the kurtosis of a list of numeric values.
    /// </summary>
    /// <param name="data">List of numeric values</param>
    /// <returns>The kurtosis value</returns>
    double CalculateKurtosis(List<double> data);

    /// <summary>
    /// Determines the data type of a list of values.
    /// </summary>
    /// <param name="data">List of values to analyze</param>
    /// <returns>The determined data type</returns>
    string DetermineDataType(List<object?> data);

    /// <summary>
    /// Checks if a value is numeric.
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <returns>True if the value is numeric</returns>
    bool IsNumeric(object? value);

    /// <summary>
    /// Checks if a column contains numeric data.
    /// </summary>
    /// <param name="data">List of values in the column</param>
    /// <returns>True if the column contains numeric data</returns>
    bool IsNumericColumn(List<object?> data);

    /// <summary>
    /// Checks if a value is a DateTime.
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <returns>True if the value is a DateTime</returns>
    bool IsDateTime(object? value);

    /// <summary>
    /// Checks if a value is a boolean.
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <returns>True if the value is a boolean</returns>
    bool IsBoolean(object? value);
}