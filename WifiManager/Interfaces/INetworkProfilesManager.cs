using System.Threading.Tasks;
using System.Collections.Generic;
using Tmds.DBus.Protocol;

namespace WifiManager.Interfaces;


public interface INetworkProfilesManager
{
    public Task<ObjectPath?> FindSavedConnectionPathAsync(string ssid);

    public Task<(bool Connected, string? Message)> ConnectUsingSavedProfileAsync(string interfaceName, string ssid);

    public Task<(bool Success, string Message)> CreateAndActivateUsingNmcliAsync(string interfaceName, string ssid, string? psk);
}