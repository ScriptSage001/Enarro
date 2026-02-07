# Changelog

All notable changes to Enarro will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of Enarro RAG application
- Multi-document batch upload with parallel processing
- Context-aware chat with conversation history
- Streaming chat responses using Server-Sent Events
- Document management APIs (CRUD operations)
- PostgreSQL integration for document metadata persistence
- Redis distributed caching for session management
- Serilog structured logging with console and file outputs
- Comprehensive health checks for all dependencies
- .NET Aspire infrastructure orchestration
- Semantic search with Qdrant vector database
- LLM integration with Ollama (phi3, nomic-embed-text)
- Source citation extraction with relevance scoring
- API versioning support
- Swagger/OpenAPI documentation

### Infrastructure
- PostgreSQL container with persistent volumes
- Redis container with persistent volumes
- Qdrant vector database container
- Ollama LLM container
- Automatic database migrations on startup
- Health monitoring dashboard

### Documentation
- Comprehensive README with quick start guide
- API documentation with examples
- Contributing guidelines
- MIT License
- Architecture diagrams

## [1.0.0] - 2026-02-07

### Initial Release
- Production-ready RAG application with .NET 10
- Full .NET Aspire orchestration
- PostgreSQL, Redis, Qdrant, and Ollama integration
