using System.Collections.Generic;
using System.Threading.Tasks;

namespace WifiManager.Interfaces;


public interface INetworkInterfacesManager
{
    public sealed record InterfaceState(string InterfaceName, NetworkManagerDeviceState State, string DevicePath);

    public Task<InterfaceState?> GetInterfaceStateAsync(string interfaceName);

    public Task<IReadOnlyList<InterfaceState>> ListInterfaceStatesAsync();
}
