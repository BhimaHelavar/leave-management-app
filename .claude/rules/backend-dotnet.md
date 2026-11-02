---
paths:
  - "src/**/*.cs"
---

# Back-end rules (.NET 10, C# 14)

- File-scoped namespaces. Nullable reference types are on. Warnings are errors, so fix them, do not suppress them.
- Domain references nothing. Application references Domain only. Infrastructure and Api may reference inward. Never reference outward.
- Endpoints stay thin: bind and validate the request, call a use case, return `Results.*`. Business rules live in Domain or Application, not in endpoint lambdas.
- Group endpoints per feature in their own file with a `Map...` extension method.
- Use `record` types for request and response DTOs. Never expose an entity directly from an endpoint.
- Every async method takes a `CancellationToken` and passes it on.
- Read-only queries use `AsNoTracking()`.
- Time comes from `TimeProvider`. Leave days are `DateOnly`; timestamps are UTC.
- Errors: return `ProblemDetails` (`Results.Problem` or `Results.NotFound`). Never return stack traces or SQL text. Use 409 for conflicts, 422 for rule failures, 404 for records the caller may not see.
- Protected endpoints use a named policy from `Policies`, never raw role strings. Resource checks happen in addition to the policy.
- Do not log tokens, emails or free text. Log ids.
- Public members of a project get an XML summary only when the name does not already explain them. Comments explain why, not what.
- No new NuGet package without asking the owner.
