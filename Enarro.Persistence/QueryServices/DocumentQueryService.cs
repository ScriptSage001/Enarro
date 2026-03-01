using CoreKernel.Functional.Results;
using Enarro.Application.Abstractions;
using Enarro.Application.Common;
using Enarro.Application.Models;
using Enarro.Domain.Common;
using Enarro.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Persistence.QueryServices;

/// <summary>
/// EF Core implementation of IDocumentQueryService.
/// Projects domain entities to Application Models directly in the database query.
/// </summary>
public class DocumentQueryService(EnarroDbContext dbContext) : IDocumentQueryService
{
    public async Task<Result<PagedResult<DocumentModel>>> GetPagedAsync(
        Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Documents
            .Include(d => d.Tags)
            .Where(d => d.Status != DocumentStatus.Deleted);

        if (userId.HasValue)
        {
            query = query.Where(d => d.UserId == userId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentModel(
                d.Id.Value,
                d.FileName,
                d.ContentType,
                d.SizeBytes,
                d.Status.ToString(),
                d.UploadedAt,
                d.UserId,
                d.ChunkCount,
                d.ErrorMessage,
                d.Tags.Select(t => new DocumentTagModel(t.TagKey, t.TagValue)).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentModel>(items, totalCount, page, pageSize);
    }

    public async Task<Result<DocumentModel>> GetByIdAsync(
        string documentId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(documentId, out var docGuid))
        {
            return Result.Failure<DocumentModel>(DocumentErrors.NotFound(documentId));
        }

        var document = await dbContext.Documents
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == DocumentId.From(docGuid), cancellationToken);

        if (document is null)
        {
            return Result.Failure<DocumentModel>(DocumentErrors.NotFound(documentId));
        }

        return new DocumentModel(
            document.Id.Value,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.Status.ToString(),
            document.UploadedAt,
            document.UserId,
            document.ChunkCount,
            document.ErrorMessage,
            document.Tags.Select(t => new DocumentTagModel(t.TagKey, t.TagValue)).ToList());
    }
}
