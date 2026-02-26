namespace Propentro.Contracts.Tags;

public sealed record CreateTagRequest(
    string Name,
    string? Color);
