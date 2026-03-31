---
globs: src/API/Infrastructure/Migrations/**
---

## Migration Rules

- Never modify a migration that has been applied to any environment
- Always review generated migration SQL before confirming
- Include appropriate indexes for foreign keys and frequently queried columns
- Use descriptive migration names (e.g., AddConnectionMappingsTable, not Migration1)
- Soft delete where appropriate (IsDeleted flag with global query filter)
- Always set appropriate column lengths for string fields
