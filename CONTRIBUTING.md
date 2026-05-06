# Contributing

Contributions are welcome — thanks for taking the time!

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (see `global.json` for the required version)
- A C# IDE or editor (Visual Studio, Rider, VS Code with C# Dev Kit)
- AWS account or local AWS emulation (e.g., LocalStack) for testing

## Building locally

```bash
dotnet build --no-incremental -warnaserror
```

The build enforces `--warnaserror`, so zero warnings are expected. Fix any warnings before opening a PR.

## Running the tests

```bash
dotnet test
```

This runs all unit and integration tests. Make sure everything passes before submitting.

## Running the sample apps

```bash
cd samples/Sample1
dotnet run
```

The samples demonstrate different loading modes (Discovery, KnownSecrets, KnownSecret) and are a good way to verify end-to-end behaviour after changes.

## Pull request expectations

- **Open an issue first** for anything non-trivial so we can agree on the approach before you invest time coding
- Keep changes **small and focused** — one logical change per PR
- Update or add tests for any changed behaviour
- Update documentation (README, XML docs, MIGRATION guide) if relevant
- The CI pipeline must be green before a PR can be merged
- Build must pass with `--warnaserror` (zero warnings)

## Code style

The repository includes an `.editorconfig`. Your IDE should pick it up automatically.

## Testing with AWS Secrets Manager

For integration testing:

1. **Use LocalStack** or a local AWS emulation for development:
   ```bash
   docker run -d -p 4566:4566 localstack/localstack:latest
   ```
   Then configure the client to use the LocalStack endpoint.

2. **Or use a real AWS account** with test secrets in a dev region:
   ```csharp
   var client = new AmazonSecretsManagerClient(RegionEndpoint.USEast1);
   // Ensure the client is configured with dev-only credentials
   ```

## Automation and coding agents

If you are an automated coding agent, also read [`AGENTS.md`](AGENTS.md) for the operational guide specific to this repository.

## Reporting issues

Please use the issue templates in `.github/ISSUE_TEMPLATE/` when opening bugs or feature requests. When reporting bugs, include:
- Package version
- .NET SDK version
- AWS region (if relevant)
- Loading mode (Discovery, KnownSecrets, KnownSecret)
- Minimal reproduction steps
- AWS IAM policy (redacted) if relevant to the issue
