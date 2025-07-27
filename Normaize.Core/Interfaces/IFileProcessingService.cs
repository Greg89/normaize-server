using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for processing different file types and extracting data.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public interface IFileProcessingService
{
    /// <summary>
    /// Processes a file based on its type and creates a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the file to process</param>
    /// <param name="fileType">The type of file to process</param>
    /// <returns>The processed DataSet</returns>
    Task<DataSet> ProcessFileAsync(string filePath, string fileType);

    /// <summary>
    /// Processes a CSV file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the CSV file</param>
    /// <param name="dataSet">The DataSet to populate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when processing is done</returns>
    Task ProcessCsvFileAsync(string filePath, DataSet dataSet, IOperationContext context);

    /// <summary>
    /// Processes a JSON file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the JSON file</param>
    /// <param name="dataSet">The DataSet to populate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when processing is done</returns>
    Task ProcessJsonFileAsync(string filePath, DataSet dataSet, IOperationContext context);

    /// <summary>
    /// Processes an Excel file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the Excel file</param>
    /// <param name="dataSet">The DataSet to populate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when processing is done</returns>
    Task ProcessExcelFileAsync(string filePath, DataSet dataSet, IOperationContext context);

    /// <summary>
    /// Processes an XML file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the XML file</param>
    /// <param name="dataSet">The DataSet to populate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when processing is done</returns>
    Task ProcessXmlFileAsync(string filePath, DataSet dataSet, IOperationContext context);

    /// <summary>
    /// Processes a text file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the text file</param>
    /// <param name="dataSet">The DataSet to populate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when processing is done</returns>
    Task ProcessTextFileAsync(string filePath, DataSet dataSet, IOperationContext context);

    /// <summary>
    /// Finalizes a DataSet after processing, including hash generation and storage strategy.
    /// </summary>
    /// <param name="filePath">The path to the processed file</param>
    /// <param name="dataSet">The DataSet to finalize</param>
    /// <returns>Task that completes when finalization is done</returns>
    Task FinalizeDataSetAsync(string filePath, DataSet dataSet);

    /// <summary>
    /// Determines if a DataSet should use a separate table based on size and configuration.
    /// </summary>
    /// <param name="dataSet">The DataSet to evaluate</param>
    /// <returns>True if the DataSet should use a separate table</returns>
    bool ShouldUseSeparateTable(DataSet dataSet);

    /// <summary>
    /// Handles processing errors and updates the DataSet accordingly.
    /// </summary>
    /// <param name="filePath">The path to the file being processed</param>
    /// <param name="fileType">The type of file being processed</param>
    /// <param name="dataSet">The DataSet being processed</param>
    /// <param name="context">Operation context for logging</param>
    /// <param name="ex">The exception that occurred</param>
    void HandleProcessingError(string filePath, string fileType, DataSet dataSet, IOperationContext context, Exception ex);
}