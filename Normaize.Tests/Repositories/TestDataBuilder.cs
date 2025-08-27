using Normaize.Core.Models;
using Normaize.Core.DTOs;
using System.Text.Json;

namespace Normaize.Tests.Repositories;

public static class TestDataBuilder
{
    public static class DataSetBuilder
    {
        public static DataSet CreateDataSet(
            int id = 1,
            string name = "Test Dataset",
            string fileName = "test.csv",
            FileType fileType = FileType.CSV,
            long fileSize = 1024,
            string userId = "user1",
            bool isDeleted = false)
        {
            return new DataSet
            {
                Id = id,
                Name = name,
                FileName = fileName,
                FileType = fileType,
                FileSize = fileSize,
                UserId = userId,
                UploadedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedBy = userId,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTime.UtcNow.AddDays(-1) : null,
                DeletedBy = isDeleted ? userId : null
            };
        }

        public static List<DataSet> CreateMultipleDataSets(int count, string userId = "user1")
        {
            var dataSets = new List<DataSet>();
            for (int i = 1; i <= count; i++)
            {
                dataSets.Add(CreateDataSet(
                    id: i,
                    name: $"Test Dataset {i}",
                    fileName: $"test{i}.csv",
                    userId: userId
                ));
            }
            return dataSets;
        }
    }

    public static class AnalysisBuilder
    {
        public static Analysis CreateAnalysis(
            int id = 1,
            int dataSetId = 1,
            AnalysisType type = AnalysisType.Normalization,
            AnalysisStatus status = AnalysisStatus.Completed,
            int? comparisonDataSetId = null,
            string results = "{\"result\": \"test\"}",
            string? errorMessage = null,
            bool isDeleted = false)
        {
            return new Analysis
            {
                Id = id,
                DataSetId = dataSetId,
                Type = type,
                Status = status,
                ComparisonDataSetId = comparisonDataSetId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Results = results,
                ErrorMessage = errorMessage,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTime.UtcNow.AddDays(-1) : null,
                DeletedBy = isDeleted ? "user1" : null
            };
        }

        public static List<Analysis> CreateMultipleAnalyses(int count, int dataSetId = 1)
        {
            var analyses = new List<Analysis>();
            for (int i = 1; i <= count; i++)
            {
                analyses.Add(CreateAnalysis(
                    id: i,
                    dataSetId: dataSetId,
                    type: (AnalysisType)((i - 1) % Enum.GetValues<AnalysisType>().Length),
                    status: (AnalysisStatus)((i - 1) % Enum.GetValues<AnalysisStatus>().Length)
                ));
            }
            return analyses;
        }
    }

    public static class DataSetRowBuilder
    {
        public static DataSetRow CreateDataSetRow(
            int id = 1,
            int dataSetId = 1,
            int rowIndex = 1,
            string data = "{\"column1\": \"value1\", \"column2\": \"value2\"}")
        {
            return new DataSetRow
            {
                Id = id,
                DataSetId = dataSetId,
                RowIndex = rowIndex,
                Data = data,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
        }

        public static List<DataSetRow> CreateMultipleDataSetRows(int count, int dataSetId = 1)
        {
            var rows = new List<DataSetRow>();
            for (int i = 1; i <= count; i++)
            {
                rows.Add(CreateDataSetRow(
                    id: i,
                    dataSetId: dataSetId,
                    rowIndex: i,
                    data: $"{{\"column1\": \"value{i}\", \"column2\": \"value{i + 1}\"}}"
                ));
            }
            return rows;
        }

        public static List<DataSetRow> CreateDataSetRowsWithCustomData(int dataSetId, params string[] dataValues)
        {
            var rows = new List<DataSetRow>();
            for (int i = 0; i < dataValues.Length; i++)
            {
                rows.Add(CreateDataSetRow(
                    id: i + 1,
                    dataSetId: dataSetId,
                    rowIndex: i + 1,
                    data: dataValues[i]
                ));
            }
            return rows;
        }
    }

    // Extension methods for easier test data creation
    public static DataSet CreateDataSet(
        int id = 1,
        string name = "Test Dataset",
        string fileName = "test.csv",
        FileType fileType = FileType.CSV,
        long fileSize = 1024,
        string userId = "user1",
        bool processed = false,
        int rowCount = 0,
        int columnCount = 2,
        bool useSeparateTable = false,
        bool isDeleted = false)
    {
        return new DataSet
        {
            Id = id,
            Name = name,
            FileName = fileName,
            FileType = fileType,
            FileSize = fileSize,
            UserId = userId,
            UploadedAt = DateTime.UtcNow.AddDays(-1),
            LastModifiedAt = DateTime.UtcNow.AddDays(-1),
            LastModifiedBy = userId,
            IsProcessed = processed,
            RowCount = rowCount,
            ColumnCount = columnCount,
            UseSeparateTable = useSeparateTable,
            Schema = processed ? "{\"Name\": \"string\", \"Age\": \"int\"}" : null,
            ProcessedData = processed ? "[{\"Name\": \"John\", \"Age\": 30}]" : null,
            PreviewData = processed ? "[{\"Name\": \"John\", \"Age\": 30}]" : null,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow.AddDays(-1) : null,
            DeletedBy = isDeleted ? userId : null
        };
    }

    public static List<DataSetRow> CreateDataSetRows(int dataSetId, int count)
    {
        var rows = new List<DataSetRow>();
        for (int i = 1; i <= count; i++)
        {
            rows.Add(new DataSetRow
            {
                Id = i,
                DataSetId = dataSetId,
                RowIndex = i,
                Data = $"[\"Name{i}\", {i}]",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }
        return rows;
    }

    public static List<DataSetRow> CreateDataSetRowsWithDuplicates(int dataSetId, int count)
    {
        var rows = new List<DataSetRow>();
        for (int i = 1; i <= count; i++)
        {
            // Create some duplicate data - only 2 columns to match schema: Name, Age
            var value = i <= (count + 1) / 2 ? "Duplicate" : $"Unique{i}";
            var age = i <= (count + 1) / 2 ? 25 : i; // Same age for duplicates, different for unique
            rows.Add(new DataSetRow
            {
                Id = i,
                DataSetId = dataSetId,
                RowIndex = i,
                Data = $"[\"{value}\", {age}]",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }
        return rows;
    }

    public static List<DataSetRow> CreateDataSetRowsWithCaseVariations(int dataSetId, int count)
    {
        var rows = new List<DataSetRow>();
        for (int i = 1; i <= count; i++)
        {
            var name = i % 2 == 0 ? "John" : "john"; // Case variations
            rows.Add(new DataSetRow
            {
                Id = i,
                DataSetId = dataSetId,
                RowIndex = i,
                Data = $"[\"{name}\", {i}]",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }
        return rows;
    }

    public static List<DataSetRow> CreateDataSetRowsWithMalformedData(int dataSetId, int count)
    {
        var rows = new List<DataSetRow>();
        for (int i = 1; i <= count; i++)
        {
            // Some rows with malformed JSON
            var data = i % 3 == 0 ? "invalid json" : $"[\"Name{i}\", {i}]";
            rows.Add(new DataSetRow
            {
                Id = i,
                DataSetId = dataSetId,
                RowIndex = i,
                Data = data,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            });
        }
        return rows;
    }

    public static DataNormalizationJob CreateDataNormalizationJob(
        string jobId = "job1",
        string userId = "user1",
        int dataSetId = 1,
        NormalizationJobStatus status = NormalizationJobStatus.Queued,
        int priority = 1)
    {
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        return new DataNormalizationJob
        {
            Id = jobId,
            DataSetId = dataSetId,
            UserId = userId,
            OperationType = "RemoveDuplicateRows",
            OperationParameters = JsonSerializer.Serialize(request),
            Status = status,
            Priority = priority,
            SubmittedAt = DateTime.UtcNow.AddMinutes(-10),
            ProgressPercentage = status == NormalizationJobStatus.Queued ? 0 : 50,
            RetryCount = 0,
            MaxRetries = 3,
            LastModifiedAt = DateTime.UtcNow.AddMinutes(-10),
            LastModifiedBy = userId
        };
    }

    public static List<DataNormalizationJob> CreateMultipleDataNormalizationJobs(int count, string userId = "user1")
    {
        var jobs = new List<DataNormalizationJob>();
        for (int i = 1; i <= count; i++)
        {
            jobs.Add(CreateDataNormalizationJob(
                jobId: $"job{i}",
                userId: userId,
                dataSetId: i,
                status: (NormalizationJobStatus)((i - 1) % Enum.GetValues<NormalizationJobStatus>().Length)
            ));
        }
        return jobs;
    }
}