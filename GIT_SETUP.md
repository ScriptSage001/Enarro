# Git Commands for Initial Commit

This file contains the commands to initialize your Git repository and push to GitHub.

## Step 1: Initialize Git Repository

```bash
# Navigate to project directory
cd d:\Programming\CodeBase\Enarro

# Initialize git repository
git init

# Add all files
git add .

# Create initial commit
git commit -m "Initial commit: Production-ready RAG application with .NET Aspire

- Multi-document batch upload with parallel processing
- Context-aware chat with conversation history
- Streaming chat responses using SSE
- PostgreSQL integration for document metadata
- Redis distributed caching for sessions
- Serilog structured logging
- Comprehensive health checks
- Full .NET Aspire orchestration"
```

## Step 2: Create GitHub Repository

1. Go to https://github.com/new
2. Create a new repository named `enarro`
3. **DO NOT** initialize with README, .gitignore, or license (we already have these)
4. Copy the repository URL

## Step 3: Push to GitHub

```bash
# Add remote origin (replace with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/enarro.git

# Rename branch to main (if needed)
git branch -M main

# Push to GitHub
git push -u origin main
```

## Step 4: Verify

Visit your GitHub repository to verify all files were pushed successfully.

## Optional: Create Development Branch

```bash
# Create and switch to development branch
git checkout -b develop

# Push development branch
git push -u origin develop
```

## Future Commits

For future changes:

```bash
# Check status
git status

# Add changes
git add .

# Commit with message
git commit -m "feat: your feature description"

# Push to GitHub
git push
```

## Conventional Commit Messages

Use these prefixes for commit messages:

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `refactor:` - Code refactoring
- `test:` - Adding tests
- `chore:` - Maintenance tasks
- `perf:` - Performance improvements

Example:
```bash
git commit -m "feat: add JWT authentication support"
git commit -m "fix: resolve session timeout issue"
git commit -m "docs: update API documentation"
```

---

**Note**: Delete this file after pushing to GitHub, or add it to .gitignore
