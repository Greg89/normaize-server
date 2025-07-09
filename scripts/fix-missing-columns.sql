-- Fix missing columns script
-- This script adds any missing columns that should exist based on the current model
-- Run this script if you encounter "Unknown column" errors after deployment

-- Check if DataHash column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'DataHash') = 0,
    'ALTER TABLE DataSets ADD COLUMN DataHash VARCHAR(255);',
    'SELECT "DataHash column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if UserId column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'UserId') = 0,
    'ALTER TABLE DataSets ADD COLUMN UserId LONGTEXT NOT NULL DEFAULT "";',
    'SELECT "UserId column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if FilePath column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'FilePath') = 0,
    'ALTER TABLE DataSets ADD COLUMN FilePath VARCHAR(500) NOT NULL DEFAULT "";',
    'SELECT "FilePath column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if StorageProvider column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'StorageProvider') = 0,
    'ALTER TABLE DataSets ADD COLUMN StorageProvider VARCHAR(50) NOT NULL DEFAULT "Local";',
    'SELECT "StorageProvider column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if ProcessedData column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'ProcessedData') = 0,
    'ALTER TABLE DataSets ADD COLUMN ProcessedData JSON;',
    'SELECT "ProcessedData column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if ProcessingErrors column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'ProcessingErrors') = 0,
    'ALTER TABLE DataSets ADD COLUMN ProcessingErrors TEXT;',
    'SELECT "ProcessingErrors column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if UseSeparateTable column exists, if not add it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSets' 
     AND COLUMN_NAME = 'UseSeparateTable') = 0,
    'ALTER TABLE DataSets ADD COLUMN UseSeparateTable BOOLEAN NOT NULL DEFAULT FALSE;',
    'SELECT "UseSeparateTable column already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if DataSetRows table exists, if not create it
SET @sql = (SELECT IF(
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'DataSetRows') = 0,
    'CREATE TABLE DataSetRows (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        DataSetId INT NOT NULL,
        RowIndex INT NOT NULL,
        Data JSON NOT NULL,
        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE
    );',
    'SELECT "DataSetRows table already exists" as message;'
));
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Create indexes if they don't exist
CREATE INDEX IF NOT EXISTS IX_DataSets_IsProcessed ON DataSets(IsProcessed);
CREATE INDEX IF NOT EXISTS IX_DataSets_UploadedAt ON DataSets(UploadedAt);
CREATE INDEX IF NOT EXISTS IX_DataSets_UseSeparateTable ON DataSets(UseSeparateTable);
CREATE INDEX IF NOT EXISTS idx_datasetrow_dataset_row ON DataSetRows(DataSetId, RowIndex);

-- Verify the fixes
SELECT 'Database schema verification complete' as status;
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'DataSets' 
ORDER BY ORDINAL_POSITION; 