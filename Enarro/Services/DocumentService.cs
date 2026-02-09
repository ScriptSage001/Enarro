using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory;
using Enarro.Data;
using Enarro.Data.Entities;
using Enarro.Models.Document;
using Enarro.Models.Common;
using Enarro.Services.Search;

namespace Enarro.Services;

public class DocumentService : IDocumentService
{
    private readonly IKernelMemory _memory;
    private readonly EnarroDbContext _dbContext;
    private readonly IKeywordSearchService _keywordSearch;
    private readonly ILogger<DocumentService> _logger;
    private readonly IConfiguration _config;
    private readonly string _index;
    private readonly int _maxConcurrentUploads;

    public DocumentService(
        IKernelMemory memory,
        EnarroDbContext dbContext,
        IKeywordSearchService keywordSearch,
        IConfiguration config,
        ILogger<DocumentService> logger)
    {
        _memory = memory;
        _dbContext = dbContext;
        _keywordSearch = keywordSearch;
        _config = config;
        _logger = logger;
        _index = config["RAGConfigs:IndexName"] ?? "rag-test";
        _maxConcurrentUploads = config.GetValue<int>("RAGConfigs:DocumentProcessing:MaxConcurrentUploads", 5);
    }

    public async Task<DocumentIngestResult> IngestAsync(
        IFormFile file,
        Guid userId,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var documentId = Guid.NewGuid();

        // 1. Save metadata to database
        var documentEntity = new DocumentEntity
        {
            Id = documentId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedAt = DateTime.UtcNow,
            UserId = userId,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                documentEntity.Tags.Add(new DocumentTagEntity
                {
                    TagKey = tag.Key,
                    TagValue = tag.Value,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _dbContext.Documents.Add(documentEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

            // 3. Extract text and index for keyword search
            try
            {
                // Read file content for keyword indexing
                using var memoryStream = new MemoryStream();
                file.OpenReadStream().CopyTo(memoryStream);
                var content = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                
                await _keywordSearch.IndexDocumentAsync(
                    documentId.ToString(),
                    content,
                    cancellationToken);
                
                _logger.LogInformation(
                    "Indexed document {DocumentId} for keyword search",
                    documentId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to index document {DocumentId} for keyword search, continuing with vector search only",
                    documentId);
            }

            // 4. Update status to Indexed
            documentEntity.Status = "Indexed";
            documentEntity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully ingested document {DocumentId} ({FileName}, {Size} bytes)",
                documentId, file.FileName, file.Length);

            return new DocumentIngestResult(documentId.ToString(), file.FileName, true, null);
        }
        catch (Exception ex)
        {
            // Update status to Failed
            documentEntity.Status = "Failed";
            documentEntity.ErrorMessage = ex.Message;
            documentEntity.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Failed to ingest document {DocumentId} ({FileName})", documentId, file.FileName);
            return new DocumentIngestResult(null, file.FileName, false, ex.Message);
        }
    }

    public async Task<BatchIngestResult> IngestBatchAsync(
        IEnumerable<IFormFile> files,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var results = new List<DocumentIngestResult>();

        _logger.LogInformation("Starting batch ingestion of {Count} documents", fileList.Count);

        var semaphore = new SemaphoreSlim(_maxConcurrentUploads);
        var tasks = fileList.Select(async file =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var documentResult = await IngestAsync(file, userId, cancellationToken: cancellationToken);
                return documentResult;
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

        return batchResult;
    }

    public async Task<DocumentMetadata?> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.Documents
                .Include(d => d.Tags)
                .FirstOrDefaultAsync(d => d.Id == Guid.Parse(documentId), cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document {DocumentId} not found", documentId);
                return null;
            }

            return new DocumentMetadata(
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
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<PagedResult<DocumentMetadata>> ListDocumentsAsync(
        int page = 1,
        int pageSize = 20,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Documents.Include(d => d.Tags).AsQueryable();

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

            return new PagedResult<DocumentMetadata>(
                Items: items,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents");
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Delete from database (cascade deletes tags)
            var entity = await _dbContext.Documents
                .FirstOrDefaultAsync(d => d.Id == Guid.Parse(documentId), cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for deletion", documentId);
                return false;
            }

            _dbContext.Documents.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 2. Delete from Kernel Memory
            await _memory.DeleteDocumentAsync(documentId, index: _index, cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully deleted document {DocumentId}", documentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            return false;
        }
    }
}