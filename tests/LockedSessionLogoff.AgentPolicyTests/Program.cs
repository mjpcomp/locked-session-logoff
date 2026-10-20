// Created: 2026-10-20 12:08 America/Los_Angeles
// Path: tests/LockedSessionLogoff.AgentPolicyTests/Program.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: in-memory framing, replay, policy and peer-change cases
// Arguments: none; Usage: no native pipe/service/registry/action calls
using System.Text;
using System.Text.Json;
using LockedSessionLogoff.Windows;
var peer = new AgentPeer(new(1, "S-1-5-21-1-2-3-1001", 123), 42, 1234, "synthetic");
var policy = new AgentIdlePolicy(true, 20, new string('A', 64), "ConfiguredDryRun", []);
var nonce = Guid.NewGuid().ToString("N"); var epoch = Guid.NewGuid().ToString("N");
var tests = new (string Name, Action Run)[]
{
    ("Frame round-trip", () => { using var m = new MemoryStream(); AgentFrames.WriteAsync(m, new AgentHello(1, "Hello")).GetAwaiter().GetResult(); m.Position = 0; Check(AgentFrames.ReadAsync<AgentHello>(m).GetAwaiter().GetResult() == new AgentHello(1, "Hello")); }),
    ("Oversized output rejected", () => Reject(() => { using var m = new MemoryStream(); AgentFrames.WriteAsync(m, new AgentHello(1, new string('X', 5000))).GetAwaiter().GetResult(); })),
    ("Zero size rejected", () => Reject(() => ReadBytes(new byte[4]))),
    ("Oversized input rejected", () => Reject(() => ReadBytes([1, 16, 0, 0]))),
    ("Truncated input rejected", () => Reject(() => ReadBytes([5, 0, 0, 0, 123]))),
    ("Unknown identity claims rejected", () => Reject(() => ReadJson("{\"Version\":1,\"Kind\":\"Hello\",\"SessionId\":1}"))),
    ("Duplicate fields rejected", () => Reject(() => ReadJson("{\"Version\":1,\"Version\":1,\"Kind\":\"Hello\"}"))),
    ("Valid challenge consumed", () => { var c = new AgentChallenge(); c.Consume(new(1, "GetIdlePolicy", c.Nonce), TimeSpan.Zero); }),
    ("Wrong challenge rejected", () => Reject(() => new AgentChallenge().Consume(new(1, "GetIdlePolicy", nonce), TimeSpan.Zero))),
    ("Wrong protocol rejected", () => Reject(() => { var c = new AgentChallenge(); c.Consume(new(2, "GetIdlePolicy", c.Nonce), TimeSpan.Zero); })),
    ("Action-shaped request rejected", () => Reject(() => { var c = new AgentChallenge(); c.Consume(new(1, "Lock", c.Nonce), TimeSpan.Zero); })),
    ("Replay rejected", () => Reject(() => { var c = new AgentChallenge(); var q = new AgentQuery(1, "GetIdlePolicy", c.Nonce); c.Consume(q, TimeSpan.Zero); c.Consume(q, TimeSpan.Zero); })),
    ("Expired challenge rejected", () => Reject(() => { var c = new AgentChallenge(); c.Consume(new(1, "GetIdlePolicy", c.Nonce), TimeSpan.FromSeconds(5)); })),
    ("Allowed policy always dry-run/no actions", () => { var r = AgentPolicyRules.ForPeer(policy, peer, nonce, epoch); Check(r.Enabled && r.DryRun && !r.ActionsEnabled); AgentPolicyRules.ValidateReply(r, nonce); }),
    ("SID exclusion applied server-side", () => { var r = AgentPolicyRules.ForPeer(policy with { ExcludedSids = [peer.Identity.Sid] }, peer, nonce, epoch); Check(!r.Enabled && r.Mode == "ExcludedBySid" && r.DryRun && !r.ActionsEnabled); AgentPolicyRules.ValidateReply(r, nonce); }),
    ("Disabled policy remains disabled", () => Check(!AgentPolicyRules.ForPeer(policy with { Enabled = false, Mode = "Disabled" }, peer, nonce, epoch).Enabled)),
    ("Enabled revision required", () => Reject(() => AgentPolicyRules.ValidatePolicy(policy with { Revision = null }))),
    ("Invalid timeout rejected", () => Reject(() => AgentPolicyRules.ValidatePolicy(policy with { IdleTimeoutSeconds = 1 }))),
    ("Session zero rejected", () => Reject(() => AgentPolicyRules.ForPeer(policy, peer with { Identity = peer.Identity with { SessionId = 0 } }, nonce, epoch))),
    ("Action-enabled reply rejected", () => Reject(() => AgentPolicyRules.ValidateReply(AgentPolicyRules.ForPeer(policy, peer, nonce, epoch) with { ActionsEnabled = true }, nonce))),
    ("Reply nonce mismatch rejected", () => Reject(() => AgentPolicyRules.ValidateReply(AgentPolicyRules.ForPeer(policy, peer, nonce, epoch), Guid.NewGuid().ToString("N")))),
    ("Changed authentication identity rejected", () => Reject(() => AgentPolicyRules.RequireSamePeer(peer, peer with { Identity = peer.Identity with { AuthenticationId = 456 } }))),
    ("Reused PID generation rejected", () => Reject(() => AgentPolicyRules.RequireSamePeer(peer, peer with { ProcessStartTicks = 9999 })))
};
int failed = 0;
foreach (var test in tests) { try { test.Run(); Console.WriteLine($"PASS {test.Name}"); } catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {e.Message}"); } }
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed"); return failed == 0 ? 0 : 1;
static void Check(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
static void Reject(Action action) { try { action(); } catch (Exception e) when (e is InvalidDataException or JsonException or EndOfStreamException or UnauthorizedAccessException) { return; } throw new Exception("Expected rejection"); }
static void ReadBytes(byte[] bytes) { using var m = new MemoryStream(bytes); AgentFrames.ReadAsync<AgentHello>(m).GetAwaiter().GetResult(); }
static void ReadJson(string json) { var data = Encoding.UTF8.GetBytes(json); using var m = new MemoryStream(); m.Write(BitConverter.GetBytes(data.Length)); m.Write(data); m.Position = 0; AgentFrames.ReadAsync<AgentHello>(m).GetAwaiter().GetResult(); }
