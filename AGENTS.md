# AGENTS.md — Coding-agent guide for Kralizek.Extensions.Configuration.AWSSecretsManager

This file is the operational reference for automated coding agents working in this repository.
It is not a replacement for [`README.md`](README.md) or [`CONTRIBUTING.md`](CONTRIBUTING.md) — read those first for background.

---

## 1. Repository purpose

**Kralizek.Extensions.Configuration.AWSSecretsManager** is a .NET configuration provider that integrates AWS Secrets Manager with `Microsoft.Extensions.Configuration`. The library offers three explicit loading modes:

- **Discovery**: List all secrets and batch-fetch their values (for zero-config, all-secrets scenarios)
- **KnownSecrets**: Batch-fetch a fixed set of known secrets (for multi-secret scenarios)
- **KnownSecret**: Fetch exactly one secret (for single-secret scenarios)

The library is designed for high-performance scenarios with built-in telemetry, structured logging, and minimal API calls.

See [`README.md`](README.md) for usage examples and [`MIGRATION.md`](MIGRATION.md) for breaking change history.

---

## 2. Repository layout

```
src/
  Kralizek.Extensions.Configuration.AWSSecretsManager/
    ├── SecretsManagerExtensions.cs          ← Entry point; contains AddSecretsManager* methods
    ├── SecretsManagerDiscoveryOptions.cs    ← Discovery mode configuration
    ├── SecretsManagerKnownSecrets*.cs       ← KnownSecrets mode configuration
    ├── SecretValueContext.cs                ← Shared secret context
    ├── SecretsManagerLogging.cs             ← Structured logging definitions
    ├── SecretsManagerTelemetry.cs           ← Telemetry / OpenTelemetry integration
    ├── DuplicateKeyHandling.cs              ← Enum for duplicate key strategies
    ├── MissingSecretValueException.cs       ← Custom exception
    └── Internal/                            ← Implementation details (not part of public API)

tests/
  Tests.Extensions.Configuration.AWSSecretsManager/
    ├── SecretsManagerExtensionsTests.cs     ← Integration tests for all modes
    ├── ConfigurationProviderExtensions.cs   ← Test utility extensions
    └── Internal/                            ← Internal test fixtures
    └── Types/                               ← Test data types

samples/
  Sample1/ through Sample8/                  ← Individual mode demonstrations
  SampleWeb/                                 ← ASP.NET Core integration example

docs/
  (If present) Architecture, design decisions, limitations
```

---

## 3. Before making changes

Depending on the task, inspect the relevant files before touching any code:

| Task type | Files to read first |
|-----------|---------------------|
| New loading mode | `README.md` (Limitations section), `SecretsManagerExtensions.cs`, `tests/` |
| Add Discovery option | `SecretsManagerDiscoveryOptions.cs`, `README.md` §Discovery |
| Add KnownSecrets option | `SecretsManagerKnownSecrets*.cs`, `README.md` §KnownSecrets |
| Add KnownSecret option | `SecretsManagerExtensions.cs`, `README.md` §KnownSecret |
| Error handling / exception | `MissingSecretValueException.cs`, `SecretsManagerLogEvents.cs` |
| Telemetry / observability | `SecretsManagerTelemetry.cs`, `SecretsManagerLogging.cs` |
| Breaking change | `MIGRATION.md`, `README.md` |
| Sample/documentation | `samples/`, `README.md` |

---

## 4. Working rules

- **Keep PRs small and focused** — one logical change per PR
- **No unrelated refactors** — fix only what the task requires
- **Test all three modes** — if a change affects the core API or configuration, ensure it works across Discovery, KnownSecrets, and KnownSecret
- **Preserve backwards compatibility** unless the task explicitly requires a breaking change (document in `MIGRATION.md` and update version)
- **Extend existing patterns** (e.g., new option in existing `*Options.cs` class) rather than inventing new ones
- **Update docs and tests when behaviour changes**:
  - If public API changes, update `README.md` and XML docs
  - If logging/telemetry changes, update `SecretsManagerLogging.cs` or `SecretsManagerTelemetry.cs`
  - If a mode's behaviour changes, update the corresponding sample
  - If a breaking change, update `MIGRATION.md`
- **AWS API efficiency** — minimize redundant AWS Secrets Manager calls; batch operations where possible

---

## 5. Required validation before finishing

Run all checks from the repository root before declaring work done:

```bash
# 1. Format validation
dotnet format --verify-no-changes

# 2. Build — warnings are treated as errors
dotnet build --no-incremental -warnaserror

# 3. All tests
dotnet test

# 4. Check code analysis (optional, if enabled)
dotnet analyze
```

For changes affecting a specific mode, also run the corresponding sample:

```bash
# For Discovery mode changes
cd samples/Sample1
dotnet run

# For KnownSecrets mode changes
cd samples/Sample2
dotnet run

# For KnownSecret mode changes
cd samples/Sample3
dotnet run

# For web integration
cd samples/SampleWeb
dotnet run
```

---

## 6. Task-specific guidance

### Configuration changes (new option, new mode)

- Add the option class or extend an existing `*Options.cs` file
- Add the extension method to `SecretsManagerExtensions.cs`
- Add integration tests to `SecretsManagerExtensionsTests.cs`
- Add a sample demonstrating the new option (or update an existing sample)
- Update `README.md` with usage examples and any new limitations

### Error handling / validation

- Add custom exception if needed (extend or add to `MissingSecretValueException.cs`)
- Log using structured logging from `SecretsManagerLogging.cs`
- Test both success and failure paths in `SecretsManagerExtensionsTests.cs`

### Telemetry / observability

- Add metrics or traces to `SecretsManagerTelemetry.cs` if emitting OpenTelemetry data
- Add log events to `SecretsManagerLogging.cs` if emitting structured logs
- Test that telemetry/logs are emitted without breaking functionality

### Logging / diagnostic changes

- Update or add log event definitions in `SecretsManagerLogging.cs`
- Use the structured logger pattern from existing code
- Test that logs are emitted at the correct levels and include relevant context

### Breaking changes

- Document the change in `MIGRATION.md` with a clear "before/after" example
- Update version in `Directory.Build.props` (major version bump)
- Update `README.md` if the change affects public API
- Run all samples to ensure they still work (or update them to match the new API)

### Dependency updates

- Update `Directory.Packages.props` for new versions
- Ensure no breaking changes in upstream packages (especially `AWSSDK.SecretsManager`)
- Verify all tests still pass after the update

### Changelog policy

The changelog describes **consumer-visible changes only** in [Keep a Changelog](https://keepachangelog.com/) format.
See [`CHANGELOG.md`](CHANGELOG.md) for the full structure and release history.

**Changelog sections** (use these exactly as they appear):
- **Added** — New features, options, or modes
- **Changed** — Changes to existing features (backwards compatible)
- **Deprecated** — Features marked for removal in a future major version
- **Removed** — Features or APIs that were previously deprecated
- **Fixed** — Bug fixes
- **Security** — Security vulnerabilities and fixes

Unreleased work is captured in `## [Unreleased]` at the top of `CHANGELOG.md`. When releasing a new version:
1. Rename `## [Unreleased]` to `## [X.Y.Z] — YYYY-MM-DD` at the top
2. Create a new `## [Unreleased]` section below it (empty, for the next release)

**Before touching `CHANGELOG.md`, ask two questions in order:**

1. **Is the change visible to package consumers?** If not, skip the changelog entirely.
2. **Is the change already represented by an existing entry?** If so, update or rewrite that entry instead of adding a new one.

**Do not add changelog items for:**
- documentation-only changes
- CI/CD or workflow changes
- test-only changes
- refactorings with no consumer-visible effect
- internal implementation cleanup
- repository structure changes with no consumer impact
- dependency updates that do not affect consumers

**Add a new changelog item only when** the change introduces a genuinely new consumer-visible capability or behavior not already represented by an existing entry.

---

## 7. Completion checklist

Before closing the task, confirm all of the following:

- [ ] `dotnet format --verify-no-changes` passes
- [ ] `dotnet build --no-incremental -warnaserror` passes with zero warnings
- [ ] `dotnet test` — all tests pass
- [ ] New behaviour is covered by tests (unit tests or integration tests as appropriate)
- [ ] `README.md`, `MIGRATION.md` (if breaking), and XML doc comments are updated if public behaviour changed
- [ ] `CHANGELOG.md` updated only if the change is consumer-visible and not already represented by an existing entry (see changelog policy in §6)
- [ ] Relevant sample app(s) run and demonstrate the feature/fix
- [ ] No unrelated files are modified
- [ ] Commit message is clear and concise
- [ ] For AWS-specific changes, consider regional behaviour, IAM policy implications, and API rate limits

---

## 8. Common scenarios

### Adding a new filtering option to Discovery mode

1. Add a new property to `SecretsManagerDiscoveryOptions.cs`
2. Implement the filtering logic in the internal provider class
3. Update the sample (e.g., `Sample1` or create a new sample)
4. Add test cases to `SecretsManagerExtensionsTests.cs`
5. Update `README.md` with the new option and example

### Fixing a bug in KnownSecrets batch-fetch

1. Write a failing test case in `SecretsManagerExtensionsTests.cs`
2. Implement the fix in the internal provider
3. Ensure the fix also applies to KnownSecret and Discovery modes (if relevant)
4. Run all samples to verify no regression
5. Update `MIGRATION.md` if the fix involves a behaviour change visible to users

### Adding OpenTelemetry support

1. Add metrics/traces to `SecretsManagerTelemetry.cs`
2. Add test(s) to verify telemetry is emitted
3. Document the new metrics in `README.md`
4. Run `samples/SampleWeb` and verify telemetry is visible in your observability tool

---

## 9. AWS-specific considerations

- **Rate limiting**: Discovery mode calls `ListSecrets` and `BatchGetSecretValue`; be aware of AWS API rate limits
- **IAM policies**: Document which permissions are required for each mode in `README.md`
- **Regions**: The library respects the configured `RegionEndpoint`; test changes across regions if they touch region-specific logic
- **Credentials**: In tests, use LocalStack or clearly mark integration tests as requiring AWS credentials
- **Cost**: Discovery mode may incur more API calls; document this trade-off in `README.md`

---

## 10. Questions?

Refer back to:
- `README.md` for usage and API surface
- `MIGRATION.md` for history and breaking changes
- `CONTRIBUTING.md` for general contribution guidelines
- `SecretsManagerExtensions.cs` for the public API entry points
