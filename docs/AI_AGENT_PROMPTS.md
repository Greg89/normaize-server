# AI Agent Prompt Templates for Normaize Refactor

**Document Version:** 1.0  
**Last Updated:** January 31, 2026  
**Purpose:** Standardized prompts for AI agents working on the Normaize refactor project

---

## 📖 How to Use This Document

This document contains pre-written prompts that any AI agent can use to understand and continue work on the Normaize refactor project. Each prompt is self-contained and provides all necessary context.

**Instructions:**
1. Copy the entire prompt that matches your task
2. Paste it into your AI agent conversation
3. The agent will have all necessary context to begin work
4. Update the [SPECIFIC_AREA] placeholders with your actual target

---

## 🎯 General Context Prompt (Use First)

```
I'm working on the Normaize project, which consists of two applications:

1. **Server (normaize-server)**: .NET 9.0 application following DDD (Domain-Driven Design) architecture
   - Path: c:\Projects\normaize-server
   - Architecture: Clean architecture with Domain, Application, Infrastructure, and API layers
   - Database: PostgreSQL
   - Patterns: CQRS with MediatR, Repository pattern, Domain events
   - Testing: xUnit with high domain test coverage
   - Recent changes: Migrated from legacy monolith to DDD, added async operations, improved telemetry

2. **Client (normaize-client)**: React + TypeScript + Vite SPA
   - Path: c:\Projects\normaize-client
   - State: Local component state, no global state management yet
   - Auth: Auth0 with session persistence
   - Styling: Tailwind CSS
   - Testing: Jest + React Testing Library
   - Recent changes: Auth improvements, error boundaries, logging integration

**Key Documentation:**
- Comprehensive refactor plan: c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md
- DDD standards: c:\Projects\normaize-server\docs\DDD_MIGRATION_STANDARDS.md
- Migration plan: c:\Projects\normaize-server\docs\DDD_MIGRATION_PLAN.md

**My Goal:** [DESCRIBE YOUR GOAL]

Please review the comprehensive refactor plan and help me understand what areas I should focus on for: [DESCRIBE SPECIFIC AREA]
```

---

## 🔐 Server: Priority 1 - Security & Stability Prompts

### Prompt 1.1: Remove Temporary Anonymous Access

```
Context: I'm working on the Normaize server (DDD .NET 9.0 application) located at c:\Projects\normaize-server. The codebase recently migrated from a legacy monolith to clean DDD architecture.

Task: Remove all temporary `[AllowAnonymous]` attributes from controllers that were added for testing purposes.

Requirements:
1. Search for all instances of [AllowAnonymous] in the API layer
2. Identify which ones have comments indicating they're temporary (e.g., "Temporary: Allow testing without Auth0")
3. Remove these attributes
4. Ensure proper authentication middleware is configured
5. Update any affected tests to handle authentication
6. Document the authentication requirements for each endpoint

Architecture Context:
- Location: src/Normaize.DataNormalization.API/Controllers/
- Auth scheme: Auth0 JWT bearer tokens
- Standard attributes: [Authorize] for protected endpoints
- Follow patterns in: docs/DDD_MIGRATION_STANDARDS.md

Please start by searching for all [AllowAnonymous] attributes and showing me what you find, then we'll create a plan to remove the temporary ones systematically.
```

### Prompt 1.2: Fix Placeholder Implementations

```
Context: I'm working on the Normaize server DDD application at c:\Projects\normaize-server.

Task: Find and fix all placeholder implementations in query and command handlers that return fake or hardcoded data.

Known Issues:
1. GetRetentionStatus handler has placeholder values like CreatedAt = DateTime.UtcNow (should be from database)
2. Some endpoints may have TODO comments indicating incomplete logic
3. Placeholders marked with "PLACEHOLDER", "TODO", or "FIXME" comments

Requirements:
1. Search for placeholder patterns in Application layer handlers
2. Identify what real implementation should be (query database, call domain service, etc.)
3. Implement proper logic following DDD patterns
4. Add unit tests for the corrected behavior
5. Ensure no hardcoded test data remains

Search Locations:
- src/Normaize.DataNormalization.Application/Queries/**/*Handler.cs
- src/Normaize.DataNormalization.Application/Commands/**/*Handler.cs

Please search for "TODO", "PLACEHOLDER", "FIXME", and any handler methods that return hardcoded DateTime.UtcNow or similar test values. Show me what you find first.
```

### Prompt 1.3: Standardize Error Handling

```
Context: I'm working on the Normaize server (.NET 9.0 DDD architecture) at c:\Projects\normaize-server.

Task: Create a consistent error handling strategy across all layers of the application.

Current State:
- Inconsistent error response formats from API
- Some handlers don't catch domain exceptions properly
- No global exception handling middleware

Requirements:
1. Create a global exception handling middleware
2. Define standard error response format (ApiResponse<T> with error details)
3. Create custom exception types for domain, application, and infrastructure layers
4. Update all handlers to throw appropriate exceptions
5. Map exceptions to HTTP status codes correctly
6. Add logging at exception boundaries
7. Ensure client gets consistent error format

Reference:
- Current response types: src/Normaize.DataNormalization.API/DTOs/ApiResponse.cs (if exists)
- Follow .NET minimal API exception handling patterns
- Log using Seq/Application Insights integration

Please start by analyzing the current error handling approach, then propose a comprehensive strategy.
```

---

## 📊 Server: Priority 2 - Data Consistency Prompts

### Prompt 2.1: Migrate Analysis Entity to Guid

```
Context: I'm working on the Normaize server at c:\Projects\normaize-server. The domain uses DDD patterns with PostgreSQL database.

Task: Convert the Analysis entity ID from int to Guid for consistency with other entities.

Current State:
- Most entities use Guid (DataSet, NormalizationJob, Statistics, User)
- Analysis entity still uses int Id
- This creates type inconsistency and potential client compatibility issues

Requirements:
1. Update Analysis entity to use Guid Id
2. Create database migration for ID column change
3. Update all references to Analysis.Id throughout the codebase
4. Update DTOs and API responses
5. Update repository methods
6. Update all tests
7. Consider data migration strategy if production data exists

Steps:
1. First, search for all references to Analysis entity
2. Show me the current implementation
3. Create a migration plan with backward compatibility if needed
4. Implement changes systematically

Architecture:
- Domain layer: src/Normaize.DataNormalization.Domain/Entities/Analysis.cs
- Infrastructure: src/Normaize.DataNormalization.Infrastructure/Data/Configurations/
- Tests: tests/Normaize.DataNormalization.Domain.Tests/

Please start by showing me the current Analysis entity and all its usages.
```

### Prompt 2.2: Fix Pagination Implementations

```
Context: I'm working on the Normaize server (c:\Projects\normaize-server), a DDD-based .NET application.

Task: Fix all pagination implementations that return incorrect total counts or missing pagination metadata.

Known Issues:
- GetDataSets query returns totalItems = responses.Count (should be total in database)
- Some queries don't calculate total count before pagination
- PagedResponse<T> may have incorrect HasNext/HasPrevious values

Requirements:
1. Find all query handlers that return paginated results
2. Ensure total count is calculated from database before applying Skip/Take
3. Verify HasNext and HasPrevious are calculated correctly
4. Ensure Page and PageSize are returned accurately
5. Add tests for pagination edge cases (empty results, last page, etc.)
6. Consider performance implications of count queries

Pattern to Follow:
```csharp
var totalItems = await query.CountAsync(cancellationToken);
var items = await query
    .Skip((request.Page - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);

return new PagedResponse<T>
{
    Items = items,
    TotalItems = totalItems,
    Page = request.Page,
    PageSize = request.PageSize,
    TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize),
    HasNext = request.Page < totalPages,
    HasPrevious = request.Page > 1
};
```

Please search for all handlers returning paginated responses and show me the issues.
```

### Prompt 2.3: Add FluentValidation

```
Context: I'm working on the Normaize server at c:\Projects\normaize-server, using .NET 9.0 with MediatR for CQRS.

Task: Implement FluentValidation for all commands and queries to validate input before reaching domain layer.

Requirements:
1. Add FluentValidation NuGet package
2. Create validator classes for each command and query in Application layer
3. Register validators in DI container
4. Add MediatR pipeline behavior for validation
5. Throw ValidationException with proper error details
6. Update error handling middleware to handle ValidationException
7. Add tests for validation logic

Implementation Pattern:
- Create validators in: src/Normaize.DataNormalization.Application/Commands/Validators/
- Create pipeline behavior: src/Normaize.DataNormalization.Application/Behaviors/ValidationBehavior.cs
- Register in Program.cs or DI configuration

Example validation rules:
- Required fields
- String length limits
- Valid Guid formats
- Business rule validation (e.g., retention days > 0)
- Email format validation

Please help me set up FluentValidation infrastructure first, then we'll add validators incrementally.
```

---

## 🎨 Client: Priority 1 - Critical Fixes Prompts

### Prompt 3.1: Standardize API Error Handling

```
Context: I'm working on the Normaize React client at c:\Projects\normaize-client.

Task: Create consistent error handling across all API calls in the services layer.

Current Issues:
- Inconsistent error handling in src/services/api.ts
- Some calls don't handle 401 (auth) errors properly
- Error messages not user-friendly
- No retry logic for transient failures

Requirements:
1. Create a centralized error handling function
2. Handle different HTTP status codes appropriately:
   - 401: Trigger re-authentication
   - 403: Show permission denied
   - 404: Show not found
   - 500: Show server error with retry option
   - Network errors: Show connectivity issue
3. Add automatic retry for GET requests (with exponential backoff)
4. Use react-hot-toast for user notifications
5. Log errors to logger utility
6. Type error responses properly

Current Structure:
- API service: src/services/api.ts
- Auth context: src/contexts/auth.tsx
- Error boundaries: src/components/ErrorBoundary*.tsx

Please analyze the current error handling and propose a comprehensive solution.
```

### Prompt 3.2: Fix Authentication Edge Cases

```
Context: I'm working on the Normaize React client (c:\Projects\normaize-client) using Auth0 for authentication.

Task: Fix authentication edge cases and improve token refresh logic.

Known Issues:
- Token refresh sometimes fails silently
- Race conditions during concurrent API calls
- User gets logged out unexpectedly
- Session persistence issues on page reload

Current Implementation:
- Auth provider: src/components/AuthStateProvider.tsx
- Auth hook: src/hooks/useAuth.ts
- Session persistence: src/components/SessionPersistence.tsx
- Auth0 wrapper: src/components/Auth0Provider.tsx

Requirements:
1. Implement proper token refresh queue (prevent concurrent refreshes)
2. Add retry logic for failed token refresh
3. Handle Auth0 errors gracefully
4. Improve session persistence mechanism
5. Add loading states during authentication
6. Test edge cases:
   - Token expired during API call
   - Multiple API calls triggering refresh
   - Network failure during auth
   - Page reload during authentication

Please analyze the current auth implementation and identify specific edge cases to fix.
```

---

## 🏗️ Client: Priority 2 - Architecture Prompts

### Prompt 4.1: Implement Global State Management

```
Context: I'm working on the Normaize React client at c:\Projects\normaize-client, currently using only local component state.

Task: Implement global state management using Zustand (or Jotai if you prefer lighter alternative).

Current Issues:
- Prop drilling for shared state
- Duplicate API calls across components
- State reset on component unmount
- No global loading/error states

Requirements:
1. Choose and install state management library (recommend Zustand for simplicity)
2. Create stores for:
   - User state (profile, preferences)
   - Datasets state (list, filters)
   - Jobs state (normalization jobs, status)
   - UI state (sidebar open, notifications)
3. Implement store persistence for appropriate state
4. Migrate components away from prop drilling
5. Add devtools for debugging
6. Create custom hooks for store access

Suggested Structure:
```
src/stores/
  userStore.ts
  datasetStore.ts
  jobStore.ts
  uiStore.ts
  index.ts
```

Please help me set up the infrastructure and migrate one component as an example, then I can continue with others.
```

### Prompt 4.2: Implement Runtime Type Validation with Zod

```
Context: I'm working on the Normaize React client (c:\Projects\normaize-client) using TypeScript.

Task: Add runtime type validation for all API responses using Zod.

Current Issues:
- API responses trusted without validation
- Runtime errors when API returns unexpected data
- TypeScript types don't protect against malformed responses
- No validation in tests

Requirements:
1. Install Zod
2. Create Zod schemas for all types in src/types/index.ts
3. Add validation to API service layer
4. Handle validation errors gracefully
5. Type the validated responses properly
6. Add tests for validation logic
7. Document the validation approach

Implementation Pattern:
```typescript
// Define schema
const DataSetSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  // ... other fields
});

// Validate in API call
const response = await fetch('/api/datasets');
const data = await response.json();
const validated = DataSetSchema.parse(data); // Throws if invalid
```

Current types location: src/types/index.ts
API service: src/services/api.ts

Please help me set up Zod and create schemas for the main types first.
```

### Prompt 4.3: Refactor Large Components

```
Context: I'm working on the Normaize React client at c:\Projects\normaize-client.

Task: Refactor large page components into smaller, reusable components.

Target Components (in order of priority):
1. src/pages/Dashboard.tsx
2. src/pages/DataSets.tsx
3. src/pages/Analysis.tsx
4. src/pages/Normalization.tsx
5. src/pages/Visualization.tsx

Current Issues:
- Components are too large (>300 lines)
- Mixed concerns (data fetching, UI, business logic)
- Duplicate code across components
- Hard to test
- Hard to maintain

Refactoring Strategy:
1. Extract data fetching into custom hooks (e.g., useDataSets, useJobs)
2. Extract UI sections into sub-components
3. Extract form logic into form components
4. Create a shared component library
5. Apply composition patterns
6. Add prop-types or TypeScript interfaces
7. Write tests for extracted components

Pattern to Follow:
- Feature-based folder structure
- Co-locate related components
- Use compound components pattern
- Keep components under 150 lines

Please start with Dashboard.tsx - analyze it and propose a refactoring structure.
```

---

## 🚀 Server: Priority 3 - Performance Prompts

### Prompt 5.1: Implement Caching Strategy

```
Context: I'm working on the Normaize server (.NET 9.0 DDD) at c:\Projects\normaize-server.

Task: Implement caching strategy for read-heavy operations to improve performance.

Requirements:
1. Choose caching technology (Redis or in-memory IMemoryCache)
2. Identify query handlers that benefit from caching:
   - GetDataSets (list of datasets)
   - GetDataSet (individual dataset)
   - GetStatistics (dataset statistics)
   - GetRetentionStatus
3. Implement caching at Application layer (decorate handlers)
4. Define cache invalidation strategy
5. Set appropriate cache TTL values
6. Add cache monitoring/metrics
7. Consider cache warming strategy
8. Add configuration for cache settings

Implementation Approach:
- Use decorator pattern or MediatR pipeline behavior
- Invalidate cache on related commands (CreateDataSet, UpdateDataSet, etc.)
- Use cache keys based on query parameters
- Add cache-control headers to API responses

Architecture:
- Add caching interfaces to Application layer
- Implement in Infrastructure layer
- Register in DI container
- Configure in appsettings.json

Please help me design the caching architecture first, then implement for one query as example.
```

### Prompt 5.2: Optimize Database Queries

```
Context: I'm working on the Normaize server (c:\Projects\normaize-server) using PostgreSQL with EF Core.

Task: Optimize database queries to improve performance and eliminate N+1 query problems.

Areas to Investigate:
1. Repository implementations with missing .Include()
2. Lazy loading causing N+1 queries
3. Missing database indexes
4. Inefficient query patterns
5. Large result sets without pagination

Requirements:
1. Analyze all repository methods for N+1 issues
2. Add appropriate .Include() for navigation properties
3. Use .AsNoTracking() for read-only queries
4. Add database indexes for frequently queried columns
5. Use projection (Select) instead of loading full entities when possible
6. Add query performance logging
7. Create performance tests

Focus Areas:
- src/Normaize.DataNormalization.Infrastructure/Repositories/
- Entity configurations with indexes
- Query handlers with multiple database calls

Tools:
- Enable EF Core query logging
- Use SQL profiler to identify slow queries
- Consider using compiled queries for hot paths

Please start by analyzing the repository implementations and showing me potential N+1 issues.
```

---

## 🎨 Client: Priority 3 - Performance & UX Prompts

### Prompt 6.1: Implement Code Splitting and Lazy Loading

```
Context: I'm working on the Normaize React client (c:\Projects\normaize-client) built with Vite.

Task: Implement code splitting and lazy loading to improve initial load time.

Current State:
- All pages loaded in main bundle
- Large bundle size affecting load time
- No dynamic imports

Requirements:
1. Convert page routes to lazy loaded components
2. Add Suspense boundaries with loading fallbacks
3. Split large third-party libraries (recharts, etc.)
4. Implement route-based code splitting
5. Analyze bundle with Vite bundle analyzer
6. Set chunk size limits
7. Preload critical chunks
8. Optimize vendor chunk splitting

Implementation:
```typescript
// Before
import Dashboard from './pages/Dashboard';

// After
const Dashboard = lazy(() => import('./pages/Dashboard'));

// In Routes
<Suspense fallback={<LoadingSpinner />}>
  <Routes>
    <Route path="/" element={<Dashboard />} />
  </Routes>
</Suspense>
```

Target Bundle Sizes:
- Initial bundle: <200KB
- Total: <1MB
- Individual chunks: <50KB

Please help me set up lazy loading and analyze the current bundle.
```

### Prompt 6.2: Implement Design System and Component Library

```
Context: I'm working on the Normaize React client at c:\Projects\normaize-client using Tailwind CSS.

Task: Create a consistent design system with reusable component library.

Current Issues:
- Inconsistent button styles across pages
- Duplicated Tailwind classes
- No standardized spacing/colors
- Hard to maintain consistency

Requirements:
1. Define design tokens (colors, spacing, typography, shadows)
2. Create base components:
   - Button (variants: primary, secondary, danger, ghost)
   - Input (with validation states)
   - Card
   - Modal
   - Alert/Toast
   - Table
   - Badge
   - Avatar
3. Create compound components (DataTable, FormField)
4. Document components with Storybook (optional)
5. Extract Tailwind utilities to component library
6. Ensure accessibility (ARIA attributes, keyboard nav)

Suggested Structure:
```
src/components/ui/
  Button.tsx
  Input.tsx
  Card.tsx
  Modal.tsx
  Alert.tsx
  Badge.tsx
  index.ts
```

Design System Config:
- Extend Tailwind config with custom colors/spacing
- Create CSS custom properties for theming
- Support dark mode (future)

Please help me create the foundation and a few core components as examples.
```

### Prompt 6.3: Add Loading Skeletons and Optimistic Updates

```
Context: I'm working on the Normaize React client (c:\Projects\normaize-client).

Task: Implement skeleton loaders and optimistic UI updates to improve perceived performance.

Requirements:

**Skeleton Loaders:**
1. Create skeleton components for:
   - Dataset list (DataSets page)
   - Dashboard cards
   - Analysis results
   - Table rows
2. Show skeletons during loading instead of spinner
3. Match skeleton layout to actual content
4. Animate skeletons (shimmer effect)

**Optimistic Updates:**
1. Implement for mutations:
   - Create dataset
   - Delete dataset
   - Update dataset name
   - Submit normalization job
2. Update UI immediately before API call
3. Rollback on error
4. Show error toast on failure
5. Use React Query or custom implementation

Implementation Example (with React Query):
```typescript
const { mutate } = useMutation({
  mutationFn: deleteDataset,
  onMutate: async (id) => {
    // Optimistically remove from UI
    queryClient.setQueryData(['datasets'], (old) => 
      old.filter(d => d.id !== id)
    );
  },
  onError: (err, id, context) => {
    // Rollback
    queryClient.invalidateQueries(['datasets']);
    toast.error('Failed to delete');
  }
});
```

Please help me implement skeletons and optimistic updates for the datasets page first.
```

---

## 🧪 Testing Prompts

### Prompt 7.1: Expand Server Test Coverage

```
Context: I'm working on the Normaize server at c:\Projects\normaize-server using xUnit.

Task: Expand test coverage in Application and Infrastructure layers to reach >90% coverage.

Current State:
- Domain layer: 59 passing tests, good coverage
- Application layer: Limited tests (3 tests)
- Infrastructure layer: Minimal tests
- Target: >95% domain, >90% application, >80% infrastructure

Requirements:

**Application Layer Tests:**
1. Test all command handlers
2. Test all query handlers
3. Test validation logic
4. Test DTO mappings
5. Test error scenarios
6. Mock repository dependencies

**Infrastructure Layer Tests:**
1. Integration tests for repositories
2. Test EF Core configurations
3. Test database migrations
4. Test external service integrations
5. Use in-memory database or test containers

**Test Patterns:**
- Use AAA pattern (Arrange, Act, Assert)
- Use builders for test data
- Use fixtures for common setup
- Parameterize tests where appropriate

Testing Tools:
- xUnit for test framework
- Moq for mocking
- FluentAssertions for readable assertions
- Bogus for test data generation
- Testcontainers for integration tests

Please help me create a test strategy and implement tests for [SPECIFIC HANDLER/REPOSITORY].
```

### Prompt 7.2: Add Client E2E Tests

```
Context: I'm working on the Normaize React client at c:\Projects\normaize-client.

Task: Set up end-to-end testing with Playwright or Cypress for critical user flows.

Requirements:

**E2E Test Infrastructure:**
1. Choose testing framework (recommend Playwright for modern features)
2. Set up test environment configuration
3. Configure test database/API mocking
4. Set up CI/CD integration
5. Add test reporting

**Critical Flows to Test:**
1. User login flow
2. Upload dataset flow
3. Create normalization job flow
4. View analysis results
5. Delete dataset flow
6. Error handling (network failures, etc.)

**Test Structure:**
```
e2e/
  fixtures/
    users.json
    datasets.json
  pages/
    LoginPage.ts
    DashboardPage.ts
    DataSetsPage.ts
  tests/
    auth.spec.ts
    datasets.spec.ts
    normalization.spec.ts
```

**Best Practices:**
- Use Page Object Model pattern
- Avoid test interdependencies
- Use API calls for setup/teardown
- Test on multiple viewports (desktop, mobile)
- Add visual regression tests

Please help me set up Playwright and create one complete E2E test as an example.
```

---

## 🔧 Maintenance & Documentation Prompts

### Prompt 8.1: Add Comprehensive API Documentation

```
Context: I'm working on the Normaize server (.NET 9.0) at c:\Projects\normaize-server.

Task: Add comprehensive XML documentation to all public APIs and generate API documentation.

Requirements:

**XML Documentation:**
1. Add XML comments to all controllers
2. Document all request/response DTOs
3. Document parameters, return values, exceptions
4. Include usage examples where helpful
5. Document authentication requirements

**API Documentation Generation:**
1. Configure Swagger/OpenAPI properly
2. Add XML comments to Swagger
3. Group endpoints logically
4. Add examples to Swagger
5. Document error responses
6. Add authentication documentation
7. Generate static documentation (optional)

**Documentation Template:**
```csharp
/// <summary>
/// Gets a paginated list of datasets for the authenticated user.
/// </summary>
/// <param name="page">Page number (1-based)</param>
/// <param name="pageSize">Number of items per page (max 100)</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>A paginated list of datasets</returns>
/// <response code="200">Returns the paginated list of datasets</response>
/// <response code="401">If the user is not authenticated</response>
/// <response code="400">If the pagination parameters are invalid</response>
[HttpGet]
[ProducesResponseType(typeof(PagedResponse<DataSetDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetDataSets(...)
```

Please help me document the API controllers systematically.
```

### Prompt 8.2: Create Developer Onboarding Guide

```
Context: I'm working on the Normaize project (both server and client) at c:\Projects\.

Task: Create comprehensive developer onboarding documentation.

Requirements:

**Documentation to Create:**
1. **CONTRIBUTING.md** - How to contribute
2. **DEVELOPMENT_SETUP.md** - Local dev environment setup
3. **ARCHITECTURE_OVERVIEW.md** - High-level architecture
4. **CODING_STANDARDS.md** - Code style and conventions
5. **TESTING_GUIDE.md** - How to write and run tests
6. **DEBUGGING_GUIDE.md** - Common debugging scenarios
7. **DEPLOYMENT_GUIDE.md** - How to deploy

**Content for Each Guide:**

**Development Setup:**
- Prerequisites (Node, .NET, PostgreSQL)
- Clone repository
- Install dependencies
- Configure environment variables
- Run database migrations
- Start development servers
- Verify setup

**Architecture Overview:**
- System architecture diagram
- DDD layer explanation
- Data flow
- Key patterns used
- Project structure

**Coding Standards:**
- Naming conventions
- File organization
- Comment standards
- Git commit messages
- PR process

**Testing Guide:**
- Test types and when to use them
- Running tests
- Writing tests
- Test coverage requirements
- Debugging tests

Please help me create these guides based on the current project structure.
```

---

## 🎯 Task-Specific Quick Prompts

### Quick Prompt: Review Specific File

```
I'm working on the Normaize project. Please review the file at [FILE_PATH] and identify:
1. Potential bugs or issues
2. Code quality concerns
3. Performance problems
4. Security issues
5. Test coverage gaps
6. Documentation needs
7. Refactoring opportunities

Provide specific, actionable recommendations following DDD and clean architecture principles.

Context documents:
- c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md
- c:\Projects\normaize-server\docs\DDD_MIGRATION_STANDARDS.md
```

### Quick Prompt: Implement Feature from Scratch

```
I'm working on the Normaize project and need to implement: [FEATURE_DESCRIPTION]

Project context:
- Server: c:\Projects\normaize-server (DDD .NET 9.0)
- Client: c:\Projects\normaize-client (React + TypeScript)

Please help me:
1. Design the feature following existing patterns
2. Identify all files that need changes
3. Create implementation plan
4. Write the code following DDD principles (server) or React best practices (client)
5. Include tests
6. Update documentation

Refer to: c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md for coding standards.
```

### Quick Prompt: Debug Specific Issue

```
I'm working on the Normaize project and encountering: [ISSUE_DESCRIPTION]

Project:
- Server: c:\Projects\normaize-server
- Client: c:\Projects\normaize-client

Error details: [ERROR_MESSAGE]

Please help me:
1. Identify the root cause
2. Search relevant files for the issue
3. Propose a fix
4. Explain why this happened
5. Suggest how to prevent similar issues
6. Add tests to catch this in future

Context: Review docs/COMPREHENSIVE_REFACTOR_PLAN.md for architecture understanding.
```

### Quick Prompt: Performance Investigation

```
I'm working on the Normaize project at c:\Projects\ and noticing performance issues with: [SPECIFIC_AREA]

Symptoms: [DESCRIBE SYMPTOMS]

Please help me:
1. Identify potential bottlenecks
2. Analyze the relevant code paths
3. Suggest optimizations
4. Implement performance improvements
5. Add performance tests
6. Measure before/after metrics

Focus areas from refactor plan:
- Server: Database queries, caching, async operations
- Client: Rendering, bundle size, data fetching

Refer to: docs/COMPREHENSIVE_REFACTOR_PLAN.md Priority 3 items.
```

---

## 📋 Session Handoff Prompt

Use this when ending a session and another AI agent will continue:

```
Handoff Summary for Next AI Agent:

Project: Normaize (Server: c:\Projects\normaize-server, Client: c:\Projects\normaize-client)

**What I Completed:**
[List completed tasks with file paths]

**Current Status:**
[Describe current state, what's in progress]

**Blockers/Issues Found:**
[Any problems encountered that need attention]

**Next Steps:**
[What should be done next]

**Files Modified:**
[List all modified files]

**Tests Status:**
[Test results, coverage changes]

**Documentation Updated:**
[What docs were updated]

**Important Notes:**
[Any context the next agent needs]

Reference Documents:
- Refactor Plan: c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md
- This Prompt File: c:\Projects\normaize-server\docs\AI_AGENT_PROMPTS.md

Please review these files and the changes I made before continuing the work.
```

---

## 🔄 Daily Standup Prompt

Use this to get a status update:

```
I'm working on the Normaize refactor project. Please provide a status update:

1. Review: c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md
2. Check git status for both projects (normaize-server and normaize-client)
3. Check for any uncommitted changes
4. Review recent commits (last 5)
5. Identify which phase/tasks are in progress
6. Check test status
7. Check for any TODO/FIXME comments added recently

Provide:
- Summary of completed work
- Current tasks in progress
- Recommended next tasks
- Any risks or blockers
- Test coverage status
```

---

## 💡 Tips for Using These Prompts

1. **Always start with the General Context Prompt** to give the AI full context
2. **Be specific** - Replace [PLACEHOLDER] values with actual details
3. **Run tests** after each change to ensure nothing broke
4. **Document decisions** made during implementation
5. **Update COMPREHENSIVE_REFACTOR_PLAN.md** with progress
6. **Use Quick Prompts** for targeted, specific tasks
7. **Use Session Handoff Prompt** when switching AI agents or ending session
8. **Combine prompts** if working on related tasks

---

## 📝 Template for Custom Prompts

```
Context: I'm working on the Normaize [server/client] at [PATH].

Task: [DESCRIBE TASK]

Current State:
[DESCRIBE CURRENT SITUATION]

Requirements:
1. [REQUIREMENT 1]
2. [REQUIREMENT 2]
...

Architecture Context:
[RELEVANT ARCHITECTURE INFO]

Reference Documents:
- c:\Projects\normaize-server\docs\COMPREHENSIVE_REFACTOR_PLAN.md
- c:\Projects\normaize-server\docs\[OTHER_RELEVANT_DOC]

Please [WHAT YOU WANT THE AI TO DO FIRST].
```

---

**Document Status:** Active  
**Last Updated:** January 31, 2026  
**Next Review:** February 14, 2026
