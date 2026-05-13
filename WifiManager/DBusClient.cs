using Tmds.DBus.Protocol;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WifiManager;


/* Low-level D-Bus client for the subset of NetworkManager used by the scanner.
    The public API of the library deliberately avoids exposing D-Bus concepts.
    This class is the translation layer between strongly typed C# methods and the
    raw NetworkManager D-Bus methods/properties.
*/

internal sealed class DBusClient
{
    // Well-known bus name of the NetworkManager service on the system bus.
    private const string Destination = "org.freedesktop.NetworkManager";

    // Root object path of the NetworkManager manager object.
    private const string ManagerPath = "/org/freedesktop/NetworkManager";

    // D-Bus interface implemented by the NetworkManager manager object.
    private const string ManagerInterface = "org.freedesktop.NetworkManager";

    // D-Bus interface shared by NetworkManager device objects.
    private const string DeviceInterface = "org.freedesktop.NetworkManager.Device";

    // D-Bus interface implemented by NetworkManager Wi-Fi device objects.
    private const string WirelessDeviceInterface = "org.freedesktop.NetworkManager.Device.Wireless";

    // D-Bus interface implemented by NetworkManager access point objects.
    private const string AccessPointInterface = "org.freedesktop.NetworkManager.AccessPoint";

    // Standard D-Bus properties interface used to read object properties.
    private const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    // Active connection to the D-Bus system bus.
    private readonly DBusConnection _connection;

    /* Creates a NetworkManager D-Bus client over an existing D-Bus connection.
        @param connection Connected D-Bus connection
    */
    public DBusClient(DBusConnection connection)
    {
        _connection = connection;
    }

    
    /* Calls "org.freedesktop.NetworkManager.GetDevices"

        @returns D-Bus object paths for all devices known by NetworkManager
    */
    public Task<ObjectPath[]> GetDevicesAsync() => _connection.CallMethodAsync(CreateGetDevicesMessage(), ReadObjectPathArray, this);


    /* Reads the Linux interface name of a NetworkManager device.

        @param devicePath D-Bus object path of the NetworkManager device

        @returns Interface name such as wlan0 or wlp2s0
    */
    public Task<string> GetInterfaceNameAsync(ObjectPath devicePath) => GetStringPropertyAsync(devicePath, DeviceInterface, "Interface");


    /* Reads the NetworkManager device type.

        @param devicePath D-Bus object path of the NetworkManager device

        @returns The device type converted to "NetworkManagerDeviceType"
    */
    public async Task<NetworkManagerDeviceType> GetDeviceTypeAsync(ObjectPath devicePath)
    {
        var value = await GetUInt32PropertyAsync(devicePath, DeviceInterface, "DeviceType");
        return (NetworkManagerDeviceType)value;
    }

    
    /* Calls "org.freedesktop.NetworkManager.Device.Wireless.GetAccessPoints"

        @param devicePath D-Bus object path of a Wi-Fi device

        @returns D-Bus object paths for access points currently known by the device
    */
    public Task<ObjectPath[]> GetAccessPointsAsync(ObjectPath devicePath) =>
        _connection.CallMethodAsync(CreateGetAccessPointsMessage(devicePath), ReadObjectPathArray, this);

    
    /* Calls "org.freedesktop.NetworkManager.Device.Wireless.RequestScan"
        The method asks NetworkManager to start a scan and returns once the request is accepted.
        Scan results become visible later through the access point objects.
        @param devicePath D-Bus object path of a Wi-Fi device
    */
    
    public Task RequestScanAsync(ObjectPath devicePath) => _connection.CallMethodAsync(CreateRequestScanMessage(devicePath));


    /* Reads all properties of a NetworkManager access point object.

        @param accessPointPath D-Bus object path of the access point
        
        @returns A typed subset of the access point property bag
    */
    public Task<AccessPointProperties> GetAccessPointPropertiesAsync(ObjectPath accessPointPath) =>
        _connection.CallMethodAsync(
            CreateGetAllPropertiesMessage(accessPointPath, AccessPointInterface),
            ReadAccessPointProperties,
            this);

    
    /* Reads a string property through org.freedesktop.DBus.Properties.Get

        @param path Object path that owns the property.
        @param dbusInterface">Interface that declares the property.
        @param property">Property name
        
        @returns The string value inside the D-Bus variant reply
    */
    private Task<string> GetStringPropertyAsync(ObjectPath path, string dbusInterface, string property)
    {
        return _connection.CallMethodAsync(
            CreateGetPropertyMessage(path, dbusInterface, property),
            static (Message message, object? _) =>
            {
                var reader = message.GetBodyReader();
                reader.ReadSignature("s");
                return reader.ReadString();
            },
            this);
    }

    
    /* Reads an unsigned 32-bit integer property through "org.freedesktop.DBus.Properties.Get"

        @param path Object path that owns the property
        @param dbusInterface Interface that declares the property
        @param property Property name

        @returns The unsigned integer value inside the D-Bus variant reply
    */
    private Task<uint> GetUInt32PropertyAsync(ObjectPath path, string dbusInterface, string property)
    {
        return _connection.CallMethodAsync(
            CreateGetPropertyMessage(path, dbusInterface, property),
            static (Message message, object? _) =>
            {
                var reader = message.GetBodyReader();
                reader.ReadSignature("u");
                return reader.ReadUInt32();
            },
            this);
    }

    
    /* Creates the message for "NetworkManager.GetDevices()"

        @returns A D-Bus method call message with no body
    */
    private MessageBuffer CreateGetDevicesMessage()
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Destination,
            path: ManagerPath,
            @interface: ManagerInterface,
            member: "GetDevices");
        return writer.CreateMessage();
    }

    
    /* Creates the message for "Device.Wireless.GetAccessPoints()"

        @param devicePath Object path of the Wi-Fi device

        @returns A D-Bus method call message with no body
    */
    private MessageBuffer CreateGetAccessPointsMessage(ObjectPath devicePath)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Destination,
            path: devicePath,
            @interface: WirelessDeviceInterface,
            member: "GetAccessPoints");
        return writer.CreateMessage();
    }

    
    /* Creates the message for "Device.Wireless.RequestScan"
        NetworkManager allows scan options in a dictionary.
        The scanner does not need special options, so it sends an empty dictionary.

        @param devicePath Object path of the Wi-Fi device

        @returns A D-Bus method call message with an empty options dictionary
    */
    
    private MessageBuffer CreateRequestScanMessage(ObjectPath devicePath)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Destination,
            path: devicePath,
            @interface: WirelessDeviceInterface,
            signature: "a{sv}",
            member: "RequestScan");
        writer.WriteDictionary(new Dictionary<string, VariantValue>());
        return writer.CreateMessage();
    }

    
    /* Creates a standard D-Bus "Properties.Get" message.

        @param path Object path that owns the property
        @param dbusInterface Interface that declares the property
        @param property Property name

        @returns A D-Bus method call message with body signature "ss"
    */ 
    private MessageBuffer CreateGetPropertyMessage(ObjectPath path, string dbusInterface, string property)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Destination,
            path: path,
            @interface: PropertiesInterface,
            signature: "ss",
            member: "Get");
        writer.WriteString(dbusInterface);
        writer.WriteString(property);
        return writer.CreateMessage();
    }

    
    /* Creates a standard D-Bus <c>Properties.GetAll</c> message.

        @param path Object path that owns the properties
        @param dbusInterface Interface whose properties should be returned

        @returns A D-Bus method call message with body signature "s"
    */
    private MessageBuffer CreateGetAllPropertiesMessage(ObjectPath path, string dbusInterface)
    {
        var writer = _connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Destination,
            path: path,
            @interface: PropertiesInterface,
            signature: "s",
            member: "GetAll");
        writer.WriteString(dbusInterface);
        return writer.CreateMessage();
    }

    
    /* Reads a D-Bus method return body containing an array of object paths.

        @param message Reply message from NetworkManager
        @param _ Unused state parameter required by the Tmds reader delegate

        @returns The object paths contained in the reply body
    */
    private static ObjectPath[] ReadObjectPathArray(Message message, object? _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadArrayOfObjectPath();
    }

    
    /* Reads the property dictionary returned by "Properties.GetAll" for a NetworkManager access point.
        Unknown properties are skipped as variants.
        This keeps the parser forward compatible with NetworkManager versions that add extra access point fields.
    
        @param message Reply message from NetworkManager
        @param _ Unused state parameter required by the Tmds reader delegate
        
        @returns A typed object containing the access point properties used by the library
    */
    private static AccessPointProperties ReadAccessPointProperties(Message message, object? _)
    {
        var reader = message.GetBodyReader();
        var properties = new AccessPointProperties();
        var arrayEnd = reader.ReadArrayStart(DBusType.Struct);

        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();

            switch (property)
            {
                case "Flags":
                    // D-Bus variant signature: "u" (UInt32).
                    reader.ReadSignature("u");
                    properties.Flags = (AccessPointFlags)reader.ReadUInt32();
                    break;
                case "WpaFlags":
                    // D-Bus variant signature: "u" (UInt32).
                    reader.ReadSignature("u");
                    properties.WpaFlags = (AccessPointSecurityFlags)reader.ReadUInt32();
                    break;
                case "RsnFlags":
                    // D-Bus variant signature: "u" (UInt32).
                    reader.ReadSignature("u");
                    properties.RsnFlags = (AccessPointSecurityFlags)reader.ReadUInt32();
                    break;
                case "Ssid":
                    // D-Bus variant signature: "ay" (array of bytes).
                    reader.ReadSignature("ay");
                    properties.Ssid = reader.ReadArrayOfByte();
                    break;
                case "Frequency":
                    // D-Bus variant signature: "u" (UInt32 MHz).
                    reader.ReadSignature("u");
                    properties.Frequency = reader.ReadUInt32();
                    break;
                case "HwAddress":
                    // D-Bus variant signature: "s" (string MAC address).
                    reader.ReadSignature("s");
                    properties.HwAddress = reader.ReadString();
                    break;
                case "Mode":
                    // D-Bus variant signature: "u" (UInt32 enum value).
                    reader.ReadSignature("u");
                    properties.Mode = (WirelessMode)reader.ReadUInt32();
                    break;
                case "MaxBitrate":
                    // D-Bus variant signature: "u" (UInt32 Kbit/s).
                    reader.ReadSignature("u");
                    properties.MaxBitrate = reader.ReadUInt32();
                    break;
                case "Strength":
                    // D-Bus variant signature: "y" (byte percentage).
                    reader.ReadSignature("y");
                    properties.Strength = reader.ReadByte();
                    break;
                case "LastSeen":
                    // D-Bus variant signature: "i" (Int32 seconds).
                    reader.ReadSignature("i");
                    properties.LastSeen = reader.ReadInt32();
                    break;
                default:
                    reader.ReadVariantValue();
                    break;
            }
        }

        return properties;
    }
}
