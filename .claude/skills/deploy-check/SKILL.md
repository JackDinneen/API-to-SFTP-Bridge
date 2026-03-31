---
name: deploy-check
description: Pre-deployment verification — build, test, lint, type-check.
disable-model-invocation: true
---

Run pre-deployment checks:

1. Run: dotnet build --configuration Release
2. Run: dotnet test
3. Run: cd src/ClientApp && npm run lint
4. Run: cd src/ClientApp && npm run type-check
5. Run: cd src/ClientApp && npm run build
6. Check for pending/unapplied EF Core migrations
7. Check environment variables are documented in docs/env-vars.md
8. Report pass/fail for each step
