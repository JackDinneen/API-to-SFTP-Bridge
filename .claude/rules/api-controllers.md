---
globs: src/API/Controllers/**
---

## API Controller Rules

- Every controller action MUST have [Authorize] attribute (or [Authorize(Roles = "...")] for role-specific)
- All responses must use ApiResponse<T> wrapper from Core/Models/ApiResponse.cs
- Never put validation logic in controllers — use FluentValidation validators
- Controllers call services only — never access DbContext or repositories directly
- All actions must be async (return Task<ActionResult<ApiResponse<T>>>)
- Use [ProducesResponseType] attributes for Swagger documentation
- Handle exceptions via middleware, not try-catch in controllers
