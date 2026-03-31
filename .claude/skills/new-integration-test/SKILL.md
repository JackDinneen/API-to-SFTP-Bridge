---
name: new-integration-test
description: Scaffold integration tests for an API endpoint using WebApplicationFactory.
disable-model-invocation: false
---

Scaffold integration tests for: $ARGUMENTS

1. Create test class in tests/IntegrationTests/
2. Set up WebApplicationFactory with test database
3. Test happy path (200/201 response, correct data)
4. Test authentication (401 for unauthenticated)
5. Test authorization (403 for wrong role)
6. Test validation (400 for invalid input)
7. Test not found (404 for missing resources)
8. Clean up test data in Dispose
