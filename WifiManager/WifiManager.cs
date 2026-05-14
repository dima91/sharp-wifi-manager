using System;
using WifiManager.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus.Protocol;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


namespace WifiManager;

/* This implementation talks to the "org.freedesktop.NetworkManager" service.
    It works on Linux systems where NetworkManager is installed, running, and
    responsible for the Wi-Fi interface. Raspberry Pi OS, Ubuntu, Debian, and many
    desktop distributions can use this backend when NetworkManager manages
    "wlan0" or the equivalent wireless interface.
*/

public sealed class WifiManager : IWifiScanner, INetworkInterfacesManager, INetworkProfilesManager
{
    /* Delay used after "RequestScan" when callers do not provide a custom delay.
        NetworkManager does not return scan results synchronously from "RequestScan".
        Two seconds is a pragmatic default for CLI/demo usage: short enough to feel responsive,
        but long enough for most adapters to refresh their access point cache.
    */
    private static readonly TimeSpan DefaultScanDelay = TimeSpan.FromSeconds(2);


    public async Task<IReadOnlyList<WifiNetwork>> GetNetworksAsync(
        bool requestScan = true,
        TimeSpan? scanDelay = null,
        CancellationToken cancellationToken = default)
    {
        /* Tmds.DBus.Protocol reads the well-known system bus address from the host environment.
            If this is missing, the process is not running in a normal Linux D-Bus environment or the system bus is unavailable. */
        if (DBusAddress.System is null)
            throw new InvalidOperationException("The D-Bus system bus address is not available.");

        var connection = new DBusConnection(DBusAddress.System);
        await connection.ConnectAsync();

        var networkManager = new DBusClient(connection);
        var devicePaths = await networkManager.GetDevicesAsync();
        var networks = new List<WifiNetwork>();

        /* NetworkManager exposes all network devices from the root manager object.
            We inspect each device and keep only devices whose DeviceType is Wi-Fi. */
        foreach (var devicePath in devicePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deviceType = await networkManager.GetDeviceTypeAsync(devicePath);
            if (deviceType != NetworkManagerDeviceType.Wifi)
            {
                continue;
            }

            if (requestScan)
            {
                /* RequestScan schedules a scan: it does not block until the scan is complete.
                    The delay lets NetworkManager update its access point objects before we read them. */
                await networkManager.RequestScanAsync(devicePath);
                await Task.Delay(scanDelay ?? DefaultScanDelay, cancellationToken);
            }

            var interfaceName = await networkManager.GetInterfaceNameAsync(devicePath);
            var accessPointPaths = await networkManager.GetAccessPointsAsync(devicePath);

            foreach (var accessPointPath in accessPointPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                /* Access point properties are exposed as a D-Bus property bag.
                    Convert the low-level representation into a stable public record. */
                var accessPoint = await networkManager.GetAccessPointPropertiesAsync(accessPointPath);
                networks.Add(new WifiNetwork(
                    InterfaceName: interfaceName,
                    Ssid: SsidFormatter.Decode(accessPoint.Ssid),
                    Bssid: accessPoint.HwAddress,
                    StrengthPercent: accessPoint.Strength,
                    FrequencyMHz: accessPoint.Frequency,
                    Channel: WifiChannel.FromFrequency(accessPoint.Frequency),
                    MaxBitrateKbps: accessPoint.MaxBitrate,
                    LastSeenSeconds: accessPoint.LastSeen,
                    Mode: accessPoint.Mode,
                    Flags: accessPoint.Flags,
                    WpaFlags: accessPoint.WpaFlags,
                    RsnFlags: accessPoint.RsnFlags,
                    Security: SecurityFormatter.Describe(accessPoint.Flags, accessPoint.WpaFlags, accessPoint.RsnFlags),
                    DevicePath: devicePath.ToString(),
                    AccessPointPath: accessPointPath.ToString()));
            }
        }

        // Stable ordering keeps CLI output predictable and also makes automated comparisons/tests less noisy.
        return networks
            .OrderBy(network => network.InterfaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(network => network.Ssid, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(network => network.StrengthPercent)
            .ToArray();
    }


    public async Task<INetworkInterfacesManager.InterfaceState?> GetInterfaceStateAsync(string interfaceName)
    {
        if (DBusAddress.System is null)
            throw new InvalidOperationException("The D-Bus system bus address is not available.");

        var connection = new DBusConnection(DBusAddress.System);
        await connection.ConnectAsync();

        var networkManager = new DBusClient(connection);
        var devicePaths = await networkManager.GetDevicesAsync();

        foreach (var devicePath in devicePaths)
        {
            var iface = await networkManager.GetInterfaceNameAsync(devicePath);
            if (string.Equals(iface, interfaceName, StringComparison.OrdinalIgnoreCase))
            {
                var state = await networkManager.GetDeviceStateAsync(devicePath);
                return new INetworkInterfacesManager.InterfaceState(iface, state, devicePath.ToString());
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<INetworkInterfacesManager.InterfaceState>> ListInterfaceStatesAsync()
    {
        if (DBusAddress.System is null)
            throw new InvalidOperationException("The D-Bus system bus address is not available.");

        var connection = new DBusConnection(DBusAddress.System);
        await connection.ConnectAsync();

        var networkManager = new DBusClient(connection);
        var devicePaths = await networkManager.GetDevicesAsync();
        var list = new List<INetworkInterfacesManager.InterfaceState>();

        foreach (var devicePath in devicePaths)
        {
            var deviceType = await networkManager.GetDeviceTypeAsync(devicePath);
            if (deviceType != NetworkManagerDeviceType.Wifi)
                continue;

            var iface = await networkManager.GetInterfaceNameAsync(devicePath);
            var state = await networkManager.GetDeviceStateAsync(devicePath);
            list.Add(new INetworkInterfacesManager.InterfaceState(iface, state, devicePath.ToString()));
        }

        return list.OrderBy(s => s.InterfaceName, StringComparer.OrdinalIgnoreCase).ToArray();
    }


    public async Task<ObjectPath?> FindSavedConnectionPathAsync(string ssid)
    {
        if (DBusAddress.System is null)
            throw new InvalidOperationException("The D-Bus system bus address is not available.");

        var connection = new DBusConnection(DBusAddress.System);
        await connection.ConnectAsync();
        var networkManager = new DBusClient(connection);
        return await networkManager.FindSavedConnectionForSsidAsync(ssid);
    }


    public async Task<(bool Connected, string? Message)> ConnectUsingSavedProfileAsync(string interfaceName, string ssid)
    {
        if (DBusAddress.System is null)
            throw new InvalidOperationException("The D-Bus system bus address is not available.");

        var connection = new DBusConnection(DBusAddress.System);
        await connection.ConnectAsync();

        var networkManager = new DBusClient(connection);
        var devicePaths = await networkManager.GetDevicesAsync();

        ObjectPath? targetDevice = null;
        foreach (var devicePath in devicePaths)
        {
            var iface = await networkManager.GetInterfaceNameAsync(devicePath);
            if (string.Equals(iface, interfaceName, StringComparison.OrdinalIgnoreCase))
            {
                var deviceType = await networkManager.GetDeviceTypeAsync(devicePath);
                if (deviceType != NetworkManagerDeviceType.Wifi)
                    return (false, "Device is not a Wi‑Fi interface.");

                targetDevice = devicePath;
                break;
            }
        }

        if (targetDevice is null)
            return (false, $"Interfaccia '{interfaceName}' non trovata.");

        var saved = await networkManager.FindSavedConnectionForSsidAsync(ssid);
        if (saved is not null)
        {
            try
            {
                var activation = await networkManager.ActivateConnectionAsync(saved.Value, targetDevice.Value);
                return (true, $"Attivazione richiesta (activation object: {activation}).");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        return (false, "Errore generico");
    }


    public async Task<(bool Success, string Message)> CreateAndActivateUsingNmcliAsync(string interfaceName, string ssid, string? psk)
    {
        // Build nmcli arguments to connect; nmcli will create a connection profile when needed.
        var args = new List<string> { "device", "wifi", "connect", ssid };
        if (!string.IsNullOrEmpty(psk))
        {
            args.Add("password");
            args.Add(psk);
        }
        if (!string.IsNullOrEmpty(interfaceName))
        {
            args.Add("ifname");
            args.Add(interfaceName);
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("nmcli")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { }
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("Failed to start nmcli");
            var sout = await proc.StandardOutput.ReadToEndAsync();
            var serr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode == 0)
                return (true, sout.Trim());

            var combined = (sout + "\n" + serr).Trim();
            return (false, string.IsNullOrWhiteSpace(combined) ? $"nmcli exit {proc.ExitCode}" : combined);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }


    public async Task<(bool Success, string Message)> DisconnectInterfaceAsync(string interfaceName)
    {
        var args = new List<string> { "device", "disconnect", interfaceName };

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("nmcli")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                ArgumentList = { }
            };

            foreach (var a in args)
                psi.ArgumentList.Add(a);

            using var proc = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("Failed to start nmcli");
            var sout = await proc.StandardOutput.ReadToEndAsync();
            var serr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode == 0)
                return (true, sout.Trim());

            var combined = (sout + "\n" + serr).Trim();
            return (false, string.IsNullOrWhiteSpace(combined) ? $"nmcli exit {proc.ExitCode}" : combined);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
