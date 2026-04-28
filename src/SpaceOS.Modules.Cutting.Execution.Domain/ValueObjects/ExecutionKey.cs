using System.Security.Cryptography;
using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>AES-256 symmetric key used to encrypt execution-related photos.</summary>
public sealed record ExecutionKey
{
    private const int RequiredLength = 32;

    public byte[] KeyBytes { get; }

    private ExecutionKey(byte[] keyBytes) => KeyBytes = keyBytes;

    /// <summary>Creates an ExecutionKey from existing bytes, validating the 32-byte length requirement.</summary>
    public static Result<ExecutionKey> Create(byte[] keyBytes)
    {
        if (keyBytes is null || keyBytes.Length != RequiredLength)
            return Result<ExecutionKey>.Invalid(new ValidationError($"AES-256 key must be exactly {RequiredLength} bytes."));
        return Result<ExecutionKey>.Success(new ExecutionKey(keyBytes));
    }

    /// <summary>Generates a new random AES-256 key.</summary>
    public static ExecutionKey Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(RequiredLength);
        return new ExecutionKey(bytes);
    }
}
