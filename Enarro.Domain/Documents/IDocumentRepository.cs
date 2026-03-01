using Enarro.Domain.Common;

namespace Enarro.Domain.Documents;

/// <summary>
/// Repository contract for the Document aggregate.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a queryable for building paginated/filtered queries.
    /// </summary>
    IQueryable<Document> Query();
    
    void Add(Document document);
    
    void Remove(Document document);
}
