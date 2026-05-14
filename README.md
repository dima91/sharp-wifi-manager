# Sharp wifi-manager

A .NET 8 library and command-line tool (CLI) for managing Wi-Fi networks on Linux systems via **NetworkManager** (D-Bus) and `nmcli`.

Developed specifically for integration into projects like pi-touch-date, it allows scanning networks, managing interfaces, and establishing connections programmatically.

## Features

- **Network Scanning**: Detailed list of access points (SSID, BSSID, Signal, Security, Frequency, Channel).
- **Interface Management**: Monitor the status of Wi-Fi interfaces (wlan0, etc.).
- **Connection**: Support for connecting via saved profiles or creating new connections using PSK.
- **Flexible Output**: Tabular format for human use, detailed (verbose), or JSON for integration with other scripts.
- **D-Bus Integration**: Uses native NetworkManager APIs for maximum efficiency.

## Requirements

- **OS**: Linux with NetworkManager installed.
- **Runtime**: .NET 8.0 SDK or higher.
- **Permissions**: The user must have permissions to interact with NetworkManager (usually via D-Bus or sudo privileges for `nmcli`).

## CLI Usage

### Common commands

```bash
# Scan for available networks (default)
dotnet run --project WifiManager.Cli

# List Wi-Fi interfaces and their status
dotnet run --project WifiManager.Cli -- --list-ifaces

# Detailed status of a specific interface
dotnet run --project WifiManager.Cli -- --iface-status=wlan0
```

### Connection and disconnection

```bash
# Connect to a network (tries saved profiles first, otherwise prompts for PSK)
dotnet run --project WifiManager.Cli -- --connect wlan0 "Nome_SSID"

# Connect providing the password directly
dotnet run --project WifiManager.Cli -- --connect wlan0 "Nome_SSID" --psk "MiaPassword"

# Disconnect a specific interface
dotnet run --project WifiManager.Cli -- --disconnect wlan0
```

### Output options

```bash
# JSON output for automation
dotnet run --project WifiManager.Cli -- --json

# Extended information (Verbose)
dotnet run --project WifiManager.Cli -- --verbose

# Use NetworkManager cache without forcing a fresh scan
dotnet run --project WifiManager.Cli -- --no-scan
```

## Project structure

- **SharpWifiManager**: The core library that manages D-Bus logic and `nmcli` wrappers.
- **WifiManager.Cli**: Console application that exposes the library's functionality.

## WifiNetwork Object Example

The library returns `WifiNetwork` objects rich with metadata:
- `Ssid` / `Bssid`
- `StrengthPercent` (Signal quality 0-100)
- `FrequencyMHz` / `Channel`
- `Security` (WPA2, WPA3, etc.)
- `MaxBitrateMbps`

---
*Developed to run on Raspberry Pi and touch devices.*
