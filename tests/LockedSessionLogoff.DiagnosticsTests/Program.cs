// Created: 2026-10-15 13:13 America/Los_Angeles
// Path: tests/LockedSessionLogoff.DiagnosticsTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: retention, deduplication and inactive wording tests
// Arguments: none; Usage: synthetic only; no registry writes/services/native actions
using System.Text;
using System.Text.Json;
using LockedSessionLogoff.Windows;
const string a = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", b = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
var time = DateTimeOffset.UnixEpoch.AddDays(1);
var sample = new ServiceReport(1, 42, time.UtcDateTime.Ticks, a, time.AddSeconds(1), "Running", null, ["event-one"]);
var first = sample with { RetainedEvents = EventHistory.Update(null, sample) };
var missing = new ConfigurationGate().Observe(null, null);
var disabled = new EffectiveConfiguration(false, true, false, 20, "revision", "Disabled");
var tests = new (string Name, Action Run)[]
{
    ("Missing settings friendly", () => Check(ServiceDisplay.Mode(missing) == "No saved settings - monitoring disabled")),
    ("Missing timeout inactive", () => Check(ServiceDisplay.Timeout(missing) == "Inactive")),
    ("Disabled timeout inactive", () => Check(ServiceDisplay.Timeout(disabled) == "Inactive")),
    ("Active timeout visible", () => Check(ServiceDisplay.Timeout(disabled with { Enabled = true }) == "20s")),
    ("Both absent explicit", () => Check(ServiceDisplay.Revision(true, null, missing) == "Both absent - no saved settings")),
    ("Failed editor not matching", () => Check(ServiceDisplay.Revision(false, null, missing) == "Unavailable")),
    ("Run start retained", () => Check(first.RetainedEvents.Any(e => e.Text.EndsWith("ServiceRunStarted")))),
    ("Repeated snapshot deduplicated", () => Check(EventHistory.Update(first, sample).SequenceEqual(first.RetainedEvents))),
    ("New run preserves prior history", () => { var rows = EventHistory.Update(first, sample with { RunId = b }); Check(rows.Any(e => e.RunId == a) && rows.Any(e => e.RunId == b)); }),
    ("Stop marker retained once", () => { var stop = sample with { State = "Stopped" }; stop = stop with { RetainedEvents = EventHistory.Update(first, stop) }; Check(stop.RetainedEvents.Count(e => e.Text.EndsWith("ServiceRunStopped")) == 1 && EventHistory.Update(stop, stop).SequenceEqual(stop.RetainedEvents)); }),
    ("Count bounded oldest evicted", () => { var rows = EventHistory.Merge([], Enumerable.Range(0, 200).Select(i => new RetainedEvent(a, i.ToString()))); Check(rows.Length == 128 && rows[0].Text == "72" && rows[^1].Text == "199"); }),
    ("Escaped JSON bytes bounded", () => { var rows = EventHistory.Merge([], Enumerable.Range(0, 50).Select(i => new RetainedEvent(a, i + new string('<', 2000)))); Check(rows.Length > 0 && Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(rows)) <= EventHistory.MaxBytes); }),
    ("Multiline rejected", () => Reject(() => EventHistory.Merge([], [new(a, "one\ntwo")]))),
    ("Invalid run rejected", () => Reject(() => EventHistory.Merge([], [new("invalid", "event")]))),
    ("Oversized entry rejected", () => Reject(() => EventHistory.Merge([], [new(a, new string('X', EventHistory.MaxText + 1))]))),
    ("Previous input unmodified", () => { var before = first.RetainedEvents.ToArray(); EventHistory.Update(first, sample with { RunId = b }); Check(first.RetainedEvents.SequenceEqual(before)); }),
    ("Oversized current snapshot rejected", () => Reject(() => EventHistory.Update(null, sample with { RecentEvents = Enumerable.Repeat("event", 25).ToArray() })))
};
int failures = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed"); return failures == 0 ? 0 : 1;
static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Expected rejection"); }
