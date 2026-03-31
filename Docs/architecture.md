# Architecture Overview

## High-Level Architecture

_To be populated as the platform is built._

## Project Structure

```
src/
  API/
    Controllers/        # API endpoints
    Core/
      Entities/         # Domain models
      DTOs/             # Request/response objects
      Interfaces/       # Service and repository contracts
      Models/           # Shared models (ApiResponse<T>)
    Application/
      Services/         # Business logic
      Validators/       # FluentValidation validators
    Infrastructure/
      Data/             # ApplicationDbContext, EF Core config
      Repositories/     # Data access implementations
      External/         # SFTP, Azure Blob, external API clients
      Migrations/       # EF Core migrations
    Program.cs          # DI registration, middleware pipeline
  ClientApp/
    src/
      components/       # Vue 3 components
      views/            # Page-level components
      stores/           # Pinia stores
      types/            # TypeScript interfaces
      composables/      # Shared composition functions
tests/
  UnitTests/            # xUnit + Moq unit tests
  IntegrationTests/     # WebApplicationFactory integration tests
docs/                   # Project documentation
```

## Key Design Decisions

_To be documented as decisions are made._
