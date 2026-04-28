using FluentAssertions;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class ValueObjectTests
{
    // ── WorkerEventHmac ────────────────────────────────────────────────────────

    [Fact]
    public void WorkerEventHmac_Create_WithValidBase64_Succeeds()
    {
        var base64 = Convert.ToBase64String(new byte[32]);

        var result = WorkerEventHmac.Create(base64, "v1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(base64);
    }

    [Fact]
    public void WorkerEventHmac_Create_WithEmptyString_Fails()
    {
        var result = WorkerEventHmac.Create(string.Empty, "v1");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerEventHmac_Create_WithWhitespace_Fails()
    {
        var result = WorkerEventHmac.Create("   ", "v1");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerEventHmac_Create_WithInvalidBase64_Fails()
    {
        var result = WorkerEventHmac.Create("not!base64@string", "v1");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerEventHmac_Create_WithEmptyKeyVersion_Fails()
    {
        var base64 = Convert.ToBase64String(new byte[32]);

        var result = WorkerEventHmac.Create(base64, string.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerEventHmac_FixedTimeEquals_SameValue_ReturnsTrue()
    {
        var bytes = new byte[32];
        var base64 = Convert.ToBase64String(bytes);
        var a = WorkerEventHmac.Create(base64, "v1").Value;
        var b = WorkerEventHmac.Create(base64, "v1").Value;

        a.FixedTimeEquals(b).Should().BeTrue();
    }

    [Fact]
    public void WorkerEventHmac_FixedTimeEquals_DifferentValue_ReturnsFalse()
    {
        var a = WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;
        var differentBytes = new byte[32]; differentBytes[0] = 1;
        var b = WorkerEventHmac.Create(Convert.ToBase64String(differentBytes), "v1").Value;

        a.FixedTimeEquals(b).Should().BeFalse();
    }

    // ── CompletionProof ────────────────────────────────────────────────────────

    [Fact]
    public void CompletionProof_CreateHashOnly_Succeeds()
    {
        var result = CompletionProof.CreateHashOnly("sha256hash");

        result.IsSuccess.Should().BeTrue();
        result.Value.Level.Should().Be(ProofLevel.HashOnly);
    }

    [Fact]
    public void CompletionProof_CreateHashOnly_WithEmptyHash_Fails()
    {
        var result = CompletionProof.CreateHashOnly(string.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CompletionProof_CreateSignedEvidence_Succeeds()
    {
        var result = CompletionProof.CreateSignedEvidence("hash", "sig");

        result.IsSuccess.Should().BeTrue();
        result.Value.Level.Should().Be(ProofLevel.SignedEvidence);
    }

    [Fact]
    public void CompletionProof_CreateSignedEvidence_WithEmptySignature_Fails()
    {
        var result = CompletionProof.CreateSignedEvidence("hash", string.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CompletionProof_CreatePhotoEvidence_Succeeds()
    {
        var result = CompletionProof.CreatePhotoEvidence("hash", "sig", "blob://ref", "key-id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Level.Should().Be(ProofLevel.PhotoEvidence);
    }

    [Fact]
    public void CompletionProof_CreatePhotoEvidence_WithEmptyBlobRef_Fails()
    {
        var result = CompletionProof.CreatePhotoEvidence("hash", "sig", string.Empty, "key-id");

        result.IsSuccess.Should().BeFalse();
    }

    // ── ScheduleWindow ─────────────────────────────────────────────────────────

    [Fact]
    public void ScheduleWindow_Create_ValidRange_Succeeds()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(2);

        var result = ScheduleWindow.Create(start, end);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ScheduleWindow_Create_EndBeforeStart_Fails()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(-1);

        var result = ScheduleWindow.Create(start, end);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ScheduleWindow_Create_EqualStartEnd_Fails()
    {
        var moment = DateTime.UtcNow;

        var result = ScheduleWindow.Create(moment, moment);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ScheduleWindow_Contains_MomentInsideWindow_ReturnsTrue()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(4);
        var window = ScheduleWindow.Create(start, end).Value;

        window.Contains(start.AddHours(2)).Should().BeTrue();
    }

    [Fact]
    public void ScheduleWindow_Contains_MomentAfterWindow_ReturnsFalse()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(4);
        var window = ScheduleWindow.Create(start, end).Value;

        window.Contains(end.AddMinutes(1)).Should().BeFalse();
    }

    // ── ProgressEventId ────────────────────────────────────────────────────────

    [Fact]
    public void ProgressEventId_Create_ValidGuid_Succeeds()
    {
        var result = ProgressEventId.Create(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ProgressEventId_Create_EmptyGuid_Fails()
    {
        var result = ProgressEventId.Create(Guid.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    // ── OffcutEvent ────────────────────────────────────────────────────────────

    [Fact]
    public void OffcutEvent_Create_ValidDimensions_Succeeds()
    {
        var result = OffcutEvent.Create(Guid.NewGuid(), 300m, 200m);

        result.IsSuccess.Should().BeTrue();
        result.Value.AreaMm2.Should().Be(60000m);
    }

    [Fact]
    public void OffcutEvent_Create_NegativeWidth_Fails()
    {
        var result = OffcutEvent.Create(Guid.NewGuid(), -1m, 200m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void OffcutEvent_Create_NegativeHeight_Fails()
    {
        var result = OffcutEvent.Create(Guid.NewGuid(), 300m, -1m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void OffcutEvent_Create_ZeroDimension_Fails()
    {
        var result = OffcutEvent.Create(Guid.NewGuid(), 0m, 200m);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void OffcutEvent_Create_EmptyMaterialId_Fails()
    {
        var result = OffcutEvent.Create(Guid.Empty, 300m, 200m);

        result.IsSuccess.Should().BeFalse();
    }

    // ── WorkerAssignment ───────────────────────────────────────────────────────

    [Fact]
    public void WorkerAssignment_Create_ValidIds_Succeeds()
    {
        var result = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void WorkerAssignment_Create_EmptyWorkerId_Fails()
    {
        var result = WorkerAssignment.Create(Guid.Empty, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void WorkerAssignment_Create_EmptyEnrollmentId_Fails()
    {
        var result = WorkerAssignment.Create(Guid.NewGuid(), Guid.Empty);

        result.IsSuccess.Should().BeFalse();
    }

    // ── ExecutionKey ───────────────────────────────────────────────────────────

    [Fact]
    public void ExecutionKey_Generate_Returns32ByteKey()
    {
        var key = ExecutionKey.Generate();

        key.KeyBytes.Should().HaveCount(32);
    }

    [Fact]
    public void ExecutionKey_Create_With32Bytes_Succeeds()
    {
        var result = ExecutionKey.Create(new byte[32]);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ExecutionKey_Create_WithWrongLength_Fails()
    {
        var result = ExecutionKey.Create(new byte[16]);

        result.IsSuccess.Should().BeFalse();
    }
}
