---
name: new-migration
description: Create a new EF Core migration for a SQL Server schema change. Use when adding tables, columns, indexes, or constraints.
disable-model-invocation: true
---

Create a new EF Core migration for: $ARGUMENTS

1. Update the relevant entity class(es) in src/API/Core/Entities/
2. Update ApplicationDbContext.cs if needed (DbSet, configurations)
3. Run: dotnet ef migrations add <DescriptiveName> --project src/API
4. Review the generated migration file — confirm it matches the intended change
5. Update docs/schema-overview.md with the change
6. Do not apply the migration — confirm with the developer first
