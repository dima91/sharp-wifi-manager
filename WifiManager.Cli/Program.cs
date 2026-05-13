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
    // Using the concrete implementation defined in the WifiManager project
    IWifiScanner scanner = new WifiManager.WifiManager();

    // By default, we request a fresh scan. --no-scan is useful for scripts
    // that prefer immediate cached output over waiting for a scan.
    var networks = await scanner.GetNetworksAsync(requestScan: !options.NoScan);
    Console.WriteLine("Lol");

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
      --no-scan   Use NetworkManager cache without requesting a fresh scan.
      --json      Print all information in JSON format.
      --verbose   Print all information in a readable and detailed format.
      --help      Show this help.
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

internal sealed record CliOptions(bool NoScan, bool AsJson, bool Verbose, bool ShowHelp)
{
    public static CliOptions Parse(string[] args) => new(
        NoScan: args.Contains("--no-scan", StringComparer.OrdinalIgnoreCase),
        AsJson: args.Contains("--json", StringComparer.OrdinalIgnoreCase),
        Verbose: args.Contains("--verbose", StringComparer.OrdinalIgnoreCase),
        ShowHelp: args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase));
}
