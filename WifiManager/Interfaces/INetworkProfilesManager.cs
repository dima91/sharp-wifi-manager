using System.Threading.Tasks;
using System.Collections.Generic;

namespace SharpWifiManager.Interfaces;


public interface INetworkProfilesManager
{
    // Returns the D-Bus object path string of the saved connection, or null if not found.
    public Task<string?> FindSavedConnectionPathAsync(string ssid);

    public Task<(bool Connected, string? Message)> ConnectUsingSavedProfileAsync(string interfaceName, string ssid);

    public Task<(bool Success, string Message)> CreateAndActivateUsingNmcliAsync(string interfaceName, string ssid, string? psk);

    public Task<(bool Success, string Message)> DisconnectInterfaceAsync(string interfaceName);
}