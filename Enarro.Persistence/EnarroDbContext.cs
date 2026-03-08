using Enarro.Domain.Conversation;
using Enarro.Domain.Documents;
using Enarro.Domain.Users;
using Enarro.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence;

/// <summary>
/// EF Core DbContext for the Enarro application.
/// Maps rich domain entities to the database.
/// Interceptors are injected via DI and wired in OnConfiguring.
/// </summary>
public class EnarroDbContext : DbContext
{
    // private readonly AuditableEntityInterceptor? _auditableInterceptor;
    private readonly DomainEventDispatchInterceptor? _domainEventInterceptor;

    /// <summary>
    /// Runtime constructor — interceptors injected via DI.
    /// </summary>
    public EnarroDbContext(
        DbContextOptions<EnarroDbContext> options,
        // AuditableEntityInterceptor? auditableInterceptor = null,
        DomainEventDispatchInterceptor? domainEventInterceptor = null)
        : base(options)
    {
        // _auditableInterceptor = auditableInterceptor;
        _domainEventInterceptor = domainEventInterceptor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add interceptors if available (not available during design-time migration generation)
        var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>();

        // if (_auditableInterceptor is not null) interceptors.Add(_auditableInterceptor);
        if (_domainEventInterceptor is not null) interceptors.Add(_domainEventInterceptor);

        if (interceptors.Count > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnarroDbContext).Assembly);
    }
}
