# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| Latest | ✅ |
| Previous major | ✅ |
| Older versions | ❌ |

## Reporting a vulnerability

Please **do not** disclose security issues in public GitHub issues or discussions.

Report vulnerabilities by opening a **GitHub issue using the [Security Report template](.github/ISSUE_TEMPLATE/security_report.yml)**:

1. Go to [Issues → New Issue](../../issues/new/choose).
2. Select **Security Report**.
3. Fill in the structured fields — describe the vulnerability without including:
   - Live AWS access keys, secret access keys, or session tokens
   - Actual secret values from your Secrets Manager
   - Private keys or credentials
   - Any sensitive material not strictly necessary to understand the problem

Issues created with the Security Report template are visible to repository maintainers only.

## Response expectations

You can expect:
- **Initial acknowledgement:** Within 2–3 business days
- **Assessment & fix:** Depending on severity:
  - **Critical:** Fix released within 1–2 weeks
  - **High:** Fix released within 1 month
  - **Medium/Low:** Fix released in next planned release (typically 1–2 months)

## Security best practices for users

When using this library:

1. **Use IAM roles / Instance profiles** — Never embed AWS credentials in code or configuration files
2. **Principle of least privilege** — Grant only the minimum required IAM permissions (`secretsmanager:GetSecretValue`, etc.)
3. **Encrypt in transit** — The library uses HTTPS by default; ensure TLS verification is enabled
4. **Monitor access** — Enable CloudTrail and AWS Secrets Manager access logging
5. **Rotate credentials** — Regularly rotate secrets stored in AWS Secrets Manager
6. **Update the package** — Keep the library up to date to receive security fixes

## Known limitations

- The library does not perform client-side encryption of secrets after retrieval — ensure your application handles sensitive values securely
- AWS Secrets Manager enforces API rate limits; see [AWS documentation](https://docs.aws.amazon.com/secretsmanager/latest/userguide/service-limits.html)
- IAM policies are the primary security boundary; the library enforces no additional validation on secret names or values
