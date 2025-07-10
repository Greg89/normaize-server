-- Database Cleanup Script
-- This script drops all tables and the migration history to resolve migration conflicts
-- WARNING: This will DELETE ALL DATA in the database!

-- Drop all tables (this will also drop the __EFMigrationsHistory table)
DROP TABLE IF EXISTS DataSetRows;
DROP TABLE IF EXISTS DataSets;
DROP TABLE IF EXISTS Analyses;
DROP TABLE IF EXISTS __EFMigrationsHistory;

-- Verify tables are dropped
SELECT 'Tables dropped successfully' as Status; 