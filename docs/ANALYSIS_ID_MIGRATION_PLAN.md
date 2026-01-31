# Analysis Entity ID Migration: int → Guid

**Document Version:** 1.0  
**Created:** January 31, 2026  
**Status:** Planning Phase  
**Priority:** High (Priority 2 - Data Consistency)

---

## 📋 Executive Summary

This document outlines the comprehensive plan to migrate the `Analysis` entity from using `int` ID to `Guid` ID for consistency with other domain entities (`DataSet`, `NormalizationJob`, `Statistics`, `User`).

**Current State:**
- Analysis uses `int` ID via `AnalysisId` value object
- All other entities use `Guid` IDs
- Creates type inconsistency across the domain
- Potential client compatibility issues

**Target State:**
- Analysis uses `Guid` ID via updated `AnalysisId` value object
- Consistent ID type across all domain entities
- Simplified type handling in client applications

---

## 🎯 Migration Goals

1. **Type Consistency:** Align Analysis with other domain entities
2. **Zero Data Loss:** Preserve all existing analysis data
3. **Backward Compatibility:** Support both ID formats during transition (if needed)
4. **Client Compatibility:** Update API contracts appropriately
5. **Test Coverage:** Maintain >95% test coverage throughout migration

---

## 🔍 Impact Analysis

### Files Affected (26 files across all layers)

#### Domain Layer (3 files)
- ✅ **ValueObjects/AnalysisId.cs** - Change `int Value` to `Guid Value`
- ✅ **Aggregates/Analysis.cs** - Already uses `AnalysisId`, no changes needed
- ✅ **Events/DomainEvents.cs** - Already uses `AnalysisId` in events, no changes needed

#### Application Layer (6 files)
- ⚠️ **DTOs/AnalysisDtos.cs** - Multiple DTOs with `int Id` property
  - `AnalysisDto`
  - `CreateAnalysisRequest`
  - `UpdateAnalysisRequest`
  - `AnalysisResultDto` (check if has ID)
- ⚠️ **Queries/GetAnalysisQuery.cs** - Query parameter type
- ⚠️ **Queries/GetAnalysisResultQuery.cs** - Query parameter type
- ⚠️ **Commands/DeleteAnalysisCommand.cs** - Command parameter type
- ⚠️ **Commands/UpdateAnalysisCommand.cs** - Command parameter type
- ⚠️ **Commands/RunAnalysisCommand.cs** - Command parameter type

#### Infrastructure Layer (7 files)
- ⚠️ **Data/Configurations/AnalysisConfiguration.cs** - EF Core configuration
  - Currently: `.ValueGeneratedOnAdd()` (for int IDENTITY)
  - Target: `.ValueGeneratedOnAdd()` (for Guid default)
- ⚠️ **Repositories/AnalysisRepository.cs** - Repository methods
- ⚠️ **Services/AnalysisMapper.cs** - DTO mapping logic
- ⚠️ **Services/AnalysisExecutionService.cs** - Service methods
- ⚠️ **Migrations/[NEW]** - Database migration to change column type
- 📝 **Migrations/DataNormalizationDbContextModelSnapshot.cs** - Auto-updated
- 📝 **Data/DataNormalizationDbContext.cs** - Context (verify no hard-coded references)

#### API Layer (5 files)
- ⚠️ **Controllers/AnalysisController.cs** - All endpoint parameters
  - `GetAnalysis(int id)` → `GetAnalysis(Guid id)`
  - `GetAnalysisResult(int id)` → `GetAnalysisResult(Guid id)`
  - `RunAnalysis(int id)` → `RunAnalysis(Guid id)`
  - `DeleteAnalysis(int id)` → `DeleteAnalysis(Guid id)`
  - `UpdateAnalysis(int id, ...)` → `UpdateAnalysis(Guid id, ...)`
  - `ResetAnalysis(int id)` → `ResetAnalysis(Guid id)`
- ⚠️ **DTOs/ApiDTOs.cs** - Response DTOs (check for Analysis references)

#### Test Layer (5+ files)
- ⚠️ **Domain.Tests/Aggregates/AnalysisTests.cs** - Update test data
- ⚠️ **Application.Tests/** - Update command/query handler tests
- ⚠️ **Infrastructure.Tests/** - Update repository tests
- ⚠️ **API.Tests/** - Update controller tests
- 📝 All test fixtures and builders

---

## 🗺️ Migration Strategy

### Option A: Clean Break Migration (Recommended)

**Approach:** Single migration with downtime, no backward compatibility layer.

**Pros:**
- Simplest implementation
- Clean codebase afterward
- Fastest to complete
- Lower risk of bugs

**Cons:**
- Requires coordinated client update
- Brief API downtime during deployment
- Cannot rollback without data restoration

**Best For:** Development/staging environments, or production if coordinated deployment is possible.

### Option B: Dual-Support Migration (Complex)

**Approach:** Support both int and Guid IDs during transition period using API versioning.

**Pros:**
- No breaking changes for existing clients
- Gradual rollout possible
- Can rollback easily

**Cons:**
- Complex implementation
- Temporary code complexity
- Longer migration timeline
- More testing required

**Best For:** Production systems with external clients that cannot update immediately.

---

## 📝 Recommended Approach: Option A (Clean Break)

Given that:
1. This is an internal application (client and server deployed together)
2. No external API consumers
3. Development stage (not yet production)
4. Simpler is better for maintainability

**We recommend Option A: Clean Break Migration**

---

## 🔧 Implementation Plan

### Phase 1: Preparation (Day 1)

**Tasks:**
1. ✅ Create this migration plan document
2. ⏳ Create feature branch: `feature/analysis-id-guid-migration`
3. ⏳ Backup current database
4. ⏳ Document current Analysis table schema
5. ⏳ Create rollback plan
6. ⏳ Notify team of upcoming changes

**Validation:**
- Branch created successfully
- Database backup verified
- Schema documented
- Team notified

---

### Phase 2: Domain Layer Changes (Day 1-2)

**Tasks:**

#### 2.1: Update AnalysisId Value Object
**File:** `src/Normaize.DataNormalization.Domain/ValueObjects/AnalysisId.cs`

```csharp
// BEFORE:
public record AnalysisId
{
    public int Value { get; init; }
    public static readonly AnalysisId Unpersisted = new(0);
    
    public AnalysisId(int value)
    {
        if (value < 0)
            throw new ArgumentException("Analysis ID cannot be negative", nameof(value));
        Value = value;
    }
    
    public bool IsPersisted => Value > 0;
    public static implicit operator int(AnalysisId analysisId) => analysisId.Value;
    public static implicit operator AnalysisId(int value) => new(value);
}

// AFTER:
public record AnalysisId
{
    public Guid Value { get; init; }
    public static readonly AnalysisId Unpersisted = new(Guid.Empty);
    
    public AnalysisId(Guid value)
    {
        Value = value;
    }
    
    /// <summary>
    /// Creates a new AnalysisId with a generated Guid
    /// </summary>
    public static AnalysisId NewId() => new(Guid.NewGuid());
    
    public bool IsPersisted => Value != Guid.Empty;
    public static implicit operator Guid(AnalysisId analysisId) => analysisId.Value;
    public static implicit operator AnalysisId(Guid value) => new(value);
}
```

#### 2.2: Verify Domain Events
**File:** `src/Normaize.DataNormalization.Domain/Events/DomainEvents.cs`

✅ No changes needed - events already use `AnalysisId` type.

#### 2.3: Update Domain Tests
**File:** `tests/Normaize.DataNormalization.Domain.Tests/Aggregates/AnalysisTests.cs`

- Update test data generation to use `Guid.NewGuid()` or `AnalysisId.NewId()`
- Update assertions to compare Guid values
- Verify all tests pass

**Validation:**
- All domain tests pass (59+ tests)
- No compilation errors
- AnalysisId behaves correctly

---

### Phase 3: Application Layer Changes (Day 2-3)

**Tasks:**

#### 3.1: Update DTOs
**File:** `src/Normaize.DataNormalization.Application/DTOs/AnalysisDtos.cs`

```csharp
// BEFORE:
public class AnalysisDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    // ...
}

// AFTER:
public class AnalysisDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    // ...
}
```

Apply to all DTOs:
- `AnalysisDto`
- `CreateAnalysisRequest` (if has ID - probably not)
- `UpdateAnalysisRequest` (if has ID - probably not)
- `AnalysisResultDto` (check implementation)

#### 3.2: Update Command/Query Contracts
**Files:**
- `Application/Queries/GetAnalysisQuery.cs`
- `Application/Queries/GetAnalysisResultQuery.cs`
- `Application/Commands/DeleteAnalysisCommand.cs`
- `Application/Commands/UpdateAnalysisCommand.cs`
- `Application/Commands/RunAnalysisCommand.cs`
- `Application/Commands/ResetAnalysisCommand.cs`

```csharp
// BEFORE:
public record GetAnalysisQuery(int AnalysisId) : IRequest<AnalysisDto>;

// AFTER:
public record GetAnalysisQuery(Guid AnalysisId) : IRequest<AnalysisDto>;
```

#### 3.3: Update Command/Query Handlers
**Files:** All corresponding handlers

```csharp
// BEFORE:
public async Task<AnalysisDto> Handle(GetAnalysisQuery request, CancellationToken cancellationToken)
{
    var analysis = await _repository.GetByIdAsync(new AnalysisId(request.AnalysisId));
    // ...
}

// AFTER:
public async Task<AnalysisDto> Handle(GetAnalysisQuery request, CancellationToken cancellationToken)
{
    var analysis = await _repository.GetByIdAsync(new AnalysisId(request.AnalysisId));
    // ... (actually the same due to implicit conversion)
}
```

#### 3.4: Update Application Tests
**Files:** `tests/Normaize.DataNormalization.Application.Tests/**`

- Update test data to use Guid
- Update mock setups
- Verify all application tests pass

**Validation:**
- All application tests pass
- DTOs serialize/deserialize correctly
- Command/query handlers work with Guid IDs

---

### Phase 4: Infrastructure Layer Changes (Day 3-4)

**Tasks:**

#### 4.1: Update EF Core Configuration
**File:** `src/Normaize.DataNormalization.Infrastructure/Data/Configurations/AnalysisConfiguration.cs`

```csharp
// BEFORE:
builder.Property(e => e.Id)
    .HasColumnName("id")
    .HasConversion(
        v => v.Value,
        v => new AnalysisId(v))
    .ValueGeneratedOnAdd(); // For int IDENTITY

// AFTER:
builder.Property(e => e.Id)
    .HasColumnName("id")
    .HasConversion(
        v => v.Value,
        v => new AnalysisId(v))
    .ValueGeneratedOnAdd(); // For Guid default

// Optional: If you want to ensure Guids are generated on add
builder.Property(e => e.Id)
    .HasColumnName("id")
    .HasConversion(
        v => v.Value,
        v => new AnalysisId(v))
    .HasDefaultValueSql("gen_random_uuid()"); // PostgreSQL function
```

#### 4.2: Update Repository
**File:** `src/Normaize.DataNormalization.Infrastructure/Repositories/AnalysisRepository.cs`

Review for any explicit int conversions or casts. Most code should work due to implicit conversions.

```csharp
// Current code should mostly work, but verify:
// - No explicit (int) casts
// - Logging shows Guid correctly
// - AddAsync works with Guid generation
```

Check the `AddAsync` method for ID generation logic:
```csharp
// BEFORE (line ~179):
var generatedId = new AnalysisId((int)entry.Property(e => e.Id).CurrentValue!);

// AFTER:
var generatedId = new AnalysisId((Guid)entry.Property(e => e.Id).CurrentValue!);
```

#### 4.3: Update Mapper Service
**File:** `src/Normaize.DataNormalization.Infrastructure/Services/AnalysisMapper.cs`

```csharp
// BEFORE:
public AnalysisDto ToDto(Analysis analysis)
{
    return new AnalysisDto
    {
        Id = analysis.Id.Value, // int
        // ...
    };
}

// AFTER:
public AnalysisDto ToDto(Analysis analysis)
{
    return new AnalysisDto
    {
        Id = analysis.Id.Value, // Guid
        // ...
    };
}
```

#### 4.4: Create Database Migration
**Command:**
```bash
cd src/Normaize.DataNormalization.Infrastructure
dotnet ef migrations add ConvertAnalysisIdToGuid --startup-project ../Normaize.DataNormalization.API
```

**Expected Migration Content:**

```csharp
public partial class ConvertAnalysisIdToGuid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Drop foreign keys (if any reference analyses.id)
        // (Check if other tables reference analyses.id)
        
        // Step 2: Create new Guid column
        migrationBuilder.AddColumn<Guid>(
            name: "id_new",
            schema: "data_normalization",
            table: "analyses",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()");
        
        // Step 3: Populate new column with generated Guids
        // (Data already populated via defaultValueSql)
        
        // Step 4: Drop old int column
        migrationBuilder.DropColumn(
            name: "id",
            schema: "data_normalization",
            table: "analyses");
        
        // Step 5: Rename new column to 'id'
        migrationBuilder.RenameColumn(
            name: "id_new",
            schema: "data_normalization",
            table: "analyses",
            newName: "id");
        
        // Step 6: Add primary key constraint
        migrationBuilder.AddPrimaryKey(
            name: "pk_analyses",
            schema: "data_normalization",
            table: "analyses",
            column: "id");
        
        // Step 7: Recreate foreign keys (if any)
        
        // Step 8: Recreate indexes on id column (if any)
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // WARNING: Down migration will LOSE DATA
        // Cannot convert Guid back to sequential int without data loss
        // Consider backing up data before migration
        
        throw new NotSupportedException(
            "Cannot downgrade from Guid to int without data loss. " +
            "Restore from backup if rollback is needed.");
    }
}
```

**⚠️ IMPORTANT:** This migration will **BREAK EXISTING DATA MAPPING**. Old integer IDs cannot be preserved as Guids. This is acceptable for development, but in production you would need:
1. Backup of old ID mappings (if external references exist)
2. Coordination with client deployment
3. Data migration strategy if preserving links is critical

#### 4.5: Update Infrastructure Tests
**Files:** `tests/Normaize.DataNormalization.Infrastructure.Tests/**`

- Update test data to use Guid
- Update integration tests with test database
- Verify repository operations work correctly

**Validation:**
- All infrastructure tests pass
- Database migration succeeds
- Data can be inserted/retrieved with Guid IDs

---

### Phase 5: API Layer Changes (Day 4-5)

**Tasks:**

#### 5.1: Update Controller Methods
**File:** `src/Normaize.DataNormalization.API/Controllers/AnalysisController.cs`

```csharp
// BEFORE:
[HttpGet("{id}")]
public async Task<IActionResult> GetAnalysis(int id)
{
    var query = new GetAnalysisQuery(id);
    var result = await _mediator.Send(query);
    // ...
}

// AFTER:
[HttpGet("{id}")]
public async Task<IActionResult> GetAnalysis(Guid id)
{
    var query = new GetAnalysisQuery(id);
    var result = await _mediator.Send(query);
    // ...
}
```

Update all 6 methods:
- `GetAnalysis(Guid id)`
- `GetAnalysisResult(Guid id)`
- `RunAnalysis(Guid id)`
- `DeleteAnalysis(Guid id)`
- `UpdateAnalysis(Guid id, ...)`
- `ResetAnalysis(Guid id)`

#### 5.2: Update API DTOs (if different from Application DTOs)
**File:** `src/Normaize.DataNormalization.API/DTOs/ApiDTOs.cs`

Check for any Analysis-related response types and update ID fields.

#### 5.3: Update Swagger/OpenAPI Documentation
- Verify Swagger UI shows Guid format for ID parameters
- Add XML comments documenting the ID format change
- Update examples in Swagger

#### 5.4: Update API Tests
**Files:** `tests/Normaize.DataNormalization.API.Tests/**`

- Update test requests to use Guid IDs
- Update assertions
- Verify all API tests pass

**Validation:**
- All API tests pass
- Swagger documentation correct
- All endpoints accept Guid IDs

---

### Phase 6: Client Updates (Day 5-6)

**Tasks:**

#### 6.1: Update Client Types
**File:** `c:\Projects\normaize-client\src\types\index.ts`

```typescript
// BEFORE:
export interface AnalysisDto {
  id: number;
  name: string;
  // ...
}

// AFTER:
export interface AnalysisDto {
  id: string; // Guid serialized as string in JSON
  name: string;
  // ...
}
```

#### 6.2: Update API Service
**File:** `c:\Projects\normaize-client\src\services\api.ts`

```typescript
// BEFORE:
async getAnalysis(id: number): Promise<AnalysisDto> {
  return this.get(`/api/analyses/${id}`);
}

// AFTER:
async getAnalysis(id: string): Promise<AnalysisDto> {
  return this.get(`/api/analyses/${id}`);
}
```

Update all analysis-related methods.

#### 6.3: Update Client Components
**Files:** `c:\Projects\normaize-client\src/pages/Analysis.tsx` and related components

- Update state types from `number` to `string`
- Update comparison logic (use string comparison instead of numeric)
- Update display formatting if needed
- Update URL parameters in routing

#### 6.4: Update Client Tests
**Files:** `c:\Projects\normaize-client\src/**/*.test.ts(x)`

- Update mock data to use string GUIDs
- Update assertions
- Verify all tests pass

**Validation:**
- All client tests pass
- No TypeScript errors
- Client can fetch and display analyses correctly

---

### Phase 7: Integration Testing (Day 6-7)

**Tasks:**

1. **Database Migration Testing:**
   - Run migration on test database
   - Verify schema changes
   - Test rollback capability (backup/restore)

2. **API Integration Tests:**
   - Create analysis via API → verify Guid returned
   - Get analysis by Guid → verify success
   - Update analysis by Guid → verify success
   - Delete analysis by Guid → verify success
   - List analyses → verify all have Guid IDs

3. **Client Integration Tests:**
   - Full E2E flow: Create → View → Update → Delete
   - Verify UI displays Guid correctly (or hides it)
   - Verify no broken links or navigation issues

4. **Performance Testing:**
   - Compare query performance (Guid vs int)
   - Verify indexes work correctly
   - Check for any performance regressions

**Validation:**
- All integration tests pass
- No data loss
- Performance acceptable
- Client fully functional

---

### Phase 8: Documentation & Deployment (Day 7)

**Tasks:**

1. **Update Documentation:**
   - API documentation (Swagger)
   - Architecture diagrams (if showing ID types)
   - Database schema documentation
   - This migration plan (mark as completed)
   - COMPREHENSIVE_REFACTOR_PLAN.md (mark task complete)

2. **Update CHANGELOG:**
   ```markdown
   ## [Unreleased]
   ### Changed
   - **BREAKING:** Analysis entity ID changed from int to Guid for consistency with other entities
   - API endpoints now accept Guid for analysis operations
   - Client types updated to use string (Guid) for analysis IDs
   ```

3. **Deployment Steps:**
   ```bash
   # 1. Backup production database
   pg_dump -h localhost -U postgres -d normaize_db > backup_$(date +%Y%m%d_%H%M%S).sql
   
   # 2. Deploy server with migration
   cd src/Normaize.DataNormalization.API
   dotnet ef database update --connection "..." --startup-project .
   
   # 3. Verify migration
   # Check that analyses table has uuid column
   
   # 4. Deploy server application
   
   # 5. Deploy client application
   
   # 6. Smoke test
   # - Create new analysis
   # - Fetch existing analyses
   # - Update analysis
   # - Delete analysis
   ```

4. **Rollback Plan:**
   ```bash
   # If issues occur:
   # 1. Stop applications
   # 2. Restore database from backup
   # 3. Revert to previous application versions
   # 4. Investigate issues
   ```

**Validation:**
- Documentation updated
- Deployment successful
- No errors in production logs
- All smoke tests pass

---

## ⚠️ Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Data loss during migration | High | Low | Full database backup before migration |
| Client breaks after deployment | High | Medium | Deploy server and client together, thorough testing |
| Performance degradation | Medium | Low | Index on Guid column, performance testing |
| Rollback complexity | High | Low | Document rollback procedure, maintain backups |
| Compilation errors missed | Medium | Low | Full solution build before commit |
| Test failures in CI/CD | Medium | Low | Run full test suite locally first |
| Foreign key constraints break | Medium | Medium | Verify no other tables reference analyses.id before migration |

---

## 📊 Success Criteria

- ✅ All 26+ files updated successfully
- ✅ Zero compilation errors
- ✅ All tests pass (>300 tests across all layers)
- ✅ Database migration completes without errors
- ✅ API accepts and returns Guid IDs
- ✅ Client works with Guid IDs
- ✅ No data loss
- ✅ Performance acceptable (no significant regression)
- ✅ Documentation updated
- ✅ Team informed and trained on changes

---

## 📋 Checklist

### Pre-Migration
- [ ] Review this plan with team
- [ ] Create feature branch
- [ ] Backup database
- [ ] Document current schema
- [ ] Notify stakeholders

### Implementation
- [ ] Phase 1: Preparation
- [ ] Phase 2: Domain Layer Changes
- [ ] Phase 3: Application Layer Changes
- [ ] Phase 4: Infrastructure Layer Changes
- [ ] Phase 5: API Layer Changes
- [ ] Phase 6: Client Updates
- [ ] Phase 7: Integration Testing
- [ ] Phase 8: Documentation & Deployment

### Post-Migration
- [ ] Verify all systems operational
- [ ] Monitor logs for errors
- [ ] Update project tracking
- [ ] Mark task complete in COMPREHENSIVE_REFACTOR_PLAN.md
- [ ] Team retrospective

---

## 🔄 Rollback Procedure

If critical issues are discovered after deployment:

1. **Stop Applications:**
   ```bash
   # Stop server and client
   ```

2. **Restore Database:**
   ```bash
   # Drop current database
   dropdb normaize_db
   
   # Restore from backup
   createdb normaize_db
   psql normaize_db < backup_YYYYMMDD_HHMMSS.sql
   ```

3. **Revert Code:**
   ```bash
   git checkout main
   # Redeploy previous versions
   ```

4. **Verify:**
   - Check database has int IDs
   - Verify API works with int IDs
   - Confirm client functional

5. **Post-Mortem:**
   - Document what went wrong
   - Update migration plan
   - Schedule retry

---

## 📚 Reference

### Similar Migrations in Codebase
- Initial DDD migration converted many entities to Guid
- Migration: `20251024004931_initial_ddd_migration.cs`
- Pattern to follow for consistency

### Related Documentation
- [COMPREHENSIVE_REFACTOR_PLAN.md](./COMPREHENSIVE_REFACTOR_PLAN.md) - Overall refactor strategy
- [DDD_MIGRATION_STANDARDS.md](./DDD_MIGRATION_STANDARDS.md) - Coding standards
- [DDD_MIGRATION_PLAN.md](./DDD_MIGRATION_PLAN.md) - DDD migration approach

### External References
- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL UUID Type](https://www.postgresql.org/docs/current/datatype-uuid.html)
- [ASP.NET Core Model Binding](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding)

---

## 📝 Notes

**Performance Considerations:**
- Guid IDs are larger (16 bytes vs 4 bytes)
- Index size will increase
- Clustering behavior different (non-sequential)
- Consider using `gen_random_uuid()` or application-generated UUIDs

**Alternative Approaches Considered:**
- Keep int ID, use Guid for external APIs: Rejected due to complexity
- Use composite key: Rejected due to domain model simplicity needs
- Implement API versioning: Rejected due to overkill for internal app

**Team Decisions:**
- Agreed to clean break migration (Option A)
- Coordinate server + client deployment
- Accept that old IDs cannot be preserved
- Prioritize simplicity over backward compatibility

---

**Document Status:** Draft → Ready for Review  
**Next Steps:** Review with team → Begin implementation  
**Estimated Effort:** 7 days (with testing and client updates)  
**Actual Effort:** TBD
