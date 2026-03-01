using CoreKernel.Functional.Results;
using Enarro.Contracts.Chat;

namespace Enarro.Services;

public interface IChatService
{
    Task<Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<string> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken);
}
