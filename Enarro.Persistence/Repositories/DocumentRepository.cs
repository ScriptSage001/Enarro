using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence.Repositories;

public class DocumentRepository(EnarroDbContext dbContext) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public IQueryable<Document> Query() => dbContext.Documents.Include(d => d.Tags);

    public void Add(Document document) => dbContext.Documents.Add(document);

    public void Remove(Document document) => dbContext.Documents.Remove(document);
}
