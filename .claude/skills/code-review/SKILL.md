---
name: code-review
description: Comprehensive code review for quality, security, and project conventions. Run after writing or modifying code.
disable-model-invocation: false
---

Review the recent code changes:

1. Run git diff on modified files
2. Check: clean architecture respected (no DbContext in controllers)
3. Check: all async methods awaited — no .Result or .Wait()
4. Check: FluentValidation used — no inline validation in controllers
5. Check: API responses use ApiResponse<T> wrapper
6. Check: [Authorize] on all controller actions
7. Check: no hardcoded credentials, connection strings, or Azure keys
8. Check: Vue uses <script setup> — not Options API
9. Check: unit tests exist for new/modified code
10. Report: Critical (must fix) > Warnings (should fix) > Suggestions
