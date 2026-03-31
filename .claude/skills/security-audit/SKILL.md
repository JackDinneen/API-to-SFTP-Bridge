---
name: security-audit
description: Security-focused review checking credentials, injection, auth, and encryption.
disable-model-invocation: false
---

Run a security audit on the codebase:

1. Check all API controllers have [Authorize] attribute
2. Check credential storage uses Azure Key Vault — never plaintext
3. Check Dapper queries are parameterised — no string concatenation in SQL
4. Check SFTP credentials never appear in logs or responses
5. Check API keys and secrets never exposed in Vue client code
6. Check EF Core has global query filters where needed
7. Check input validation on all public endpoints
8. Check CORS configuration is restrictive
9. Check no sensitive data in error responses
10. Report findings: Critical > Warnings
