-- MySQL Optimizations for Normaize Database
-- Run this script after your initial migration

-- 1. Update existing tables to use JSON data types
ALTER TABLE DataSets 
MODIFY COLUMN ProcessedData JSON,
MODIFY COLUMN PreviewData JSON,
MODIFY COLUMN Schema JSON,
MODIFY COLUMN ProcessingErrors TEXT,
MODIFY COLUMN DataHash VARCHAR(255),
MODIFY COLUMN UploadedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
MODIFY COLUMN ProcessedAt TIMESTAMP NULL;

ALTER TABLE DataSetRows 
MODIFY COLUMN Data JSON,
MODIFY COLUMN CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE Analyses 
MODIFY COLUMN Configuration JSON,
MODIFY COLUMN Results JSON,
MODIFY COLUMN ErrorMessage TEXT,
MODIFY COLUMN CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
MODIFY COLUMN CompletedAt TIMESTAMP NULL;

-- 2. Add performance indexes
CREATE INDEX idx_datasets_uploaded_at ON DataSets(UploadedAt);
CREATE INDEX idx_datasets_is_processed ON DataSets(IsProcessed);
CREATE INDEX idx_datasets_use_separate_table ON DataSets(UseSeparateTable);
CREATE INDEX idx_datasets_file_size ON DataSets(FileSize);
CREATE INDEX idx_datasets_row_count ON DataSets(RowCount);

CREATE INDEX idx_datasetrows_dataset_row ON DataSetRows(DataSetId, RowIndex);
CREATE INDEX idx_datasetrows_created_at ON DataSetRows(CreatedAt);

CREATE INDEX idx_analyses_dataset_id ON Analyses(DataSetId);
CREATE INDEX idx_analyses_status ON Analyses(Status);
CREATE INDEX idx_analyses_created_at ON Analyses(CreatedAt);

-- 3. Add JSON indexes (MySQL 8.0+)
-- These indexes improve performance for JSON queries
CREATE INDEX idx_datasets_schema ON DataSets((CAST(Schema AS CHAR(1000))));
CREATE INDEX idx_datasetrows_data ON DataSetRows((CAST(Data AS CHAR(1000))));

-- 4. Add foreign key constraints if not already present
ALTER TABLE DataSetRows 
ADD CONSTRAINT fk_datasetrows_dataset 
FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE;

ALTER TABLE Analyses 
ADD CONSTRAINT fk_analyses_dataset 
FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE;

ALTER TABLE Analyses 
ADD CONSTRAINT fk_analyses_comparison_dataset 
FOREIGN KEY (ComparisonDataSetId) REFERENCES DataSets(Id) ON DELETE SET NULL;

-- 5. Optimize table settings for JSON data
ALTER TABLE DataSets ROW_FORMAT=DYNAMIC;
ALTER TABLE DataSetRows ROW_FORMAT=DYNAMIC;
ALTER TABLE Analyses ROW_FORMAT=DYNAMIC;

-- 6. Set proper character set and collation
ALTER DATABASE normaize CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE DataSets CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE DataSetRows CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Analyses CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 7. Create views for common queries
CREATE VIEW v_dataset_summary AS
SELECT 
    Id,
    Name,
    FileName,
    FileType,
    FileSize,
    RowCount,
    ColumnCount,
    UploadedAt,
    IsProcessed,
    UseSeparateTable,
    JSON_LENGTH(Schema) as SchemaColumnCount
FROM DataSets
ORDER BY UploadedAt DESC;

-- 8. Create stored procedures for common operations
DELIMITER //

CREATE PROCEDURE GetDataSetsByColumn(IN column_name VARCHAR(255))
BEGIN
    SELECT * FROM DataSets 
    WHERE JSON_CONTAINS(Schema, JSON_QUOTE(column_name))
    ORDER BY UploadedAt DESC;
END //

CREATE PROCEDURE GetDataSetsByValue(IN column_name VARCHAR(255), IN search_value VARCHAR(255))
BEGIN
    SELECT DISTINCT d.* FROM DataSets d
    WHERE d.UseSeparateTable = 0 
    AND JSON_EXTRACT(d.ProcessedData, CONCAT('$[*].', JSON_QUOTE(column_name))) LIKE CONCAT('%', search_value, '%')
    ORDER BY d.UploadedAt DESC;
END //

CREATE PROCEDURE CleanupOldDataSets(IN days_to_keep INT)
BEGIN
    DECLARE cutoff_date TIMESTAMP;
    SET cutoff_date = DATE_SUB(NOW(), INTERVAL days_to_keep DAY);
    
    DELETE FROM DataSets 
    WHERE UploadedAt < cutoff_date;
    
    SELECT ROW_COUNT() as deleted_count;
END //

DELIMITER ;

-- 9. Create triggers for data integrity
DELIMITER //

CREATE TRIGGER before_dataset_delete
BEFORE DELETE ON DataSets
FOR EACH ROW
BEGIN
    -- Log deletion for audit purposes
    INSERT INTO DataSetAuditLog (DataSetId, Action, ActionDate)
    VALUES (OLD.Id, 'DELETE', NOW());
END //

CREATE TRIGGER after_dataset_insert
AFTER INSERT ON DataSets
FOR EACH ROW
BEGIN
    -- Update statistics
    UPDATE DataSetStatistics 
    SET TotalDataSets = TotalDataSets + 1,
        TotalRows = TotalRows + NEW.RowCount,
        LastUpdated = NOW()
    WHERE Id = 1;
END //

DELIMITER ;

-- 10. Create audit and statistics tables
CREATE TABLE IF NOT EXISTS DataSetAuditLog (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    DataSetId INT,
    Action VARCHAR(50),
    ActionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_audit_dataset (DataSetId),
    INDEX idx_audit_date (ActionDate)
);

CREATE TABLE IF NOT EXISTS DataSetStatistics (
    Id INT PRIMARY KEY DEFAULT 1,
    TotalDataSets INT DEFAULT 0,
    TotalRows BIGINT DEFAULT 0,
    TotalFileSize BIGINT DEFAULT 0,
    LastUpdated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert initial statistics record
INSERT INTO DataSetStatistics (Id, TotalDataSets, TotalRows, TotalFileSize) 
VALUES (1, 0, 0, 0) 
ON DUPLICATE KEY UPDATE Id = Id;

-- 11. Grant necessary permissions (adjust as needed)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON normaize.* TO 'normaize_user'@'localhost';
-- GRANT EXECUTE ON PROCEDURE normaize.* TO 'normaize_user'@'localhost';

-- 12. Optimize MySQL settings for JSON performance
-- Add these to your my.cnf or my.ini file:
/*
[mysqld]
# JSON performance optimizations
innodb_buffer_pool_size = 1G
innodb_log_file_size = 256M
innodb_flush_log_at_trx_commit = 2
innodb_file_per_table = 1

# Character set
character-set-server = utf8mb4
collation-server = utf8mb4_unicode_ci

# Query cache (if using MySQL 5.7)
query_cache_type = 1
query_cache_size = 64M
*/ 