# Quick Setup Guide

This guide will help you get Enarro up and running quickly.

## Prerequisites

Before you begin, ensure you have the following installed:

- ✅ [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- ✅ [Docker Desktop](https://www.docker.com/products/docker-desktop) (running)
- ✅ [Ollama](https://ollama.ai/)
- ✅ Git

## Step-by-Step Setup

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/enarro.git
cd enarro
```

### 2. Install Ollama Models

```bash
# Pull the chat model (phi3)
ollama pull phi3

# Pull the embedding model
ollama pull nomic-embed-text

# Verify models are installed
ollama list
```

### 3. Restore NuGet Packages

```bash
dotnet restore
```

### 4. Start the Application

```bash
# Run with .NET Aspire (recommended)
dotnet run --project Enarro.AppHost
```

This will:
- ✅ Start PostgreSQL container
- ✅ Start Redis container
- ✅ Start Qdrant container
- ✅ Start Ollama container
- ✅ Run database migrations
- ✅ Launch the API
- ✅ Open Aspire dashboard

### 5. Access the Application

Once started, you can access:

| Service | URL |
|---------|-----|
| **API** | https://localhost:7001 |
| **Swagger UI** | https://localhost:7001/swagger |
| **Aspire Dashboard** | https://localhost:15888 |
| **Health Checks** | https://localhost:7001/health |

## Quick Test

### Upload a Document

```bash
curl -X POST "https://localhost:7001/api/v1/ingest" \
  -H "accept: application/json" \
  -H "Content-Type: multipart/form-data" \
  -F "file=@/path/to/your/document.pdf"
```

### Ask a Question

```bash
curl -X POST "https://localhost:7001/api/v1/chat" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is this document about?",
    "minRelevance": 0.3
  }'
```

### Check Health

```bash
curl https://localhost:7001/health
```

## Troubleshooting

### Docker Not Running
```
Error: Docker is not running
```
**Solution**: Start Docker Desktop

### Ollama Models Not Found
```
Error: Model 'phi3' not found
```
**Solution**: Run `ollama pull phi3` and `ollama pull nomic-embed-text`

### Port Already in Use
```
Error: Port 7001 is already in use
```
**Solution**: Stop the conflicting application or change the port in `Enarro/Properties/launchSettings.json`

### Database Migration Errors
```
Error: Unable to create migration
```
**Solution**: Ensure PostgreSQL container is running and connection string is correct

## Next Steps

1. **Explore the API** - Visit https://localhost:7001/swagger
2. **Monitor Services** - Check Aspire dashboard at https://localhost:15888
3. **Upload Documents** - Try uploading different document types
4. **Test Chat** - Ask questions about your uploaded documents
5. **Check Logs** - View logs in `logs/enarro-[date].log`

## Configuration

To customize the application, edit `Enarro/appsettings.json`:

```json
{
  "RAGConfigs": {
    "ChatModel": "phi3",
    "EmbeddingModel": "nomic-embed-text",
    "Retrieval": {
      "MinRelevance": 0.3,
      "MaxResults": 5
    }
  }
}
```

## Development Mode

For development with hot reload:

```bash
dotnet watch --project Enarro
```

## Stopping the Application

Press `Ctrl+C` in the terminal where Aspire is running.

To stop and remove all containers:

```bash
docker compose down
```

## Need Help?

- 📖 Read the full [README.md](README.md)
- 🐛 Report issues on [GitHub Issues](https://github.com/yourusername/enarro/issues)
- 💬 Join discussions on [GitHub Discussions](https://github.com/yourusername/enarro/discussions)

---

**Happy coding! 🚀**
