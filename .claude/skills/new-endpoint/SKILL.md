---
name: new-endpoint
description: Scaffold a new API endpoint following clean architecture conventions.
disable-model-invocation: false
---

Scaffold a new API endpoint for: $ARGUMENTS

1. Entity in src/API/Core/Entities/
2. Request/response DTOs in src/API/Core/DTOs/
3. Interface in src/API/Core/Interfaces/
4. Service implementation in src/API/Application/Services/
5. Repository in src/API/Infrastructure/Repositories/
6. Register in src/API/Program.cs (DI)
7. Controller action in src/API/Controllers/ — include [Authorize] attribute
8. FluentValidation validator in src/API/Application/Validators/
9. Unit tests in tests/UnitTests/
10. Confirm dotnet test passes before finishing
