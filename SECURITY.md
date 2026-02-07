# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability within Enarro, please send an email to the maintainers. All security vulnerabilities will be promptly addressed.

**Please do not report security vulnerabilities through public GitHub issues.**

### What to Include

When reporting a vulnerability, please include:

- Type of vulnerability
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the vulnerability, including how an attacker might exploit it

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Fix Timeline**: Depends on severity and complexity

### Disclosure Policy

- Security issues will be disclosed after a fix is available
- Credit will be given to the reporter (unless they wish to remain anonymous)
- A security advisory will be published on GitHub

## Security Best Practices

When deploying Enarro:

1. **Use HTTPS** - Always use TLS/SSL in production
2. **Secure Secrets** - Never commit secrets to version control
3. **Update Dependencies** - Keep all NuGet packages up to date
4. **Database Security** - Use strong passwords and restrict network access
5. **API Keys** - Rotate API keys regularly
6. **Health Checks** - Monitor health endpoints for anomalies
7. **Logging** - Review logs regularly for suspicious activity
8. **Rate Limiting** - Implement rate limiting to prevent abuse

## Known Security Considerations

### Current Implementation

- **Authentication**: Phase 4 will add JWT authentication (currently not implemented)
- **Authorization**: Phase 4 will add role-based access control
- **Rate Limiting**: Phase 4 will add API rate limiting
- **Input Validation**: Basic validation is implemented, enhanced validation planned

### Recommendations for Production

Until Phase 4 is complete, we recommend:

1. Deploy behind a reverse proxy with authentication
2. Use network-level access controls
3. Implement rate limiting at the infrastructure level
4. Monitor API usage closely
5. Restrict file upload sizes and types

## Dependencies

We regularly update dependencies to patch security vulnerabilities. Run:

```bash
dotnet list package --vulnerable
```

To check for vulnerable packages.

## Contact

For security concerns, please contact the project maintainers through GitHub issues (for non-sensitive matters) or via email for sensitive security issues.
