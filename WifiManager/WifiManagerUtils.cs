using System;
using System.Globalization;
using System.Text;

namespace WifiManager;

/* Produces concise user-facing security labels from NetworkManager security flags.
    NetworkManager exposes raw capability bit flags. They are precise, but not ideal
    for CLI output or simple application UIs.
    This helper keeps the raw flags available on "WifiNetwork and adds a friendly summary such as Open, WPA2-Personal or WPA3-Personal.
*/
internal static class SecurityFormatter
{
    
    /* Builds a friendly security description from generic, WPA, and RSN flags.

        @param flags Generic access point flags
        @param wpaFlags WPA capability flags
        @param rsnFlags RSN/WPA2/WPA3 capability flags

        @returns A short security label suitable for CLI or UI display.
    */
    public static string Describe(
        AccessPointFlags flags,
        AccessPointSecurityFlags wpaFlags,
        AccessPointSecurityFlags rsnFlags)
    {
        if (!flags.HasFlag(AccessPointFlags.Privacy)
            && wpaFlags == AccessPointSecurityFlags.None
            && rsnFlags == AccessPointSecurityFlags.None)
        {
            return "Open";
        }

        /* Prefer the strongest and most specific modern modes first.
            WPA3/OWE are represented in RSN flags, while older WPA Personal can appear only in the WPA flag set. */
        if (rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtSae))
        {
            return "WPA3-Personal";
        }

        if (rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtOwe)
            || rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtOweTransition))
        {
            return "OWE";
        }

        if (rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtEapSuiteB192))
        {
            return "WPA3-Enterprise";
        }

        if (rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmt8021X)
            || wpaFlags.HasFlag(AccessPointSecurityFlags.KeyMgmt8021X))
        {
            return "Enterprise";
        }

        if (rsnFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtPsk))
        {
            return "WPA2-Personal";
        }

        if (wpaFlags.HasFlag(AccessPointSecurityFlags.KeyMgmtPsk))
        {
            return "WPA-Personal";
        }

        if (flags.HasFlag(AccessPointFlags.Privacy))
        {
            return "WEP";
        }

        return "Protected";
    }
}




/* Converts raw NetworkManager SSID byte arrays into safe display strings.
    The D-Bus API exposes SSIDs as bytes because Wi-Fi SSIDs are not guaranteed to be valid UTF-8.
    This helper prefers readable UTF-8, but falls back to hexadecimal so callers never receive lossy replacement characters. */
internal static class SsidFormatter
{
    
    // UTF-8 decoder configured to throw when the SSID contains invalid UTF-8.
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /* Decodes raw SSID bytes into a human-readable string.

        @param bytes Raw SSID bytes returned by NetworkManager
    
        @returns A UTF-8 SSID, "hidden" for empty/blank SSIDs, or a hexadecimal representation prefixedwith 0x when the bytes are not valid UTF-8.
    */
    public static string Decode(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return "<hidden>";
        }

        try
        {
            var decoded = StrictUtf8.GetString(bytes).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(decoded) ? "<hidden>" : decoded;
        }
        catch (DecoderFallbackException)
        {
            // Preserve the exact SSID bytes for diagnostics instead of replacing invalid byte sequences with U+FFFD.
            return "0x" + Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
        }
    }
}
