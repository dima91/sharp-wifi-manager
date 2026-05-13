namespace WifiManager;

/* Maps Wi-Fi radio frequencies to channel numbers.
    NetworkManager reports frequency in MHz. Channel numbers are not transmitted as a separate property,
    so the library derives them for the common 2.4 GHz, 5 GHz, and 6 GHz bands. */
internal static class WifiChannel
{
    
    /* Converts a Wi-Fi frequency in MHz to a channel number when the mapping is known.
        
        @param frequencyMHz Frequency in MHz reported by NetworkManager
        
        @returns The Wi-Fi channel number, or null frequencies outside the known channel plans.
    */
    public static int? FromFrequency(uint frequencyMHz)
    {
        return frequencyMHz switch
        {
            2484 => 14,
            >= 2412 and <= 2472 => (int)((frequencyMHz - 2407) / 5),
            5935 => 2,
            >= 4910 and <= 5895 => (int)((frequencyMHz - 5000) / 5),
            >= 5955 and <= 7115 => (int)((frequencyMHz - 5950) / 5),
            _ => null
        };
    }
}
