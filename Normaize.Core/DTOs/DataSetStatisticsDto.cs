namespace Normaize.Core.DTOs;

public class DataSetStatisticsDto
{
    public int TotalCount { get; set; }
    public long TotalSize { get; set; }
    public IEnumerable<DataSetDto> RecentlyModified { get; set; } = new List<DataSetDto>();
} 