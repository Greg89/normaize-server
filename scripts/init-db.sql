-- Initialize PostgreSQL database for Normaize DDD projects
-- This script runs when the PostgreSQL container starts for the first time

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create schemas for different bounded contexts
CREATE SCHEMA IF NOT EXISTS data_normalization;
CREATE SCHEMA IF NOT EXISTS data_sets;
CREATE SCHEMA IF NOT EXISTS user_settings;
CREATE SCHEMA IF NOT EXISTS audit;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA data_normalization TO normaize_user;
GRANT ALL PRIVILEGES ON SCHEMA data_sets TO normaize_user;
GRANT ALL PRIVILEGES ON SCHEMA user_settings TO normaize_user;
GRANT ALL PRIVILEGES ON SCHEMA audit TO normaize_user;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA data_normalization GRANT ALL ON TABLES TO normaize_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA data_sets GRANT ALL ON TABLES TO normaize_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA user_settings GRANT ALL ON TABLES TO normaize_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA audit GRANT ALL ON TABLES TO normaize_user;

-- Create a function to generate UUIDs (if not using uuid-ossp)
CREATE OR REPLACE FUNCTION generate_uuid() RETURNS UUID AS $$
BEGIN
    RETURN uuid_generate_v4();
END;
$$ LANGUAGE plpgsql;
