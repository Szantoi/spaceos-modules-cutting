using System.Security.Cryptography;
using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Worker event HMAC — authenticates a progress event from a badged worker.</summary>
public sealed record WorkerEventHmac
{
    public string Value { get; }
    public string KeyVersion { get; }

    private WorkerEventHmac(string value, string keyVersion)
    {
        Value = value;
        KeyVersion = keyVersion;
    }

    /// <summary>Creates a WorkerEventHmac after validating that <paramref name="base64"/> is a non-empty valid Base64 string.</summary>
    public static Result<WorkerEventHmac> Create(string base64, string keyVersion)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return Result<WorkerEventHmac>.Invalid(new ValidationError("HMAC value must not be empty."));
        if (string.IsNullOrWhiteSpace(keyVersion))
            return Result<WorkerEventHmac>.Invalid(new ValidationError("Key version must not be empty."));

        Span<byte> buffer = stackalloc byte[4096];
        if (!Convert.TryFromBase64String(base64, buffer, out _))
            return Result<WorkerEventHmac>.Invalid(new ValidationError("HMAC value is not valid Base64."));

        return Result<WorkerEventHmac>.Success(new WorkerEventHmac(base64, keyVersion));
    }

    /// <summary>Constant-time equality check against another HMAC value.</summary>
    public bool FixedTimeEquals(WorkerEventHmac other)
    {
        var a = Convert.FromBase64String(Value);
        var b = Convert.FromBase64String(other.Value);
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
