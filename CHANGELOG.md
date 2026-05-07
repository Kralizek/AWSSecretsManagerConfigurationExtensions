# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This changelog tracks **consumer-visible changes only**. Internal refactorings, test-only changes, CI/CD updates, and documentation-only changes are not listed.

For **breaking changes**, see [`MIGRATION.md`](MIGRATION.md).

---

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

---

## [2.0.0] — 2026-05-06

### Added

- Initial release of Kralizek.Extensions.Configuration.AWSSecretsManager (v2.0.0+)
- **Three explicit loading modes**:
  - `AddSecretsManagerDiscovery`: List all secrets via `ListSecrets` + batch-fetch values
  - `AddSecretsManagerKnownSecrets`: Batch-fetch a fixed set of known secrets
  - `AddSecretsManagerKnownSecret`: Fetch exactly one secret
- **Configuration options**:
  - `SecretsManagerDiscoveryOptions`: Filtering by prefix, name pattern, tags, and custom client-side filters
  - `SecretsManagerKnownSecretsOptions`: Batch operation configuration and duplicate key handling strategies
  - `SecretsManagerKnownSecretOptions`: Single-secret fetch with optional transformation
- **Duplicate key handling**: Strategies for resolving conflicting secret keys (Replace, Skip, ThrowOnDuplicate)
- **Structured logging**: Comprehensive logging via `ILogger` with semantic events in `SecretsManagerLogging`
- **OpenTelemetry integration**: Activity tracing and metrics via `SecretsManagerTelemetry`
- **Exception handling**: `MissingSecretValueException` for missing or invalid secrets
- **AWS region support**: Configurable `RegionEndpoint` and `AWSOptions`
- **Credential flexibility**: Support for IAM roles, profiles, and explicit credentials
- **Sample applications**:
  - Sample1–Sample8: Individual demonstrations of Discovery, KnownSecrets, and KnownSecret modes
  - SampleWeb: ASP.NET Core integration example
- **Telemetry capabilities**:
  - Activity tracing for AWS API calls
  - Metrics for call counts, durations, and error rates
  - Integration with Application Insights and other observability platforms

### Technical Details

- **Target frameworks**: 
  - `netstandard2.0` (for broad compatibility)
  - `net10.0` (modern .NET runtime)
- **Dependencies**:
  - `Microsoft.Extensions.Configuration`
  - `AWSSDK.SecretsManager`
  - `Microsoft.Extensions.Logging.Abstractions`
- **API efficiency**:
  - Discovery mode: Uses `ListSecrets` + `BatchGetSecretValue` for optimized multi-secret scenarios
  - KnownSecrets mode: Single batch call for fixed secret sets
  - KnownSecret mode: Single direct call for individual secrets
- **AWS API best practices**: Respects rate limits and implements exponential backoff (via SDK)

---

## Notes

- Pre-release versions prior to 2.0.0 are not tracked in this changelog. For historical context, refer to the repository's git history.
- Version numbers follow [Semantic Versioning](https://semver.org/): MAJOR.MINOR.PATCH
  - **MAJOR**: Breaking API changes
  - **MINOR**: New features (backwards compatible)
  - **PATCH**: Bug fixes (backwards compatible)
