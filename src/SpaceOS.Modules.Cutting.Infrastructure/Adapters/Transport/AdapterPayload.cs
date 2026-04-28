namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

/// <summary>Raw payload exchanged with an external cutting system adapter.</summary>
public sealed record AdapterPayload(
    string ContentType,
    byte[] Content,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>Result of submitting a payload to an external adapter transport.</summary>
public sealed record TransportSubmitResult(
    Guid SheetId,
    string CorrelationId,
    DateTimeOffset SubmittedAt);
