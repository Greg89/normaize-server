using Normaize.Core.Models;
using Normaize.Core.DTOs;

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
}