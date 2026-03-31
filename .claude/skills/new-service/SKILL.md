---
name: new-service
description: Scaffold a backend service class following the interface pattern with DI and tests.
disable-model-invocation: false
---

Scaffold a new service for: $ARGUMENTS

1. Interface in src/API/Core/Interfaces/I<ServiceName>.cs
2. Implementation in src/API/Application/Services/<ServiceName>.cs
3. Register in src/API/Program.cs (AddScoped/AddTransient)
4. Unit tests in tests/UnitTests/Services/ with Moq
5. Confirm dotnet test passes
