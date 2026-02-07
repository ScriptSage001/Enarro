# Enarro - Contributing Guide

Thank you for your interest in contributing to Enarro! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or screenshots

### Suggesting Features

Feature requests are welcome! Please create an issue with:
- Clear description of the feature
- Use case and benefits
- Potential implementation approach (optional)

### Pull Requests

1. **Fork the repository**
   ```bash
   git clone https://github.com/yourusername/enarro.git
   cd enarro
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow the existing code style
   - Add tests for new functionality
   - Update documentation as needed
   - Ensure all tests pass

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add amazing feature"
   ```

   Follow [Conventional Commits](https://www.conventionalcommits.org/):
   - `feat:` - New feature
   - `fix:` - Bug fix
   - `docs:` - Documentation changes
   - `refactor:` - Code refactoring
   - `test:` - Adding tests
   - `chore:` - Maintenance tasks

5. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request**
   - Provide a clear description
   - Reference any related issues
   - Ensure CI checks pass

## Development Setup

### Prerequisites
- .NET 10 SDK
- Docker Desktop
- Ollama
- Git

### Local Development

1. **Clone and restore**
   ```bash
   git clone https://github.com/yourusername/enarro.git
   cd enarro
   dotnet restore
   ```

2. **Pull Ollama models**
   ```bash
   ollama pull phi3
   ollama pull nomic-embed-text
   ```

3. **Run with Aspire**
   ```bash
   dotnet run --project Enarro.AppHost
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

### Code Style

- Use C# 12 features appropriately
- Follow Microsoft's C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

### Testing

- Write unit tests for new functionality
- Ensure existing tests pass
- Aim for high code coverage
- Test edge cases and error conditions

## Project Structure

```
Enarro/
├── Enarro/                  # Main API project
│   ├── Data/               # EF Core entities and DbContext
│   ├── Endpoints/          # Minimal API endpoints
│   ├── Models/             # DTOs and models
│   ├── Services/           # Business logic services
│   └── Program.cs          # Application entry point
├── Enarro.AppHost/         # Aspire orchestration
├── Enarro.ServiceDefaults/ # Shared service configurations
└── README.md
```

## Database Migrations

When making database schema changes:

```bash
cd Enarro
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

## Documentation

- Update README.md for user-facing changes
- Update code comments for implementation details
- Add examples for new features
- Update API documentation

## Review Process

All pull requests require:
- Code review from at least one maintainer
- Passing CI/CD checks
- Updated documentation
- Appropriate tests

## Questions?

Feel free to:
- Open an issue for questions
- Start a discussion in GitHub Discussions
- Reach out to maintainers

Thank you for contributing to Enarro! 🎉
