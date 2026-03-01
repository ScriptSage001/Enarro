using CoreKernel.Messaging.Commands;

namespace Enarro.Application.Documents.Commands.Delete;

public sealed record DeleteDocumentCommand(string DocumentId) : ICommand;
