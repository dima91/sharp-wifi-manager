using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpWifiManager.Interfaces;


/* Defines the public contract for components that discover nearby Wi-Fi networks.
    Implementations are expected to return one entry per detected access point.
    Multiple access points can expose the same SSID when a network is backed by
    several radios, bands, or mesh nodes. */
public interface IWifiScanner
{
    
    /* Retrieves the Wi-Fi networks currently known by the underlying Wi-Fi backend.
        @param requstScan When true, asks the backend to perform a fresh scan before reading the access point list.
                When false returns the backend cache immediately.
        @param scanDelay Optional delay to wait after requesting a scan. NetworkManager updates scan results asynchronously,
                so a short delay gives it time to publish fresh data. If omitted, the implementation chooses a conservative default.
        @param cancellationToken Token used to cancel the scan delay or stop processing devices/access points.

        @returns A read-only list of detected Wi-Fi access points, sorted by interface, SSID and descending signal strength.
        @exception InvalidOperationException Thrown by implementations when the required Wi-Fi backend is not available.
    */

    Task<IReadOnlyList<WifiNetwork>> GetNetworksAsync(
        bool requestScan = true,
        TimeSpan? scanDelay = null,
        CancellationToken cancellationToken = default);
}
