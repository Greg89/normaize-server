# Contributing to Normaize

Thank you for your interest in contributing to Normaize! This document provides guidelines and information for contributors.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a feature branch** for your changes
4. **Make your changes** following the guidelines below
5. **Test your changes** thoroughly
6. **Submit a pull request**

## Development Setup

### Prerequisites
- .NET 9 SDK
- Node.js 18+ (if working on frontend)
- Docker (optional)

### Backend Development
```bash
cd Normaize.API
dotnet restore
dotnet run
```

### Frontend Development
```bash
cd frontend
npm install
npm run dev
```

## Code Style Guidelines

### C# (.NET Backend)
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and under 50 lines when possible
- Use async/await for I/O operations

### TypeScript/React (Frontend)
- Follow ESLint and Prettier configurations
- Use functional components with hooks
- Implement proper error handling
- Add JSDoc comments for complex functions

## Testing

### Backend Tests
- Write unit tests for business logic
- Use xUnit for testing framework
- Aim for >80% code coverage
- Run tests: `dotnet test`

### Frontend Tests
- Write unit tests for components
- Use Jest and React Testing Library
- Run tests: `npm test`

## Pull Request Guidelines

1. **Create a descriptive title** for your PR
2. **Add a detailed description** of your changes
3. **Include screenshots** for UI changes
4. **Reference issues** if applicable
5. **Ensure all tests pass**
6. **Update documentation** if needed

## Commit Message Format

Use conventional commit format:
```
type(scope): description

[optional body]

[optional footer]
```

Examples:
- `feat(api): add dataset upload endpoint`
- `fix(ui): resolve navigation menu alignment`
- `docs(readme): update deployment instructions`

## Issue Reporting

When reporting issues:
1. Use the issue template
2. Provide detailed reproduction steps
3. Include error messages and logs
4. Specify your environment (OS, browser, etc.)

## Questions or Need Help?

- Open an issue for bugs or feature requests
- Join our discussions for general questions
- Check existing issues and PRs first

Thank you for contributing to Normaize! 🚀 