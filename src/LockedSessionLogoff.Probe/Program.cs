// Created: 2026-10-15 09:45 America/Los_Angeles
// Path: src/LockedSessionLogoff.Probe/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: local read-only session diagnostic
// Arguments: --watch polls every two seconds; --layout-only validates marshalling only
// Usage: LockedSessionLogoff.Probe.exe [--watch | --layout-only]; Ctrl+C stops
using System.Runtime.InteropServices;
using System.Text.Json;
using LockedSessionLogoff.Windows;

if (args.Length > 1 || (args.Length == 1 && args[0] != "--watch" && args[0] != "--layout-only"))
{
    Console.Error.WriteLine("Usage: LockedSessionLogoff.Probe.exe [--watch | --layout-only]");
    return 2;
}
try
{
    SessionReader.CheckLayout();
    Console.WriteLine($"Read-only session diagnostic; architecture={RuntimeInformation.ProcessArchitecture}");
    if (args.Contains("--layout-only")) { Console.WriteLine("WTS layout checks passed"); return 0; }
    Console.Error.WriteLine("Output includes local account/session details. Redact before sharing; nothing is uploaded.");
    using var stop = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
    do
    {
        Console.WriteLine(JsonSerializer.Serialize(new { TimestampUtc = DateTimeOffset.UtcNow, Sessions = SessionReader.ReadAll() }));
        if (!args.Contains("--watch")) break;
        try { await Task.Delay(TimeSpan.FromSeconds(2), stop.Token); }
        catch (OperationCanceledException) { break; }
    } while (!stop.IsCancellationRequested);
    return 0;
}
catch (Exception e) { Console.Error.WriteLine($"Probe failed: {e.Message}"); return 1; }
