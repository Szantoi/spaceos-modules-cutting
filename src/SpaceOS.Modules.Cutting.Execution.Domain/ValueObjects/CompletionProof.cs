using Ardalis.Result;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

/// <summary>Represents cryptographic proof that an execution completed.</summary>
public sealed record CompletionProof
{
    public ProofLevel Level { get; }
    public string ProofHash { get; }
    public string? Signature { get; }
    public string? BlobRef { get; }
    public string? EncryptedWith { get; }

    private CompletionProof(ProofLevel level, string proofHash, string? signature, string? blobRef, string? encryptedWith)
    {
        Level = level;
        ProofHash = proofHash;
        Signature = signature;
        BlobRef = blobRef;
        EncryptedWith = encryptedWith;
    }

    /// <summary>Creates a hash-only proof — weakest level.</summary>
    public static Result<CompletionProof> CreateHashOnly(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result<CompletionProof>.Invalid(new ValidationError("Proof hash must not be empty."));
        return Result<CompletionProof>.Success(new CompletionProof(ProofLevel.HashOnly, hash, null, null, null));
    }

    /// <summary>Creates a signed-evidence proof — medium level.</summary>
    public static Result<CompletionProof> CreateSignedEvidence(string hash, string signature)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result<CompletionProof>.Invalid(new ValidationError("Proof hash must not be empty."));
        if (string.IsNullOrWhiteSpace(signature))
            return Result<CompletionProof>.Invalid(new ValidationError("Signature must not be empty."));
        return Result<CompletionProof>.Success(new CompletionProof(ProofLevel.SignedEvidence, hash, signature, null, null));
    }

    /// <summary>Creates a photo-evidence proof — strongest level.</summary>
    public static Result<CompletionProof> CreatePhotoEvidence(string hash, string signature, string blobRef, string encryptedWith)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return Result<CompletionProof>.Invalid(new ValidationError("Proof hash must not be empty."));
        if (string.IsNullOrWhiteSpace(signature))
            return Result<CompletionProof>.Invalid(new ValidationError("Signature must not be empty."));
        if (string.IsNullOrWhiteSpace(blobRef))
            return Result<CompletionProof>.Invalid(new ValidationError("Blob reference must not be empty."));
        if (string.IsNullOrWhiteSpace(encryptedWith))
            return Result<CompletionProof>.Invalid(new ValidationError("EncryptedWith must not be empty."));
        return Result<CompletionProof>.Success(new CompletionProof(ProofLevel.PhotoEvidence, hash, signature, blobRef, encryptedWith));
    }
}
