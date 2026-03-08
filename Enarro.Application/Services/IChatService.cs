using Enarro.Application.Models;
using System.Runtime.CompilerServices;

namespace Enarro.Application.Services;

public interface IChatService
{
    IAsyncEnumerable<string> ChatStreamAsync(Guid userId, ChatRequestModel request, CancellationToken cancellationToken);
}