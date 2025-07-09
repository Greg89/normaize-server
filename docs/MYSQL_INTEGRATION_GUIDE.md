# MySQL Integration Guide for Normaize

## Why MySQL is Perfect for This Approach

### 1. **Native JSON Support**
MySQL 5.7+ provides excellent JSON data type support:
- **JSON validation**: Ensures data integrity
- **JSON indexing**: Fast queries on JSON fields
- **JSON functions**: Powerful querying capabilities
- **Storage efficiency**: Optimized JSON storage

### 2. **Performance Benefits**
- **LONGTEXT fields**: Handle large datasets efficiently
- **Indexing**: Fast lookups on any column combination
- **Bulk operations**: Efficient batch inserts
- **Connection pooling**: Optimized database connections

### 3. **Cost-Effectiveness**
- **Open source**: No licensing costs
- **Scalable**: Handles growth without expensive upgrades
- **Cloud options**: Available on all major cloud providers
- **Resource efficient**: Lower memory and CPU requirements

## Database Schema in Detail

### DataSet Table Structure
```sql
CREATE TABLE DataSets (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    FileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(500) NOT NULL,
    FileType VARCHAR(50) NOT NULL,
    FileSize BIGINT NOT NULL,
    UploadedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Schema information (JSON array of column names)
    Schema JSON, -- ["Name", "Age", "City", "Email"]
    
    -- Statistics
    RowCount INT NOT NULL,
    ColumnCount INT NOT NULL,
    
    -- Storage strategy
    UseSeparateTable BOOLEAN DEFAULT FALSE,
    StorageProvider VARCHAR(50) DEFAULT 'Local',
    
    -- Data storage (for small datasets)
    ProcessedData JSON, -- Array of row objects
    PreviewData JSON,   -- First 10 rows for UI
    
    -- Metadata
    DataHash VARCHAR(255),  -- SHA256 hash for change detection
    ProcessingErrors TEXT,
    IsProcessed BOOLEAN DEFAULT FALSE,
    ProcessedAt TIMESTAMP NULL
);
```

### DataSetRow Table Structure (for large datasets)
```sql
CREATE TABLE DataSetRows (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    DataSetId INT NOT NULL,
    RowIndex INT NOT NULL,
    Data JSON NOT NULL, -- Single row as JSON object
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE,
    INDEX idx_dataset_row (DataSetId, RowIndex)
);
```

## How Data is Stored

### Example 1: Small Dataset (stored in ProcessedData)
```json
{
  "Id": 1,
  "Name": "Sales Data Q1",
  "Schema": ["Product", "Region", "Sales", "Date"],
  "RowCount": 1000,
  "ColumnCount": 4,
  "UseSeparateTable": false,
  "ProcessedData": [
    {"Product": "Widget A", "Region": "North", "Sales": "5000", "Date": "2024-01-15"},
    {"Product": "Widget B", "Region": "South", "Sales": "3000", "Date": "2024-01-16"},
    {"Product": "Widget C", "Region": "East", "Sales": "4000", "Date": "2024-01-17"}
  ],
  "PreviewData": [
    {"Product": "Widget A", "Region": "North", "Sales": "5000", "Date": "2024-01-15"}
  ]
}
```

### Example 2: Large Dataset (stored in DataSetRow table)
```sql
-- DataSet table
INSERT INTO DataSets (Id, Name, Schema, RowCount, ColumnCount, UseSeparateTable) 
VALUES (2, 'Large Dataset', '["ID", "Name", "Value", "Category"]', 50000, 4, true);

-- DataSetRow table (showing first few rows)
INSERT INTO DataSetRows (DataSetId, RowIndex, Data) VALUES
(2, 1, '{"ID": "1", "Name": "Item A", "Value": "100", "Category": "A"}'),
(2, 2, '{"ID": "2", "Name": "Item B", "Value": "200", "Category": "B"}'),
(2, 3, '{"ID": "3", "Name": "Item C", "Value": "150", "Category": "A"}');
```

## MySQL JSON Querying Capabilities

### 1. Find Datasets by Column
```sql
-- Find all datasets containing a "Sales" column
SELECT * FROM DataSets 
WHERE JSON_CONTAINS(Schema, '"Sales"')
ORDER BY UploadedAt DESC;
```

### 2. Search Data Values
```sql
-- Find datasets with specific values in a column
SELECT DISTINCT d.* FROM DataSets d
WHERE d.UseSeparateTable = 0 
AND JSON_EXTRACT(d.ProcessedData, '$[*].Sales') LIKE '%5000%'
ORDER BY d.UploadedAt DESC;
```

### 3. Complex JSON Queries
```sql
-- Find datasets with sales > 3000
SELECT d.* FROM DataSets d
WHERE d.UseSeparateTable = 0 
AND JSON_EXTRACT(d.ProcessedData, '$[*].Sales') > 3000;

-- Count unique values in a column
SELECT 
    JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Category')) as Category,
    COUNT(*) as Count
FROM DataSetRows 
WHERE DataSetId = 2
GROUP BY JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Category'));
```

## Performance Optimizations

### 1. Indexing Strategy
```sql
-- Primary indexes for fast lookups
CREATE INDEX idx_datasets_uploaded_at ON DataSets(UploadedAt);
CREATE INDEX idx_datasets_is_processed ON DataSets(IsProcessed);
CREATE INDEX idx_datasets_use_separate_table ON DataSets(UseSeparateTable);

-- Composite index for DataSetRow queries
CREATE INDEX idx_datasetrow_dataset_row ON DataSetRows(DataSetId, RowIndex);

-- JSON indexes for MySQL 8.0+
CREATE INDEX idx_datasets_schema ON DataSets((CAST(Schema AS CHAR(1000))));
CREATE INDEX idx_datasetrows_data ON DataSetRows((CAST(Data AS CHAR(1000))));
```

### 2. Bulk Operations
```csharp
// Efficient bulk insert for large datasets
public async Task BulkInsertDataRowsAsync(int dataSetId, IEnumerable<DataSetRow> rows)
{
    var batchSize = 1000;
    var batches = rows.Chunk(batchSize);

    foreach (var batch in batches)
    {
        _context.DataSetRows.AddRange(batch);
        await _context.SaveChangesAsync();
    }
}
```

### 3. Connection String Optimizations
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=normaize;User=root;Password=password;CharSet=utf8mb4;AllowLoadLocalInfile=true;Convert Zero Datetime=True;Allow Zero Datetime=True;"
  }
}
```

## Data Retrieval Patterns

### 1. Small Datasets (from ProcessedData)
```csharp
public async Task<List<Dictionary<string, object>>> GetDataSetDataAsync(int dataSetId)
{
    var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
    if (dataSet?.ProcessedData != null)
    {
        return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
    }
    return new List<Dictionary<string, object>>();
}
```

### 2. Large Datasets (from DataSetRow table)
```csharp
public async Task<List<Dictionary<string, object>>> GetDataSetDataAsync(int dataSetId, int skip = 0, int take = 1000)
{
    var rows = await _dataSetRowRepository.GetByDataSetIdAsync(dataSetId, skip, take);
    return rows.Select(row => JsonSerializer.Deserialize<Dictionary<string, object>>(row.Data)).ToList();
}
```

### 3. Pagination for Large Datasets
```csharp
public async Task<PaginatedResult<Dictionary<string, object>>> GetDataSetDataPaginatedAsync(
    int dataSetId, int page = 1, int pageSize = 100)
{
    var skip = (page - 1) * pageSize;
    var rows = await _dataSetRowRepository.GetByDataSetIdAsync(dataSetId, skip, pageSize);
    var totalCount = await _dataSetRowRepository.GetCountByDataSetIdAsync(dataSetId);
    
    var data = rows.Select(row => JsonSerializer.Deserialize<Dictionary<string, object>>(row.Data)).ToList();
    
    return new PaginatedResult<Dictionary<string, object>>
    {
        Data = data,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
    };
}
```

## MySQL vs Other Databases

### MySQL Advantages
| Feature | MySQL | PostgreSQL | SQL Server |
|---------|-------|------------|------------|
| JSON Support | ✅ Native | ✅ Native | ✅ Native |
| Cost | ✅ Free | ✅ Free | ❌ Expensive |
| Performance | ✅ Excellent | ✅ Excellent | ✅ Excellent |
| Cloud Support | ✅ All providers | ✅ All providers | ✅ Azure focus |
| JSON Indexing | ✅ MySQL 8.0+ | ✅ | ✅ |
| Community | ✅ Large | ✅ Large | ✅ Large |

### Why MySQL is Best for Normaize
1. **JSON Performance**: Excellent JSON querying and indexing
2. **Cost**: No licensing fees, perfect for startups
3. **Flexibility**: Handles any schema without predefined tables
4. **Scalability**: Can grow from small to large datasets
5. **Ecosystem**: Rich tooling and community support

## Migration from Other Databases

### If You're Currently Using SQL Server
```sql
-- SQL Server JSON syntax
SELECT * FROM DataSets WHERE JSON_VALUE(Schema, '$[0]') = 'Sales'

-- MySQL equivalent
SELECT * FROM DataSets WHERE JSON_EXTRACT(Schema, '$[0]') = 'Sales'
```

### If You're Currently Using PostgreSQL
```sql
-- PostgreSQL JSON syntax
SELECT * FROM DataSets WHERE Schema @> '["Sales"]'

-- MySQL equivalent
SELECT * FROM DataSets WHERE JSON_CONTAINS(Schema, '"Sales"')
```

## Monitoring and Maintenance

### 1. Performance Monitoring
```sql
-- Check table sizes
SELECT 
    table_name,
    ROUND(((data_length + index_length) / 1024 / 1024), 2) AS 'Size (MB)'
FROM information_schema.tables 
WHERE table_schema = 'normaize'
ORDER BY (data_length + index_length) DESC;

-- Check JSON field sizes
SELECT 
    Id,
    Name,
    JSON_LENGTH(Schema) as SchemaColumns,
    JSON_LENGTH(ProcessedData) as DataRows
FROM DataSets
WHERE UseSeparateTable = 0;
```

### 2. Cleanup Operations
```sql
-- Clean up old datasets
DELETE FROM DataSets 
WHERE UploadedAt < DATE_SUB(NOW(), INTERVAL 90 DAY);

-- Optimize tables
OPTIMIZE TABLE DataSets;
OPTIMIZE TABLE DataSetRows;
```

### 3. Backup Strategy
```bash
# Full database backup
mysqldump -u root -p normaize > normaize_backup.sql

# Backup with JSON optimization
mysqldump -u root -p --single-transaction --routines --triggers normaize > normaize_full_backup.sql
```

## Conclusion

MySQL is an excellent choice for the Normaize application because:

1. **Native JSON Support**: Perfect for flexible schema storage
2. **Performance**: Fast queries on JSON data with proper indexing
3. **Cost-Effective**: No licensing fees, scales with your needs
4. **Flexibility**: Handles any file structure without schema changes
5. **Ecosystem**: Rich tooling and community support

The hybrid approach with MySQL gives you the best of both worlds - fast database queries for analysis while preserving original files for audit and reprocessing. This is exactly what you need for a data normalization and analysis application! 