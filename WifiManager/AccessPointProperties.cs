using System;

namespace WifiManager;


/* Internal representation of the NetworkManager AccessPoint property bag.
    NetworkManager returns access point data as a dictionary
    whose values are D-Bus variants. This type stores only the properties the public WifiNetwork model currently needs. */
internal sealed class AccessPointProperties
{
    // Generic capability flags from the "Flags" property.
    public AccessPointFlags Flags { get; set; }

    // WPA capability flags from the "WpaFlags" property.
    public AccessPointSecurityFlags WpaFlags { get; set; }

    // RSN/WPA2/WPA3 capability flags from the "RsnFlags" property.
    public AccessPointSecurityFlags RsnFlags { get; set; }

    /* Raw SSID bytes from the "Ssid" property.
        SSIDs are byte arrays on D-Bus because not every real-world SSID is valid UTF-8.
        Formatting is handled later by "SsidFormatter". */
    public byte[] Ssid { get; set; } = Array.Empty<byte>();

    // Access point frequency in MHz from the "Frequency" property.
    public uint Frequency { get; set; }

    // Access point MAC address from the "HwAddress" property.
    public string HwAddress { get; set; } = string.Empty;

    // Wireless operating mode from the "Mode" property.
    public WirelessMode Mode { get; set; }

    // Maximum bitrate in kilobits per second from the "MaxBitrate" property.
    public uint MaxBitrate { get; set; }

    // Signal strength percentage from the "Strength" property.
    public byte Strength { get; set; }

    // Number of seconds since NetworkManager last saw this access point.
    public int LastSeen { get; set; }
}
