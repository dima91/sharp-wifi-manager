using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;


namespace SharpWifiManager;


/* NetworkManager device type values used by this library.
    NetworkManager defines many more device types.
    The library currently needs only the values that let it filter the global device list down to Wi-Fi interfaces.
*/
public enum NetworkManagerDeviceType : uint
{    
    // Unknown or not reported device type
    Unknown = 0,

    // Wired Ethernet device
    Ethernet = 1,

    // Wi-Fi device managed by NetworkManager    
    Wifi = 2
}


// Operating mode of a Wi-Fi access point as reported by NetworkManager.
public enum WirelessMode : uint
{
    // Mode is unknown or not reported.
    Unknown = 0,

    // Peer-to-peer IBSS/ad-hoc network
    AdHoc = 1,

    // Infrastructure network, the normal mode for routers and managed access points.
    Infrastructure = 2,

    // Device is operating as an access point.
    AccessPoint = 3,

    // Mesh network mode.
    Mesh = 4
}


// Generic capability flags for a NetworkManager access point.
[Flags]
public enum AccessPointFlags : uint
{
    // No generic access point flags are set.
    None = 0,

    /* The access point requires some form of privacy/security.
        On old networks this can indicate WEP; on modern networks the detailed
        WPA/RSN flags describe the actual key management method. */
    Privacy = 0x1
}


/* WPA/RSN security capability flags for a NetworkManager access point.
    NetworkManager exposes separate WPA and RSN flag sets.
    The same enum is used for both because the bit layout is shared.
*/
[Flags]
public enum AccessPointSecurityFlags : uint
{
    // No security capability flags are set.
    None = 0,

    // Pairwise WEP-40 cipher is supported.
    PairWep40 = 0x1,

    // Pairwise WEP-104 cipher is supported.
    PairWep104 = 0x2,

    // Pairwise TKIP cipher is supported.
    PairTkip = 0x4,

    // Pairwise CCMP/AES cipher is supported.
    PairCcmp = 0x8,

    // Group WEP-40 cipher is supported.
    GroupWep40 = 0x10,

    // Group WEP-104 cipher is supported.
    GroupWep104 = 0x20,

    // Group TKIP cipher is supported.
    GroupTkip = 0x40,

    // Group CCMP/AES cipher is supported.
    
    GroupCcmp = 0x80,

    // Pre-shared-key authentication is supported. This is used by WPA/WPA2 Personal
    KeyMgmtPsk = 0x100,

    // 802.1X/EAP authentication is supported. This is used by Enterprise networks
    KeyMgmt8021X = 0x200,

    // SAE authentication is supported. This is the WPA3 Personal key management mode
    KeyMgmtSae = 0x400,

    // Opportunistic Wireless Encryption is supported
    KeyMgmtOwe = 0x800,

    // OWE transition mode is supported
    KeyMgmtOweTransition = 0x1000,

    // WPA3 Enterprise 192-bit suite is supported
    KeyMgmtEapSuiteB192 = 0x2000
}


// Represents the current operating state of a network device managed by NetworkManager.
public enum NetworkManagerDeviceState : uint
{
    // The device state is unknown or cannot be determined.
    Unknown = 0,
    
    // The device is not managed by NetworkManager.
    Unmanaged = 10,
    
    // The device is managed but cannot be used (e.g., missing firmware or cable disconnected).
    Unavailable = 20,
    
    // The device is managed and available, but not connected to any network.
    Disconnected = 30,
    
    // The device is preparing the connection (initial activation stage).
    Prepare = 40,
    
    // Device or link configuration in progress.
    Config = 50,
    
    // The device requires secrets or authentication to proceed.
    NeedAuth = 60,
    
    // The device is acquiring an IP address and related network parameters.
    IpConfig = 70,
    
    // IP connectivity check in progress (e.g., checking the gateway).
    IpCheck = 80,
    
    // Waiting for activation of secondary connections (like a VPN).
    Secondaries = 90,
    
    // The device has a valid network connection and is fully operational.
    Activated = 100,
    
    // The device is disconnecting.
    Deactivating = 110,
    
    // The last activation attempt or existing connection has failed.
    Failed = 120
}
