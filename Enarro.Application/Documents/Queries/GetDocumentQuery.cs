using CoreKernel.Messaging.Queries;
using Enarro.Application.Models;

namespace Enarro.Application.Documents.Queries;

public sealed record GetDocumentQuery(string DocumentId) : IQuery<DocumentModel>;
