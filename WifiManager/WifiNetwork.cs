namespace WifiManager;


/* Describes a Wi-Fi access point detected by NetworkManager.
    This record intentionally keeps both friendly values and raw NetworkManager flags.
    Friendly values are convenient for UI/CLI output, while raw flags make the model useful for callers that need more precise policy decisions.

    @param InterfaceName Name of the Linux network interface that detected the access point, for example wlan0 or wlp2s0.
    @param Ssid Human-readable SSID. Hidden SSIDs are represented as "hidden" non-UTF-8 SSIDs are represented
            as a lowercase hexadecimal string prefixed by 0x
    @param Bssid MAC address of the access point radio, also known as the BSSID.
    @param StrengthPercent Signal quality reported by NetworkManager, expressed as a value from 0 to 100.
    @param FrequencyMHz Radio frequency in MHz, for example 2412, 5180, or 5955.
    @param Channel Wi-Fi channel derived from "FrequencyMHz" when the frequency is part of a known 2.4 GHz, 5 GHz, or 6 GHz channel plan.
            Otherwise null
    @param MaxBitrateKbps Maximum nominal bitrate reported by NetworkManager, in kilobits per second.
    @param LastSeenSeconds Number of seconds since NetworkManager last saw the access point.
            Negative values mean NetworkManager did not provide a reliable age.
    @param Mode Operating mode advertised by the access point.
    @param Flags Generic access point capability flags, such as whether privacy is enabled.
    @param WpaFlags WPA capability flags reported by NetworkManager.
    @param RsnFlags RSN/WPA2/WPA3 capability flags reported by NetworkManager.
    @param Security Friendly security label derived from "Flags", "WpaFlags" and "RsnFlags".
    @param DevicePath D-Bus object path of the NetworkManager Wi-Fi device that detected the access point.
    @param AccessPointPath D-Bus object path of the NetworkManager access point object.
*/
public sealed record WifiNetwork(
    string InterfaceName,
    string Ssid,
    string Bssid,
    byte StrengthPercent,
    uint FrequencyMHz,
    int? Channel,
    uint MaxBitrateKbps,
    int LastSeenSeconds,
    WirelessMode Mode,
    AccessPointFlags Flags,
    AccessPointSecurityFlags WpaFlags,
    AccessPointSecurityFlags RsnFlags,
    string Security,
    string DevicePath,
    string AccessPointPath)
{    
    /* Gets the maximum nominal bitrate in megabits per second.
        NetworkManager exposes this value in kilobits per second.
        This convenience property converts it to the unit commonly shown in Wi-Fi tools. */
    public double MaxBitrateMbps => MaxBitrateKbps / 1000d;
}
