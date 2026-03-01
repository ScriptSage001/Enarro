using CoreKernel.Functional.Results;
using CoreKernel.Messaging.Queries;
using Enarro.Application.Common;
using Enarro.Application.Documents.DTOs;
using Enarro.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Enarro.Application.Documents.Queries.ListDocuments;

public sealed class ListDocumentsQueryHandler(IDocumentRepository documentRepository)
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentDto>>
{
    public async Task<Result<PagedResult<DocumentDto>>> Handle(
        ListDocumentsQuery query, CancellationToken cancellationToken)
    {
        var queryable = documentRepository.Query()
            .Where(d => d.Status != DocumentStatus.Deleted);

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(d => d.UserId == query.UserId.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(d => d.UploadedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DocumentDto(
                d.Id.Value,
                d.FileName,
                d.ContentType,
                d.SizeBytes,
                d.Status.ToString(),
                d.UploadedAt,
                d.UserId,
                d.ChunkCount,
                d.ErrorMessage,
                d.Tags.Select(t => new DocumentTagDto(t.TagKey, t.TagValue)).ToList()))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<DocumentDto>(
            items, totalCount, query.Page, query.PageSize));
    }
}
