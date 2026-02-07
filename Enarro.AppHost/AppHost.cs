using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL for document metadata
var postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var enarroDb = postgres.AddDatabase("enarro-db");

// Add Redis for distributed caching
var redis = builder
    .AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// Add Qdrant vector database
var qdrant = builder
    .AddQdrant("qdrant")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// Add Ollama for LLM
var ollama = builder
    .AddOllama("ollama")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var chatModelName = builder.Configuration["RAGConfigs:ChatModel"];
var embeddingModelName = builder.Configuration["RAGConfigs:EmbeddingModel"];

var chatModel = ollama.AddModel("chat", chatModelName!);
var embeddingModel = ollama.AddModel("embeddings", embeddingModelName!);

builder
    .AddProject<Enarro>("enarro-api")
    .WithReference(enarroDb)
    .WithReference(redis)
    .WithReference(qdrant)
    .WithReference(ollama)
    .WithReference(chatModel)
    .WithReference(embeddingModel)
    .WaitFor(enarroDb)
    .WaitFor(redis)
    .WaitFor(qdrant)
    .WaitFor(ollama)
    .WaitFor(chatModel)
    .WaitFor(embeddingModel)
    .WithEnvironment("RAGConfigs__ChatModel", chatModelName)
    .WithEnvironment("RAGConfigs__EmbeddingModel", embeddingModelName)
    .WithEnvironment("RAGConfigs__QdrantEndpoint", qdrant.Resource.HttpEndpoint);

builder.Build().Run();