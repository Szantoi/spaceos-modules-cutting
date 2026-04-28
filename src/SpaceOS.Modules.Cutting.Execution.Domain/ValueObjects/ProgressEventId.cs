using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Strongly-typed wrapper around a progress event's GUID identifier.</summary>
public sealed record ProgressEventId
{
    public Guid Value { get; }

    private ProgressEventId(Guid value) => Value = value;

    /// <summary>Creates a ProgressEventId, rejecting empty GUIDs.</summary>
    public static Result<ProgressEventId> Create(Guid guid)
    {
        if (guid == Guid.Empty)
            return Result<ProgressEventId>.Invalid(new ValidationError("ProgressEventId must not be empty."));
        return Result<ProgressEventId>.Success(new ProgressEventId(guid));
    }
}
