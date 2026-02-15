using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory;
using CoreKernel.Functional.Results;
using Enarro.Common;
using Enarro.Common.Errors;
using Enarro.Data;
using Enarro.Data.Entities;
using Enarro.Models.Document;
using Enarro.Models.Common;

namespace Enarro.Services;

public class DocumentService : IDocumentService
{
    private readonly IKernelMemory _memory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;
    private readonly ILogger<DocumentService> _logger;
    private readonly string _index;
    private readonly int _maxConcurrentUploads;

    public DocumentService(
        IKernelMemory memory,
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        IConfiguration config,
        ILogger<DocumentService> logger)
    {
        _memory = memory;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _logger = logger;
        _index = config["RAGConfigs:IndexName"] ?? "rag-test";
        _maxConcurrentUploads = config.GetValue<int>("RAGConfigs:DocumentProcessing:MaxConcurrentUploads", 5);
    }

    public async Task<Result<DocumentIngestResult>> IngestAsync(
        IFormFile file,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (file.Length == 0)
        {
            return Result.Failure<DocumentIngestResult>(Errors.Documents.EmptyFile());
        }

        var documentId = Guid.NewGuid();

        // 1. Save metadata to database
        var documentEntity = new DocumentEntity
        {
            Id = documentId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            UserId = _userContext.UserId,
            Status = "Processing"
        };

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                documentEntity.Tags.Add(new DocumentTagEntity
                {
                    TagKey = tag.Key,
                    TagValue = tag.Value
                });
            }
        }

        _unitOfWork.Documents.Add(documentEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // 2. Ingest to Kernel Memory
            var document = new Document(id: documentId.ToString())
                .AddStream(file.FileName, file.OpenReadStream())
                .AddTag("name", file.FileName)
                .AddTag("type", "upload")
                .AddTag("uploadedAt", DateTime.UtcNow.ToString("O"))
                .AddTag("size", file.Length.ToString());

            // Add custom tags if provided
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    document.AddTag(tag.Key, tag.Value);
                }
            }

            await _memory.ImportDocumentAsync(
                document: document,
                index: _index,
                cancellationToken: cancellationToken);

            // 3. Update status to Indexed
            documentEntity.Status = "Indexed";
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully ingested document {DocumentId} ({FileName}, {Size} bytes)",
                documentId, file.FileName, file.Length);

            return Result.Success(new DocumentIngestResult(documentId.ToString(), file.FileName, true, null));
        }
        catch (Exception ex)
        {
            // Update status to Failed
            documentEntity.Status = "Failed";
            documentEntity.ErrorMessage = ex.Message;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to ingest document {DocumentId} ({FileName})", documentId, file.FileName);
            return Result.Failure<DocumentIngestResult>(Errors.Documents.UploadFailed(ex.Message));
        }
    }

    public async Task<Result<BatchIngestResult>> IngestBatchAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();

        if (fileList.Count == 0)
        {
            return Result.Failure<BatchIngestResult>(Errors.Documents.EmptyFile());
        }

        _logger.LogInformation("Starting batch ingestion of {Count} documents", fileList.Count);

        var results = new List<DocumentIngestResult>();
        var semaphore = new SemaphoreSlim(_maxConcurrentUploads);
        var tasks = fileList.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var ingestResult = await IngestAsync(file, cancellationToken: cancellationToken);
                return ingestResult.IsSuccess
                    ? ingestResult.Value
                    : new DocumentIngestResult(null, file.FileName, false, ingestResult.Error.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest document {FileName}", file.FileName);
                return new DocumentIngestResult(null, file.FileName, false, ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        });

        results.AddRange(await Task.WhenAll(tasks));

        var batchResult = new BatchIngestResult(
            TotalFiles: results.Count,
            SuccessCount: results.Count(r => r.Success),
            FailedCount: results.Count(r => !r.Success),
            Results: results
        );

        _logger.LogInformation(
            "Batch ingestion completed: {Total} total, {Success} succeeded, {Failed} failed",
            batchResult.TotalFiles, batchResult.SuccessCount, batchResult.FailedCount);

        return Result.Success(batchResult);
    }

    public async Task<Result<DocumentMetadata>> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _unitOfWork.Documents
                .FirstOrDefaultAsync(
                    d => d.Id == Guid.Parse(documentId),
                    include: q => q.Include(d => d.Tags),
                    cancellationToken: cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return Result.Failure<DocumentMetadata>(Errors.Documents.NotFound(documentId));
            }

            return Result.Success(new DocumentMetadata(
                Id: entity.Id.ToString(),
                FileName: entity.FileName,
                ContentType: entity.ContentType,
                SizeBytes: entity.SizeBytes,
                UploadedAt: entity.UploadedAt,
                UploadedBy: entity.UploadedBy,
                Status: Enum.Parse<DocumentStatus>(entity.Status),
                Tags: entity.Tags.ToDictionary(t => t.TagKey, t => t.TagValue),
                ChunkCount: entity.ChunkCount,
                ErrorMessage: entity.ErrorMessage
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
            return Result.Failure<DocumentMetadata>(Errors.Internal(ex.Message));
        }
    }

    public async Task<Result<PagedResult<DocumentMetadata>>> ListDocumentsAsync(
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _unitOfWork.Documents.Query(q => q.Include(d => d.Tags));

            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(d => d.Tags.Any(t => t.TagValue == tag));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var dbData = await query
                                    .OrderByDescending(d => d.UploadedAt)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .Select(e => new
                                    {
                                        e.Id,
                                        e.FileName,
                                        e.ContentType,
                                        e.SizeBytes,
                                        e.UploadedAt,
                                        e.UploadedBy,
                                        e.Status,
                                        e.Tags,
                                        e.ChunkCount,
                                        e.ErrorMessage
                                    })
                                    .ToListAsync(cancellationToken);

            var items = dbData.Select(e => new DocumentMetadata(
                                            e.Id.ToString(),
                                            e.FileName,
                                            e.ContentType,
                                            e.SizeBytes,
                                            e.UploadedAt,
                                            e.UploadedBy,
                                            Enum.Parse<DocumentStatus>(e.Status),
                                            e.Tags.ToDictionary(t => t.TagKey, t => t.TagValue),
                                            e.ChunkCount,
                                            e.ErrorMessage
                                        )).ToList();
            
            _logger.LogInformation("Listed {Count} documents (page {Page} of {TotalPages})", 
                items.Count, page, (int)Math.Ceiling(totalCount / (double)pageSize));

            return Result.Success(new PagedResult<DocumentMetadata>(
                Items: items,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents");
            return Result.Failure<PagedResult<DocumentMetadata>>(Errors.Internal(ex.Message));
        }
    }

    public async Task<Result> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Delete from database (cascade deletes tags)
            var entity = await _unitOfWork.Documents
                .FirstOrDefaultAsync(d => d.Id == Guid.Parse(documentId), cancellationToken: cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for deletion", documentId);
                return Result.Failure(Errors.Documents.NotFound(documentId));
            }

            _unitOfWork.Documents.Remove(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 2. Delete from Kernel Memory
            await _memory.DeleteDocumentAsync(documentId, index: _index, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully deleted document {DocumentId}", documentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return Result.Failure(Errors.Documents.DeleteFailed(documentId, ex.Message));
        }
    }
}