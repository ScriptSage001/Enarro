using CoreKernel.Messaging.Commands;
using Enarro.Application.Chat.Models;

namespace Enarro.Application.Chat.Commands;

public sealed record SendMessageCommand(
    string Message,
    string? SessionId = null,
    IDictionary<string, string>? Filters = null,
    double MinRelevance = 0.3,
    int MaxResults = 5) : ICommand<ChatResultModel>;
