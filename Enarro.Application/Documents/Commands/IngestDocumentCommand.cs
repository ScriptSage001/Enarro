using CoreKernel.Messaging.Commands;
using Enarro.Application.Models;

namespace Enarro.Application.Documents.Commands;

public sealed record IngestDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? UserId,
    IDictionary<string, string>? Tags = null) : ICommand<DocumentIngestResultModel>;
