// Created: 2026-10-20 12:08 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/AgentPolicyChannel.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: bounded read-only policy exchange, not an action/control API
// Arguments: connected server pipe, trusted policy provider/epoch, cancellation; Usage: future SYSTEM host; not auto-started
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace LockedSessionLogoff.Windows;
public sealed record AgentHello(int Version, string Kind);
public sealed record AgentQuery(int Version, string Kind, string Nonce);
public sealed record AgentChallengeMessage(int Version, string Nonce);
public sealed record AgentIdlePolicy(bool Enabled, int IdleTimeoutSeconds, string? Revision, string Mode, string[] ExcludedSids);
public sealed record AgentPolicyReply(int Version, string Nonce, string ServerEpoch, bool Enabled, bool DryRun, bool ActionsEnabled, int IdleTimeoutSeconds, string? Revision, string Mode);
public sealed record AgentPeer(TokenIdentity Identity, int ProcessId, long ProcessStartTicks, string WtsIdentity);

public static class AgentFrames
{
    public const int MaximumBytes = 4096;
    private static readonly JsonSerializerOptions Options = new() { MaxDepth = 4, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancel = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        if (data.Length is < 1 or > MaximumBytes) throw new InvalidDataException("Frame size rejected");
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, data.Length);
        await stream.WriteAsync(header, cancel); await stream.WriteAsync(data, cancel); await stream.FlushAsync(cancel);
    }
    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancel = default) where T : class
    {
        var header = new byte[4]; await stream.ReadExactlyAsync(header, cancel);
        int size = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (size is < 1 or > MaximumBytes) throw new InvalidDataException("Frame size rejected");
        var data = new byte[size]; await stream.ReadExactlyAsync(data, cancel);
        using var document = JsonDocument.Parse(data, new JsonDocumentOptions { MaxDepth = 4 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Object required");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate property rejected");
        return JsonSerializer.Deserialize<T>(data, Options) ?? throw new InvalidDataException("Null message");
    }
}
public sealed class AgentChallenge
{
    public string Nonce { get; } = Guid.NewGuid().ToString("N");
    private bool consumed;
    public void Consume(AgentQuery query, TimeSpan elapsed)
    {
        if (consumed) throw new InvalidDataException("Challenge already consumed");
        consumed = true;
        if (elapsed < TimeSpan.Zero || elapsed >= TimeSpan.FromSeconds(5) || query.Version != 1 || query.Kind != "GetIdlePolicy"
            || !Matches(Nonce, query.Nonce)) throw new InvalidDataException("Invalid/stale challenge");
    }
    public static bool Matches(string expected, string? actual) => Guid.TryParseExact(expected, "N", out _)
        && Guid.TryParseExact(actual, "N", out _)
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual!));
}
public static class AgentPolicyRules
{
    public static void ValidatePolicy(AgentIdlePolicy policy)
    {
        if (policy.IdleTimeoutSeconds is < 5 or > 604800 || policy.Mode is not ("Disabled" or "AwaitingDisableThenEnable" or "ReadFailed" or "ConfiguredDryRun")
            || policy.Enabled != (policy.Mode == "ConfiguredDryRun")
            || (policy.Revision is not null && !ValidRevision(policy.Revision)) || (policy.Enabled && policy.Revision is null))
            throw new InvalidDataException("Invalid authoritative idle policy");
        new AdminSettings { ExcludedSids = policy.ExcludedSids }.Validate();
    }
    private static bool ValidRevision(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
    public static AgentPolicyReply ForPeer(AgentIdlePolicy policy, AgentPeer peer, string nonce, string epoch)
    {
        ValidatePolicy(policy);
        if (!Guid.TryParseExact(nonce, "N", out _) || !Guid.TryParseExact(epoch, "N", out _) || peer.Identity.SessionId <= 0
            || peer.Identity.AuthenticationId == 0 || peer.ProcessId <= 0 || peer.ProcessStartTicks <= 0 || string.IsNullOrEmpty(peer.WtsIdentity)
            || string.IsNullOrWhiteSpace(peer.Identity.Sid) || peer.Identity.Sid == "S-1-5-18") throw new InvalidDataException("Invalid peer/context");
        bool excluded = policy.ExcludedSids.Contains(peer.Identity.Sid, StringComparer.OrdinalIgnoreCase);
        return new(1, nonce, epoch, policy.Enabled && !excluded, true, false, policy.IdleTimeoutSeconds, policy.Revision, excluded ? "ExcludedBySid" : policy.Mode);
    }
    public static void ValidateReply(AgentPolicyReply reply, string nonce)
    {
        if (reply.Version != 1 || !AgentChallenge.Matches(nonce, reply.Nonce) || !Guid.TryParseExact(reply.ServerEpoch, "N", out _)
            || !reply.DryRun || reply.ActionsEnabled || reply.IdleTimeoutSeconds is < 5 or > 604800
            || reply.Mode is not ("Disabled" or "AwaitingDisableThenEnable" or "ReadFailed" or "ConfiguredDryRun" or "ExcludedBySid")
            || reply.Enabled != (reply.Mode == "ConfiguredDryRun") || (reply.Revision is not null && !ValidRevision(reply.Revision))
            || (reply.Enabled && reply.Revision is null)) throw new InvalidDataException("Invalid read-only policy reply");
    }
    public static void RequireSamePeer(AgentPeer before, AgentPeer after)
    { if (before != after) throw new UnauthorizedAccessException("Pipe peer changed"); }
}
public static class AgentPolicyChannel
{
    public static async Task ServeOnceAsync(NamedPipeServerStream pipe, Func<AgentPeer, AgentIdlePolicy> policyProvider, string serverEpoch, CancellationToken cancel = default)
    {
        AgentPipePeer.RequireSystem();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancel); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var ct = deadline.Token; long started = Stopwatch.GetTimestamp();
        var hello = await AgentFrames.ReadAsync<AgentHello>(pipe, ct);
        if (hello.Version != 1 || hello.Kind != "Hello") throw new InvalidDataException("Unsupported hello");
        var peer = AgentPipePeer.ReadClient(pipe);
        var challenge = new AgentChallenge();
        await AgentFrames.WriteAsync(pipe, new AgentChallengeMessage(1, challenge.Nonce), ct);
        var query = await AgentFrames.ReadAsync<AgentQuery>(pipe, ct);
        challenge.Consume(query, Stopwatch.GetElapsedTime(started));
        AgentPolicyRules.RequireSamePeer(peer, AgentPipePeer.ReadClient(pipe));
        var reply = AgentPolicyRules.ForPeer(policyProvider(peer), peer, challenge.Nonce, serverEpoch);
        AgentPolicyRules.RequireSamePeer(peer, AgentPipePeer.ReadClient(pipe));
        await AgentFrames.WriteAsync(pipe, reply, ct);
        var ack = await AgentFrames.ReadAsync<AgentQuery>(pipe, ct);
        if (ack.Version != 1 || ack.Kind != "PolicyReceived" || !AgentChallenge.Matches(challenge.Nonce, ack.Nonce))
            throw new InvalidDataException("Invalid receipt");
    }
    public static async Task<AgentPolicyReply> QueryAsync(CancellationToken cancel = default)
    {
        AgentPipePeer.RequireNormalUser();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancel); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var ct = deadline.Token;
        using var pipe = await AgentPipePeer.ConnectAsync(ct);
        var server = AgentPipePeer.ReadServiceInstance(pipe);
        await AgentFrames.WriteAsync(pipe, new AgentHello(1, "Hello"), ct);
        var challenge = await AgentFrames.ReadAsync<AgentChallengeMessage>(pipe, ct);
        if (challenge.Version != 1 || !Guid.TryParseExact(challenge.Nonce, "N", out _)) throw new InvalidDataException("Invalid challenge");
        await AgentFrames.WriteAsync(pipe, new AgentQuery(1, "GetIdlePolicy", challenge.Nonce), ct);
        var reply = await AgentFrames.ReadAsync<AgentPolicyReply>(pipe, ct);
        AgentPolicyRules.ValidateReply(reply, challenge.Nonce);
        if (server != AgentPipePeer.ReadServiceInstance(pipe)) throw new UnauthorizedAccessException("Service instance changed");
        await AgentFrames.WriteAsync(pipe, new AgentQuery(1, "PolicyReceived", challenge.Nonce), ct);
        return reply;
    }
}
