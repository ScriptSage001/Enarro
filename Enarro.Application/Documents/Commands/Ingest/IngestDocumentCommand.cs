using CoreKernel.Messaging.Commands;
using Enarro.Application.Documents.DTOs;

namespace Enarro.Application.Documents.Commands.Ingest;

public sealed record IngestDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? UserId,
    IDictionary<string, string>? Tags = null) : ICommand<DocumentIngestResultDto>;
