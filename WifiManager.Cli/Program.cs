using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WifiManager;
using WifiManager.Interfaces;


var options = CliOptions.Parse(args);
if (options.ShowHelp)
{
    PrintHelp();
    return 0;
}

try
{
    WifiManager.WifiManager manager = new WifiManager.WifiManager();


    if (!string.IsNullOrEmpty(options.IfaceStatus))
    {
        var state = await manager.GetInterfaceStateAsync(options.IfaceStatus);
        if (state is null)
        {
            Console.Error.WriteLine($"Inteface '{options.IfaceStatus}' not found.");
            return 2;
        }

        Console.WriteLine($"Interface: {state.InterfaceName}");
        Console.WriteLine($"  Device path: {state.DevicePath}");
        Console.WriteLine($"  State: {state.State} ({(uint)state.State})");
        return 0;
    }

    if (options.ListIfaces)
    {
        var ifaces = await manager.ListInterfaceStatesAsync();

        if (options.AsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(ifaces, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (ifaces.Count == 0)
        {
            Console.WriteLine("No Wi‑Fi interface found.");
            return 0;
        }

        var rows = ifaces.Select(i => new[] { i.InterfaceName, i.State.ToString(), i.DevicePath }).ToArray();
        var headers = new[] { "IFACE", "STATE", "DEVICE" };
        var widths = headers.Select((header, index) => Math.Max(header.Length, rows.Max(r => r[index].Length))).ToArray();

        PrintRow(headers, widths);
        Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        foreach (var row in rows)
            PrintRow(row, widths);

        return 0;
    }

    if (options.ListProfiles)
    {
        Console.WriteLine("Listing profiles is not yet implemented.");
        return 0;
    }

    if (!string.IsNullOrEmpty(options.ConnectIface) && !string.IsNullOrEmpty(options.ConnectSsid))
    {
        Console.WriteLine($"Connecting to {options.ConnectSsid} using {options.ConnectIface}");

        // Try saved profile first.
        var saved = await manager.FindSavedConnectionPathAsync(options.ConnectSsid);
        if (saved is not null)
        {
            Console.WriteLine("Connecting using saved profile");
            var (ok, message) = await manager.ConnectUsingSavedProfileAsync(options.ConnectIface, options.ConnectSsid);
            if (options.AsJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { success = ok, message }, new JsonSerializerOptions { WriteIndented = true }));
                return ok ? 0 : 3;
            }

            if (ok)
            {
                Console.WriteLine($"Connessione avviata verso '{options.ConnectSsid}' usando profilo salvato su {options.ConnectIface}.");
                Console.WriteLine(message);
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Connessione fallita: " + message);
                return 3;
            }
        }

        // No saved profile — fall back to nmcli. Use provided PSK or prompt the user.
        var psk = options.ConnectPsk;
        if (string.IsNullOrEmpty(psk))
        {
            Console.Write("PSK (blank for open network): ");
            psk = Console.ReadLine();
        }

        var (success, msg) = await manager.CreateAndActivateUsingNmcliAsync(options.ConnectIface, options.ConnectSsid, psk);
        if (options.AsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success, message = msg }, new JsonSerializerOptions { WriteIndented = true }));
            return success ? 0 : 3;
        }

        if (success)
        {
            Console.WriteLine("Successful connection: " + msg);
            return 0;
        }
        else
        {
            Console.Error.WriteLine("Failed connection: " + msg);
            return 3;
        }
    }

    if (!string.IsNullOrEmpty(options.DisconnectIface))
    {
        var (success, msg) = await manager.DisconnectInterfaceAsync(options.DisconnectIface);
        if (options.AsJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success, message = msg }, new JsonSerializerOptions { WriteIndented = true }));
            return success ? 0 : 3;
        }

        if (success)
        {
            Console.WriteLine("Disconnessione riuscita: " + msg);
            return 0;
        }
        else
        {
            Console.Error.WriteLine("Disconnessione fallita: " + msg);
            return 3;
        }
    }

    // By default, we request a fresh scan. --no-scan is useful for scripts
    // that prefer immediate cached output over waiting for a scan.
    var networks = await manager.GetNetworksAsync(requestScan: !options.NoScan);

    if (options.AsJson)
    {
        Console.WriteLine(JsonSerializer.Serialize(networks, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    if (options.Verbose)
    {
        PrintVerbose(networks);
        return 0;
    }

    PrintTable(networks);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error reading Wi-Fi networks from NetworkManager via D-Bus.");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
    WifiManager.Cli

    Usage:
      dotnet run --project WifiManager.Cli
      dotnet run --project WifiManager.Cli -- --json

    Options:
      --no-scan                   Use NetworkManager cache without requesting a fresh scan.
      --iface-status=<iface>      Show the current status of the specified interface (e.g. wlan0).
      --list-ifaces               List the Wi‑Fi interfaces known by NetworkManager.
      --list-profiles             List saved network connection profiles.
      --connect <iface> <ssid>    Prova a connettere usando profili salvati, altrimenti crea con `nmcli`.
      --psk <password>            Password per la connessione (usata se non esiste profilo salvato).
      --disconnect <iface>        Disconnetti l'interfaccia specificata.
      --json                      Print all information in JSON format.
      --verbose                   Print all information in a readable and detailed format.
      --help                      Show this help.
    """);
}

// Output tabellare compatto per l'uso interattivo da terminale.
static void PrintTable(IReadOnlyList<WifiNetwork> networks)
{
    if (networks.Count == 0)
    {
        Console.WriteLine("No Wi-Fi network found.");
        return;
    }

    var rows = networks
        .Select(network => new[]
        {
            network.Ssid,
            $"{network.StrengthPercent}%",
            network.Security,
            network.FrequencyMHz.ToString(),
            network.Channel?.ToString() ?? "-",
            $"{network.MaxBitrateMbps:0.#}",
            network.Bssid,
            network.InterfaceName,
            network.LastSeenSeconds < 0 ? "-" : $"{network.LastSeenSeconds}s"
        })
        .ToArray();

    var headers = new[] { "SSID", "SIGNAL", "SECURITY", "MHz", "CH", "Mbit/s", "BSSID", "IFACE", "SEEN" };
    var widths = headers
        .Select((header, index) => Math.Max(header.Length, rows.Max(row => row[index].Length)))
        .ToArray();

    PrintRow(headers, widths);
    Console.WriteLine(string.Join("  ", widths.Select(width => new string('-', width))));

    foreach (var row in rows)
    {
        PrintRow(row, widths);
    }
}

static void PrintRow(string[] values, int[] widths)
{
    Console.WriteLine(string.Join("  ", values.Select((value, index) => value.PadRight(widths[index]))));
}

static void PrintVerbose(IReadOnlyList<WifiNetwork> networks)
{
    if (networks.Count == 0)
    {
        Console.WriteLine("No Wi-Fi network found.");
        return;
    }

    foreach (var network in networks)
    {
        Console.WriteLine(network.Ssid);
        Console.WriteLine($"  Interface:         {network.InterfaceName}");
        Console.WriteLine($"  BSSID:             {network.Bssid}");
        Console.WriteLine($"  Signal:            {network.StrengthPercent}%");
        Console.WriteLine($"  Security:          {network.Security}");
        Console.WriteLine($"  Frequency:         {network.FrequencyMHz} MHz");
        Console.WriteLine($"  Channel:           {network.Channel?.ToString() ?? "-"}");
        Console.WriteLine($"  Max bitrate:       {network.MaxBitrateMbps:0.#} Mbit/s");
        Console.WriteLine($"  Mode:              {network.Mode}");
        Console.WriteLine($"  Last seen:         {(network.LastSeenSeconds < 0 ? "-" : $"{network.LastSeenSeconds}s")}");
        Console.WriteLine();
    }
}

internal sealed record CliOptions(bool NoScan, bool AsJson, bool Verbose, bool ShowHelp, string? IfaceStatus, bool ListIfaces, bool ListProfiles, string? ConnectIface, string? ConnectSsid, string? ConnectPsk, string? DisconnectIface)
{
    public static CliOptions Parse(string[] args)
    {
        var connectIdx = Array.FindIndex(args, arg => arg.Equals("--connect", StringComparison.OrdinalIgnoreCase));
        var pskIdx = Array.FindIndex(args, arg => arg.Equals("--psk", StringComparison.OrdinalIgnoreCase));
        var disconnectIdx = Array.FindIndex(args, arg => arg.Equals("--disconnect", StringComparison.OrdinalIgnoreCase));

        return new(
            NoScan: args.Contains("--no-scan", StringComparer.OrdinalIgnoreCase),
            AsJson: args.Contains("--json", StringComparer.OrdinalIgnoreCase),
            Verbose: args.Contains("--verbose", StringComparer.OrdinalIgnoreCase),
            ShowHelp: args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase),
            IfaceStatus: args.FirstOrDefault(arg => arg.StartsWith("--iface-status=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2).LastOrDefault(),
            ListIfaces: args.Contains("--list-ifaces", StringComparer.OrdinalIgnoreCase),
            ListProfiles: args.Contains("--list-profiles", StringComparer.OrdinalIgnoreCase),
            ConnectIface: connectIdx >= 0 && args.Length > connectIdx + 1 ? args[connectIdx + 1] : null,
            ConnectSsid: connectIdx >= 0 && args.Length > connectIdx + 2 ? args[connectIdx + 2] : null,
            ConnectPsk: pskIdx >= 0 && args.Length > pskIdx + 1 ? args[pskIdx + 1] : null,
            DisconnectIface: disconnectIdx >= 0 && args.Length > disconnectIdx + 1 ? args[disconnectIdx + 1] : null
        );
    }
}
