# Samples

Each sample is a minimal standalone console (or web) application that demonstrates one feature of the library. The table below maps every sample to the API mode it showcases.

| Sample | API mode | What it demonstrates |
|---|---|---|
| [Sample1](Sample1/Program.cs) | Discovery | Minimal discovery — default credentials and region |
| [Sample2](Sample2/Program.cs) | Discovery | Discovery with `AWSOptions` (region / profile) |
| [Sample3](Sample3/Program.cs) | Discovery | Discovery with a pre-built `IAmazonSecretsManager` client |
| [Sample4](Sample4/Program.cs) | KnownSecrets | Load a fixed set of secrets by ARN or name — no `ListSecrets` call |
| [Sample5](Sample5/Program.cs) | Discovery | Custom `KeyGenerator` to control configuration key names |
| [Sample6](Sample6/Program.cs) | Discovery | `DuplicateKeyHandling` to resolve key conflicts |
| [Sample7](Sample7/Program.cs) | KnownSecret | Bootstrap logging before the DI container is ready |
| [Sample8](Sample8/Program.cs) | KnownSecret | Least-privilege single secret — only `secretsmanager:GetSecretValue` required |
| [SampleWeb](SampleWeb/Program.cs) | Discovery | ASP.NET Core integration with periodic reload |

## API modes at a glance

| Mode | Extension method | AWS API calls | Minimum IAM permissions |
|---|---|---|---|
| **Discovery** | `AddSecretsManagerDiscovery` | `ListSecrets` + `BatchGetSecretValue` | `secretsmanager:ListSecrets`, `secretsmanager:BatchGetSecretValue` |
| **KnownSecrets** | `AddSecretsManagerKnownSecrets` | `BatchGetSecretValue` | `secretsmanager:BatchGetSecretValue` |
| **KnownSecret** | `AddSecretsManagerKnownSecret` | `GetSecretValue` | `secretsmanager:GetSecretValue` |

> **Least-privilege tip**: If you know the exact ARN or name of every secret your application needs, use `AddSecretsManagerKnownSecret` or `AddSecretsManagerKnownSecrets`. Neither method calls `ListSecrets`, so you can omit the `secretsmanager:ListSecrets` permission from your IAM policy entirely.
