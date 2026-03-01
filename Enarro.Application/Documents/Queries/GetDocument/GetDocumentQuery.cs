using CoreKernel.Messaging.Queries;
using Enarro.Application.Documents.DTOs;

namespace Enarro.Application.Documents.Queries.GetDocument;

public sealed record GetDocumentQuery(string DocumentId) : IQuery<DocumentDto>;
