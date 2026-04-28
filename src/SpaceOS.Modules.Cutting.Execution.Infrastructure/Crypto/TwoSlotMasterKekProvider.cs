namespace SpaceOS.Modules.Cutting.Execution.Infrastructure.Crypto;

/// <summary>
/// Reads PRIMARY_KEK and PREVIOUS_KEK from environment variables.
/// Supports zero-downtime key rotation by keeping the previous key active for decryption.
/// </summary>
public sealed class TwoSlotMasterKekProvider
{
    private const string PrimaryKekEnvVar = "PRIMARY_KEK";
    private const string PreviousKekEnvVar = "PREVIOUS_KEK";

    /// <summary>Returns the primary (current) KEK bytes, or null if not configured.</summary>
    public byte[]? GetPrimaryKey()
        => ReadKey(PrimaryKekEnvVar);

    /// <summary>Returns the previous KEK bytes used during rotation, or null if not configured.</summary>
    public byte[]? GetPreviousKey()
        => ReadKey(PreviousKekEnvVar);

    private static byte[]? ReadKey(string envVar)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
