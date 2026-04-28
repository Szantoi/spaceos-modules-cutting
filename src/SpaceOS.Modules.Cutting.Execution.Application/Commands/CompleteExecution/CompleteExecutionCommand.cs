using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Execution.Application.Commands.CompleteExecution;

public sealed record CompleteExecutionCommand(
    Guid ExecutionId,
    Guid TenantId,
    ProofLevel ProofLevel,
    string ProofHash,
    string? Signature,
    string? BlobRef,
    string? EncryptedWith) : IRequest<Result>;
