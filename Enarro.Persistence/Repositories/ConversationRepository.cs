using Enarro.Application.Abstractions;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Conversation;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence.Repositories;

public class ConversationRepository(EnarroDbContext db) : IConversationRepository
{
    // ─── Sessions ────────────────────────────────────────────────────────

    public async Task<SessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var entity = await db.ConversationSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        return entity is null ? null
            : new SessionRecord(entity.SessionId, entity.UserId, entity.Title, entity.CreatedAt, entity.UpdatedAt);
    }

    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken ct = default) =>
        db.ConversationSessions.AnyAsync(s => s.SessionId == sessionId, ct);

    public async Task AddSessionAsync(SessionRecord session, CancellationToken ct = default)
    {
        var entity = ConversationSession.Create(session.SessionId, session.UserId, session.CreatedAt);
        entity.Title = session.Title;

        db.ConversationSessions.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken ct = default) =>
        await db.ConversationSessions
            .Where(s => s.SessionId == sessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Title, title), ct);

    public async Task TouchSessionAsync(string sessionId, DateTime updatedAt, CancellationToken ct = default) =>
        await db.ConversationSessions
            .Where(s => s.SessionId == sessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, updatedAt), ct);

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var deleted = await db.ConversationSessions
            .Where(s => s.SessionId == sessionId)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(
        Guid userId, CancellationToken ct = default) =>
        await BuildSessionSummaryQuery(userId).ToListAsync(ct);

    public async Task<PagedResult<SessionSummaryModel>> GetUserSessionsPagedAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildSessionSummaryQuery(userId);
        var total = await db.ConversationSessions.CountAsync(s => s.UserId == userId, ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SessionSummaryModel>(items, total, page, pageSize);
    }

    // ─── Messages ────────────────────────────────────────────────────────

    public async Task AddMessageAsync(MessageRecord message, CancellationToken ct = default)
    {
        var entity = ConversationMessage.Create(
            message.SessionId, message.Role, message.Content, message.CreatedAt);

        db.ConversationMessages.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> GetMessageCountAsync(string sessionId, string role, CancellationToken ct = default) =>
        db.ConversationMessages
            .CountAsync(m => m.SessionId == sessionId && m.Role == role, ct);

    public async Task<IReadOnlyList<MessageRecord>> GetRecentMessagesAsync(
        string sessionId, int limit, CancellationToken ct = default) =>
        await db.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt) // restore ascending after top-N
            .AsNoTracking()
            .Select(m => new MessageRecord(m.SessionId, m.Role, m.Content, m.CreatedAt))
            .ToListAsync(ct);

    public async Task<PagedResult<ConversationMessageModel>> GetFullHistoryAsync(
        string sessionId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(m => new ConversationMessageModel(m.Role, m.Content, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ConversationMessageModel>(items, total, page, pageSize);
    }

    #region Private Helpers

    private IQueryable<SessionSummaryModel> BuildSessionSummaryQuery(Guid userId) =>
        db.ConversationSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .AsNoTracking()
            .Select(s => new SessionSummaryModel(
                s.SessionId,
                s.Title,
                s.CreatedAt,
                s.Messages.Count(),
                s.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault()));

    #endregion
}