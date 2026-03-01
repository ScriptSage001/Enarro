namespace Enarro.Application.Chat.DTOs;

public record ChatResultDto(
    string Answer,
    IReadOnlyList<CitationDto> Citations,
    double ConfidenceScore,
    string SessionId,
    int TokensUsed);

public record CitationDto(
    string DocumentId,
    string FileName,
    string Excerpt,
    double Relevance);
