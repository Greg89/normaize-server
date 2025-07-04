# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Upgraded to .NET 9 for improved performance and latest features

### Changed
- Updated all NuGet packages to .NET 9 compatible versions
- Updated Dockerfile to use .NET 9 SDK and runtime
- Updated documentation to reflect .NET 9 requirements

### Added
- GitHub Actions CI/CD pipeline for automated testing and deployment
- Pull request validation workflow
- Code coverage reporting with Codecov integration
- Security vulnerability scanning
- Docker image testing in CI pipeline

### Added
- Initial project setup with .NET 9 Web API
- DataSet management endpoints (CRUD operations)
- File upload functionality for CSV, JSON, and Excel files
- Health check endpoint
- Swagger/OpenAPI documentation
- Docker containerization
- Railway deployment configuration
- Entity Framework Core with MySQL
- AutoMapper for object mapping
- CORS configuration
- Global exception handling middleware

### Changed
- N/A

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [1.0.0] - 2024-01-15

### Added
- Initial release
- Basic API structure
- Health monitoring
- DataSet management
- File upload capabilities 