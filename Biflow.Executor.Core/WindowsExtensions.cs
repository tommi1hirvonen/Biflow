using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Biflow.Executor.Core;

[SupportedOSPlatform("windows")]
internal static partial class WindowsExtensions
{
    private const int WindowStationAllAccess = 0x000f037f;
    private const int DesktopRightsAllAccess = 0x000f01ff;

    public static void GrantAccessToWindowStationAndDesktop(string? domainName, string username)
    {
        // Domain name passed to NTAccount can be null or an empty string.
        var account = new NTAccount(domainName ?? "", username);

        var windowStationHandle = GetProcessWindowStation();
        GrantAccess(account, windowStationHandle, WindowStationAllAccess);

        var threadId = GetCurrentThreadId();
        var desktopHandle = GetThreadDesktop(threadId);
        GrantAccess(account, desktopHandle, DesktopRightsAllAccess);
    }

    private static void GrantAccess(NTAccount account, nint handle, int accessMask)
    {
        var safeHandle = new NoCloseSafeHandle(handle);
        var security = new WindowObjectAccessSecurity(safeHandle);
        var accessRule = new AllowAccessRule(account, accessMask);
        security.AddAccessRule(accessRule);
        security.Persist(safeHandle);
    }

    // Use source generators for interop functions.

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetProcessWindowStation();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint GetThreadDesktop(int dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetCurrentThreadId();

    // Use .NET types in the simplest possible way to interop with WinAPI and avoid needing to use P/Invoke.

    private class AllowAccessRule(IdentityReference identity, int accessMask)
        : AccessRule(identity, accessMask, false, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);

    private class WindowObjectAccessSecurity(SafeHandle objectHandle)
        : NativeObjectSecurity(false, ResourceType.WindowObject, objectHandle, AccessControlSections.Access)
    {
        public void Persist(SafeHandle handle) => Persist(handle, AccessControlSections.Access);

        public new void AddAccessRule(AccessRule rule) => base.AddAccessRule(rule);

        public override Type AccessRightType => throw new NotImplementedException();

        public override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type) => throw new NotImplementedException();

        public override Type AccessRuleType => typeof(AccessRule);

        public override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags) => throw new NotImplementedException();

        public override Type AuditRuleType => typeof(AuditRule);
    }

    // Helper class for the handles returned by GetProcessWindowStation() (handle should not be closed)
    // and GetThreadDesktop() (handle does not need to be closed).
    private class NoCloseSafeHandle(nint handle) : SafeHandle(handle, false)
    {
        public override bool IsInvalid => false;

        protected override bool ReleaseHandle() => true; // Don't close handle => do nothing
    }
}
