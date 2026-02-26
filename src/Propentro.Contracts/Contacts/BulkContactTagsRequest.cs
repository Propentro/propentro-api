namespace Propentro.Contracts.Contacts;

public sealed record BulkContactTagsRequest(string[] ContactIds, string[] TagIds);
