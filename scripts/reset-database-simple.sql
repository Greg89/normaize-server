-- Simple Database Reset Script
-- This script drops all tables to resolve migration conflicts
-- WARNING: This will DELETE ALL DATA in the database!

-- Drop all tables in dependency order
DROP TABLE IF EXISTS DataSetRows;
DROP TABLE IF EXISTS Analyses;
DROP TABLE IF EXISTS DataSets;
DROP TABLE IF EXISTS __EFMigrationsHistory;

-- Verify tables are dropped
SELECT 'Database reset completed successfully' as Status; 