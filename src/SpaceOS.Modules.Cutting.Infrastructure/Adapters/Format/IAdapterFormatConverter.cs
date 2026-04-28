using Ardalis.Result;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Format;

/// <summary>
/// Converts domain requests to vendor-specific wire formats and parses vendor responses.
/// One implementation per external adapter (OptiCut, CutRite, etc.).
/// </summary>
public interface IAdapterFormatConverter
{
    /// <summary>The adapter name this converter handles (e.g. "opticut", "cutrite").</summary>
    string AdapterName { get; }

    /// <summary>Converts a domain request object to a vendor-specific <see cref="AdapterPayload"/>.</summary>
    AdapterPayload ToVendorInput(object request);

    /// <summary>Parses a vendor response <see cref="AdapterPayload"/> into a domain result object.</summary>
    Result<object> ParseVendorOutput(AdapterPayload payload);
}
