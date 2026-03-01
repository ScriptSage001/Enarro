using CoreKernel.Messaging.Commands;

namespace Enarro.Application.Documents.Commands;

public sealed record DeleteDocumentCommand(string DocumentId) : ICommand;
