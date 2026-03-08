using Enarro.Application.Abstractions;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Conversation;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence.Repositories;

public class ConversationRepository(EnarroDbContext db) : IConversationRepository
{
    #region Sessions

    public async Task<ConversationSession?> GetSessionTrackedAsync(string sessionId, CancellationToken ct = default) =>
        await db.ConversationSessions
                    .Include(x => x.Messages)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

    public async Task<SessionRecord?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var entity = await db.ConversationSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        return entity is null ? null
            : new SessionRecord(entity.SessionId, entity.UserId, entity.Title, entity.CreatedOn, entity.LastModifiedOn);
    }

    public Task<bool> SessionExistsAsync(string sessionId, CancellationToken ct = default) =>
        db.ConversationSessions.AnyAsync(s => s.SessionId == sessionId, ct);

    public void AddSession(SessionRecord session)
    {
        var entity = ConversationSession.Create(session.SessionId, session.UserId, session.Title);
        db.ConversationSessions.Add(entity);
    }

    public async Task UpdateSessionTitleAsync(string sessionId, string title, CancellationToken ct = default)
    {
        var entity = await db.ConversationSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (entity is not null)
        {
            entity.Title = title;
        }
    }

    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var entity = await db.ConversationSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);

        if (entity is null) return false;

        db.ConversationSessions.Remove(entity);
        return true;
    }

    public async Task<IReadOnlyList<SessionSummaryModel>> GetUserSessionsAsync(
        UserId userId, CancellationToken ct = default) =>
        await BuildSessionSummaryQuery(userId).ToListAsync(ct);

    public async Task<PagedResult<SessionSummaryModel>> GetUserSessionsPagedAsync(
        UserId userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildSessionSummaryQuery(userId);
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SessionSummaryModel>(items, total, page, pageSize);
    }

    #endregion Sessions

    #region Messages


    public Task<int> GetMessageCountAsync(string sessionId, string role, CancellationToken ct = default) =>
        db.ConversationMessages
            .CountAsync(m => m.SessionId == sessionId && m.Role == role, ct);

    public async Task<IReadOnlyList<MessageRecord>> GetRecentMessagesAsync(
        string sessionId, int limit, CancellationToken ct = default) =>
        await db.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
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

    #endregion Messages

    #region Private Helpers

    private IQueryable<SessionSummaryModel> BuildSessionSummaryQuery(UserId userId) =>
        db.ConversationSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastModifiedOn)
            .AsNoTracking()
            .Select(s => new SessionSummaryModel(
                s.SessionId,
                s.Title,
                s.CreatedOn,
                s.Messages.Count(),
                s.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault()));

    #endregion
}