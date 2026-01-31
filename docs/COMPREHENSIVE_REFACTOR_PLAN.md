# Comprehensive Refactor Plan for Normaize (Server + Client)

**Document Version:** 1.0  
**Last Updated:** January 31, 2026  
**Status:** Active  
**Review Cycle:** Weekly/Bi-weekly/Monthly (As time permits)

---

## 📋 Executive Summary

This document outlines a comprehensive, incremental refactor plan for both the Normaize server and client applications. The server has recently undergone significant modernization with DDD architecture, async operations, PostgreSQL migration, and enhanced telemetry. The client has been updated with improved authentication and state management. This plan identifies areas for continued improvement, technical debt removal, and architectural enhancement to be executed over weeks or months.

---

## 🎯 Objectives

1. **Remove Technical Debt**: Eliminate TODOs, placeholders, deprecated patterns
2. **Improve Code Quality**: Enhance maintainability, testability, and readability
3. **Optimize Performance**: Address bottlenecks and inefficiencies
4. **Enhance Developer Experience**: Improve tooling, documentation, and onboarding
5. **Strengthen Architecture**: Ensure consistency with DDD and clean architecture principles
6. **Increase Test Coverage**: Achieve >95% coverage across critical paths
7. **Modernize UI/UX**: Update client for better user experience and accessibility

---

## 🏗️ Current State Assessment

### Server (Normaize.DataNormalization)

**Strengths:**
- ✅ Clean DDD architecture with clear layer separation
- ✅ CQRS pattern implementation with MediatR
- ✅ Domain events for extensibility
- ✅ Rich value objects and aggregates
- ✅ Modern .NET 9.0 framework
- ✅ PostgreSQL migration complete
- ✅ Comprehensive domain test coverage (59 passing tests)
- ✅ Enhanced telemetry and logging

**Areas for Improvement:**
- ✅ ID types consistent (Analysis migrated to Guid)
- ✅ Pagination total counts implemented (Analyses + DataSets)
- ✅ Jobs list pagination implemented
- ⚠️ Temporary `[AllowAnonymous]` attributes for testing
- ⚠️ Placeholder logic in some endpoints (e.g., `GetRetentionStatus`)
- ⚠️ Missing comprehensive error handling
- ⚠️ Application layer test coverage needs expansion
- ⚠️ Background job processing needs review
- ⚠️ API documentation (XML comments) incomplete
- ⚠️ Caching strategy not implemented

### Client (Normaize-Client)

**Strengths:**
- ✅ Modern React + TypeScript + Vite setup
- ✅ Auth0 integration with session persistence
- ✅ Error boundary implementation
- ✅ Sentry integration for error tracking
- ✅ Tailwind CSS for styling
- ✅ API service layer abstraction
- ✅ Custom hooks for reusable logic

**Areas for Improvement:**
- ⚠️ Component organization and file structure
- ⚠️ State management (no global state solution like Zustand/Redux)
- ⚠️ API response type safety and validation
- ⚠️ Loading states and error handling consistency
- ⚠️ Accessibility (a11y) compliance
- ⚠️ Performance optimization (code splitting, lazy loading)
- ⚠️ Test coverage expansion
- ⚠️ UI/UX consistency and design system
- ⚠️ Mobile responsiveness improvements

---

## 📊 Refactor Areas by Priority

### Priority 1: Critical (Security & Stability)

#### Server
1. **Remove Temporary Anonymous Access**
   - Location: Controllers with `[AllowAnonymous]` and "Temporary" comments
   - Impact: Security vulnerability
   - Effort: 1-2 days
   - Files: All controllers in API layer

2. **Fix Placeholder Implementations**
   - Location: `GetRetentionStatus` and similar endpoints
   - Impact: Incorrect data returned to clients
   - Effort: 3-5 days
   - Files: Query handlers with TODO/placeholder comments

3. **Standardize Error Handling**
   - Location: All layers
   - Impact: Inconsistent error responses
   - Effort: 1 week
   - Files: All handlers, controllers

#### Client
1. **API Error Handling Consistency**
   - Location: `src/services/api.ts`
   - Impact: Unpredictable error states
   - Effort: 2-3 days
   - Files: All API service methods

2. **Authentication Edge Cases**
   - Location: `AuthStateProvider.tsx`, `useAuth.ts`
   - Impact: User experience issues
   - Effort: 2-3 days
   - Files: Auth components and hooks

### Priority 2: High (Quality & Maintainability)

#### Server
1. **ID Type Consistency**
   - Location: Analysis entity and related code
   - Impact: Type safety and consistency
   - Effort: 1 week
   - ✅ Migration complete: Analysis converted from int to Guid

2. **Complete Pagination Implementation**
   - Location: Query handlers returning paginated responses
   - Impact: Client pagination breaks
   - Effort: 3-5 days
   - Files: All query handlers with pagination
   - ✅ Completed for Analyses + DataSets (correct `TotalItems`)
   - ⚠️ Remaining: Jobs list endpoint (currently placeholder)

3. **Expand Test Coverage**
   - Location: Application layer, Infrastructure layer
   - Impact: Confidence in changes
   - Effort: 2-3 weeks (ongoing)
   - Target: >95% coverage

4. **Add XML Documentation**
   - Location: All public APIs
   - Impact: Developer experience
   - Effort: 1 week
   - Files: Controllers, handlers, domain entities

#### Client
1. **Implement Global State Management**
   - Location: Root of application
   - Impact: Prop drilling, unnecessary re-renders
   - Effort: 1-2 weeks
   - Solution: Zustand or Jotai for lightweight state

2. **Component Refactoring**
   - Location: Large components (Dashboard, DataSets, Analysis)
   - Impact: Maintainability
   - Effort: 2-3 weeks
   - Pattern: Extract smaller, reusable components

3. **Type Safety Enhancement**
   - Location: `src/types/`, API responses
   - Impact: Runtime errors
   - Effort: 1 week
   - Solution: Zod for runtime validation

### Priority 3: Medium (Performance & UX)

#### Server
1. **Implement Caching Strategy**
   - Location: Query handlers for read-heavy operations
   - Impact: Performance improvement
   - Effort: 1-2 weeks
   - Technology: Redis or in-memory cache

2. **Background Job Processing Review**
   - Location: Infrastructure layer, workers
   - Impact: Job reliability
   - Effort: 1-2 weeks
   - Files: Background worker implementations

3. **Database Query Optimization**
   - Location: Repository implementations
   - Impact: Response times
   - Effort: 1 week
   - Approach: Add indexes, optimize N+1 queries

#### Client
1. **Performance Optimization**
   - Location: All pages and components
   - Impact: Load times, user experience
   - Effort: 1-2 weeks
   - Techniques: Code splitting, lazy loading, memoization

2. **UI/UX Consistency**
   - Location: All pages
   - Impact: Professional appearance
   - Effort: 2-3 weeks
   - Solution: Design system, component library

3. **Loading States & Skeletons**
   - Location: All data-fetching components
   - Impact: Perceived performance
   - Effort: 1 week
   - Pattern: Skeleton screens, suspense boundaries

### Priority 4: Low (Nice-to-Have)

#### Server
1. **API Versioning**
   - Location: Controllers
   - Impact: Future-proofing
   - Effort: 1-2 weeks

2. **Rate Limiting**
   - Location: API middleware
   - Impact: API protection
   - Effort: 3-5 days

3. **GraphQL Endpoint** (Optional)
   - Location: New API layer
   - Impact: Flexible querying
   - Effort: 2-4 weeks

#### Client
1. **Accessibility Audit & Fixes**
   - Location: All components
   - Impact: Inclusivity
   - Effort: 2-3 weeks
   - Tools: axe DevTools, WAVE

2. **Internationalization (i18n)**
   - Location: All text content
   - Impact: Global reach
   - Effort: 1-2 weeks
   - Library: react-i18next

3. **Dark Mode**
   - Location: Theming system
   - Impact: User preference
   - Effort: 1 week

---

## 🗺️ Detailed Refactor Roadmap

### Phase 1: Foundation & Stability (Weeks 1-4)

**Focus:** Critical security issues, error handling, testing infrastructure

#### Server Tasks
- [ ] Remove all `[AllowAnonymous]` temporary attributes
- [ ] Implement proper authentication middleware
- [ ] Fix placeholder implementations in handlers
- [ ] Standardize error response format
- [ ] Add global exception handling middleware
- [ ] Set up integration test infrastructure
- [ ] Document authentication flow

#### Client Tasks
- [ ] Audit and fix authentication edge cases
- [ ] Standardize API error handling
- [ ] Add error boundary logging
- [ ] Create loading state standards
- [ ] Set up E2E testing infrastructure (Playwright/Cypress)
- [ ] Document component patterns

**Deliverables:**
- Secure authentication across all endpoints
- Consistent error handling patterns
- Test infrastructure in place
- Documentation updated

### Phase 2: Data Consistency & Quality (Weeks 5-8)

**Focus:** Data integrity, pagination, type safety

#### Server Tasks
- [x] Migrate Analysis entity from int to Guid
- [x] Fix pagination implementations (Analyses + DataSets)
- [ ] Add database migrations for schema changes
- [x] Implement proper total count calculations (Analyses + DataSets)
- [x] Implement Jobs list pagination (`GET /api/datanormalization/jobs`)
- [ ] Add validation using FluentValidation
- [ ] Create integration tests for all endpoints
- [ ] Add XML documentation to public APIs

#### Client Tasks
- [ ] Implement runtime type validation with Zod
- [ ] Fix pagination UI components
- [ ] Add optimistic updates for mutations
- [ ] Implement proper cache invalidation
- [ ] Refactor large components into smaller ones
- [ ] Add unit tests for critical logic
- [ ] Create component library foundation

**Deliverables:**
- Consistent data types across system
- Working pagination everywhere
- Improved type safety
- Higher test coverage

### Phase 3: Architecture & Performance (Weeks 9-12)

**Focus:** State management, caching, optimization

#### Server Tasks
- [ ] Implement caching strategy (Redis)
- [ ] Optimize database queries
- [ ] Add database indexes
- [ ] Review background job processing
- [ ] Implement proper retry mechanisms
- [ ] Add performance monitoring
- [ ] Create load testing suite

#### Client Tasks
- [ ] Implement global state management (Zustand)
- [ ] Add React Query for server state
- [ ] Implement code splitting
- [ ] Add lazy loading for routes
- [ ] Optimize bundle size
- [ ] Add performance monitoring (Web Vitals)
- [ ] Implement virtual scrolling for large lists

**Deliverables:**
- Improved application performance
- Scalable state management
- Optimized data fetching
- Performance baselines established

### Phase 4: Polish & Enhancement (Weeks 13-16)

**Focus:** UX improvements, documentation, tooling

#### Server Tasks
- [ ] Add API versioning
- [ ] Implement rate limiting
- [ ] Create comprehensive API documentation
- [ ] Add health check dashboard
- [ ] Implement audit logging
- [ ] Create deployment runbooks
- [ ] Set up automated performance testing

#### Client Tasks
- [ ] Implement design system
- [ ] Add accessibility features
- [ ] Create skeleton loaders
- [ ] Improve mobile responsiveness
- [ ] Add user onboarding flow
- [ ] Create user documentation
- [ ] Implement analytics tracking

**Deliverables:**
- Professional UI/UX
- Complete documentation
- Production-ready monitoring
- User-friendly features

---

## 📝 File-by-File Review Checklist

### Server Files to Review

#### Critical Files
- [ ] `src/Normaize.DataNormalization.API/Controllers/*.cs` - Remove temporary auth
- [ ] `src/Normaize.DataNormalization.Application/Queries/**/*Handler.cs` - Fix placeholders
- [x] `src/Normaize.DataNormalization.Domain/Entities/Analysis.cs` - Convert ID type
- [ ] `src/Normaize.DataNormalization.Infrastructure/Repositories/*.cs` - Query optimization

#### Supporting Files
- [x] Pagination handlers (Analyses + DataSets)
- [x] Jobs list pagination (DataNormalizationController)
- [ ] All command handlers with validation
- [ ] Background worker implementations
- [ ] Health check implementations
- [ ] Middleware implementations

### Client Files to Review

#### Critical Files
- [ ] `src/services/api.ts` - Error handling consistency
- [ ] `src/components/AuthStateProvider.tsx` - Edge case handling
- [ ] `src/hooks/useAuth.ts` - Token refresh logic
- [ ] `src/types/index.ts` - Add Zod schemas

#### Page Components (Priority Order)
1. [ ] `src/pages/Login.tsx` - First impression
2. [ ] `src/pages/Dashboard.tsx` - Main page, performance critical
3. [ ] `src/pages/DataSets.tsx` - Core functionality
4. [ ] `src/pages/Normalization.tsx` - Core functionality
5. [ ] `src/pages/Analysis.tsx` - Complex state management
6. [ ] `src/pages/Visualization.tsx` - Performance critical
7. [ ] `src/pages/AccountSettings.tsx` - User preferences

#### Supporting Components
- [ ] `src/components/Layout.tsx` - Navigation consistency
- [ ] `src/components/ErrorBoundary*.tsx` - Error handling
- [ ] `src/components/LoadingSpinner.tsx` - Loading states
- [ ] All components in `src/components/normalization/`

---

## 🔍 Code Review Criteria

### Server Code Reviews

**Architecture:**
- [ ] Follows DDD principles (domain logic in domain layer)
- [ ] Proper aggregate boundaries
- [ ] Value objects used appropriately
- [ ] No infrastructure leakage into domain
- [ ] Repository pattern correctly implemented

**Code Quality:**
- [ ] No TODO or placeholder comments
- [ ] Comprehensive error handling
- [ ] Proper logging at appropriate levels
- [ ] XML documentation on public APIs
- [ ] Consistent naming conventions
- [ ] No magic strings or numbers

**Testing:**
- [ ] Unit tests for domain logic
- [ ] Integration tests for repositories
- [ ] API tests for endpoints
- [ ] Test coverage >80% minimum
- [ ] Edge cases covered

**Performance:**
- [ ] Async operations used appropriately
- [ ] No N+1 query issues
- [ ] Proper indexing on database
- [ ] Caching where appropriate
- [ ] No unnecessary allocations

### Client Code Reviews

**Architecture:**
- [ ] Component hierarchy makes sense
- [ ] Proper separation of concerns
- [ ] No prop drilling (use context/state management)
- [ ] Hooks used correctly
- [ ] No anti-patterns (e.g., useEffect dependencies)

**Code Quality:**
- [ ] TypeScript types are specific, not `any`
- [ ] Runtime validation for external data
- [ ] Consistent error handling
- [ ] Proper cleanup in useEffect
- [ ] Accessible HTML elements
- [ ] No console.logs in production code

**Testing:**
- [ ] Unit tests for logic
- [ ] Component tests with React Testing Library
- [ ] E2E tests for critical flows
- [ ] Test coverage >70% minimum
- [ ] Accessibility tests

**Performance:**
- [ ] Memoization where needed
- [ ] No unnecessary re-renders
- [ ] Lazy loading for routes
- [ ] Optimized bundle size
- [ ] No memory leaks

**UX:**
- [ ] Loading states for async operations
- [ ] Error states with helpful messages
- [ ] Keyboard navigation support
- [ ] Mobile-responsive design
- [ ] Consistent UI patterns

---

## 🛠️ Technical Debt Inventory

### Server Technical Debt

| Category | Description | Location | Impact | Effort |
|----------|-------------|----------|--------|--------|
| Security | Temporary anonymous access | Controllers | High | Low |
| Data | Inconsistent ID types | Analysis entity | High | Medium |
| Logic | Placeholder implementations | Query handlers | High | Medium |
| Quality | Pagination total counts incomplete (Jobs) | Controllers/queries | Medium | Low |
| Testing | Low application test coverage | Application layer | Medium | High |
| Docs | Missing XML comments | All public APIs | Low | Medium |
| Performance | No caching strategy | Query handlers | Medium | Medium |
| Architecture | Single DbContext | Infrastructure | Low | High |

### Client Technical Debt

| Category | Description | Location | Impact | Effort |
|----------|-------------|----------|--------|--------|
| State | No global state management | Root | High | Medium |
| Types | No runtime validation | API layer | High | Low |
| Components | Large, complex components | Pages | Medium | High |
| Performance | No code splitting | Routes | Medium | Low |
| Testing | Low test coverage | Components | Medium | High |
| UX | Inconsistent loading states | All pages | Medium | Medium |
| A11y | Accessibility issues | Components | Low | Medium |
| Mobile | Responsive issues | Some pages | Low | Medium |

---

## 📈 Success Metrics

### Server Metrics
- **Test Coverage:** Target >95% for domain, >90% for application, >80% for infrastructure
- **API Response Time:** P95 <200ms for queries, <500ms for commands
- **Error Rate:** <0.1% for 5xx errors
- **Code Quality:** SonarQube maintainability rating A
- **Documentation:** 100% public API coverage

### Client Metrics
- **Test Coverage:** Target >80% overall
- **Lighthouse Score:** Performance >90, Accessibility >90, Best Practices >90, SEO >90
- **Bundle Size:** Initial <200KB, Total <1MB
- **Load Time:** First Contentful Paint <1.5s, Time to Interactive <3s
- **Error Rate:** <1% unhandled errors
- **User Satisfaction:** Based on feedback/analytics

---

## 🚀 Quick Start Guide for AI Agents

When picking up work on this refactor plan:

1. **Understand Context:**
   - Read this document fully
   - Review the specific phase you're working on
   - Check related documentation in `docs/` folder

2. **Choose a Task:**
   - Start with Priority 1 items
   - Pick tasks matching your estimated time availability
   - Check dependencies between tasks

3. **Before Coding:**
   - Review relevant existing code
   - Check test coverage in that area
   - Identify files that will be affected
   - Plan your changes

4. **During Development:**
   - Follow established patterns
   - Write tests first (TDD when possible)
   - Keep commits small and focused
   - Update documentation as you go

5. **Before Completion:**
   - Run all tests
   - Check code quality metrics
   - Update this document with progress
   - Mark tasks as complete

6. **Handoff:**
   - Document any blockers or decisions made
   - Update task status in this document
   - Note any new technical debt discovered

---

## 📚 Related Documentation

### Server Documentation
- [DDD_MIGRATION_PLAN.md](./DDD_MIGRATION_PLAN.md) - Overall DDD migration strategy
- [DDD_MIGRATION_STANDARDS.md](./DDD_MIGRATION_STANDARDS.md) - Coding standards
- [MIGRATION_ANALYSIS.md](./MIGRATION_ANALYSIS.md) - Legacy to DDD analysis
- [CLEAN_ARCHITECTURE_MIGRATION.md](./CLEAN_ARCHITECTURE_MIGRATION.md) - Clean architecture guidelines

### Client Documentation
- [README.md](../../normaize-client/README.md) - Client setup and overview
- [AUTH_REFRESH_FIX.md](../../normaize-client/docs/AUTH_REFRESH_FIX.md) - Auth implementation
- [PRODUCTION_CHECKLIST.md](../../normaize-client/docs/PRODUCTION_CHECKLIST.md) - Production readiness

---

## 📞 Contact & Communication

- **Primary Documentation:** This file and related docs in `docs/` folders
- **Issue Tracking:** Use GitHub issues for tracking specific tasks
- **Progress Updates:** Update task checkboxes in this document
- **Questions:** Document assumptions and decisions inline

---

**Next Review Date:** February 7, 2026  
**Document Owner:** Development Team  
**Status:** Active - Ready for implementation
