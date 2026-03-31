---
name: run-tests
description: Run the full backend and frontend test suites with coverage reporting.
disable-model-invocation: true
---

Run the full test suite:

1. Run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
2. Report coverage summary per project
3. Flag any service/controller below 80% coverage
4. Run: cd src/ClientApp && npm run test -- --coverage
5. Report Vue component coverage
6. Flag failures with file paths and line numbers
