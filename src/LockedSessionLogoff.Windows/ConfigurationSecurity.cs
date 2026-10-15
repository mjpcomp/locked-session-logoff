// Created: 2026-10-15 11:21 America/Los_Angeles
// Path: src/LockedSessionLogoff.Windows/ConfigurationSecurity.cs; Owner: maintainers
// Permissions: public read/maintainer write; Purpose: reject unsafe keys/serialize writers
// Arguments: existing registry key or none for writer lease; Usage: using WriterLease()
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;

namespace LockedSessionLogoff.Windows;

public static class ConfigurationSecurity
{
    private static readonly SecurityIdentifier Admins = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);
    private static bool Trusted(IdentityReference? sid) => sid == Admins || sid == SystemSid;

    public static void Verify(RegistryKey key)
    {
        var acl = key.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
        if (!Trusted(acl.GetOwner(typeof(SecurityIdentifier))))
            throw new UnauthorizedAccessException("Configuration key owner is not SYSTEM/Administrators");
        var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
        if (rules.Count == 0) throw new UnauthorizedAccessException("Empty configuration ACL");
        foreach (RegistryAccessRule rule in rules)
        {
            if ((rule.PropagationFlags & PropagationFlags.InheritOnly) != 0) continue;
            if (rule.AccessControlType == AccessControlType.Allow && !Trusted(rule.IdentityReference) &&
                (unchecked((uint)rule.RegistryRights) & 0x500D0026u) != 0)
                throw new UnauthorizedAccessException("Configuration key grants untrusted write access");
        }
    }

    public static IDisposable WriterLease()
    {
        var acl = new MutexSecurity();
        acl.SetAccessRuleProtection(true, false);
        acl.SetOwner(Admins);
        acl.AddAccessRule(new MutexAccessRule(Admins, MutexRights.FullControl, AccessControlType.Allow));
        acl.AddAccessRule(new MutexAccessRule(SystemSid, MutexRights.FullControl, AccessControlType.Allow));
        var mutex = MutexAcl.Create(false, @"Global\LockedSessionLogoff.ConfigurationWriter", out _, acl);
        try
        {
            var actual = mutex.GetAccessControl();
            if (!Trusted(actual.GetOwner(typeof(SecurityIdentifier))) || !actual.AreAccessRulesProtected)
                throw new UnauthorizedAccessException("Untrusted writer mutex");
            foreach (MutexAccessRule rule in actual.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && !Trusted(rule.IdentityReference))
                    throw new UnauthorizedAccessException("Untrusted writer mutex grant");
            try { if (!mutex.WaitOne(TimeSpan.FromSeconds(5))) throw new TimeoutException("Configuration writer busy"); }
            catch (AbandonedMutexException) { }
            return new Lease(mutex);
        }
        catch { mutex.Dispose(); throw; }
    }

    private sealed class Lease(Mutex mutex) : IDisposable
    {
        public void Dispose() { try { mutex.ReleaseMutex(); } finally { mutex.Dispose(); } }
    }
}
