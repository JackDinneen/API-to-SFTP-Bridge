---
globs: src/API/Application/Services/**
---

## Service Rules

- Every service must implement an interface from Core/Interfaces/
- Register services in Program.cs via DI (AddScoped for request-scoped, AddSingleton for stateless)
- All methods must be async — no synchronous DB calls
- Use ILogger<T> for structured logging — never log sensitive data (credentials, tokens)
- Services call repositories for data access — never use DbContext directly
- Throw specific exceptions, catch in middleware — don't swallow errors
- Use CancellationToken on all async methods
