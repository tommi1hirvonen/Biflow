using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Biflow.Executor.Core;

[SuppressMessage(
    "Interoperability",
    "CA1416:Validate platform compatibility",
    Justification = "These extensions are used only when hosted as a Windows Service")]
internal static partial class WindowsExtensions
{
    private const int LOGON32_PROVIDER_DEFAULT = 0;
    private const int LOGON32_LOGON_INTERACTIVE = 2;

    private const int WindowStationAllAccess = 0x000f037f;
    private const int DesktopRightsAllAccess = 0x000f01ff;

    public static bool TryGetTokenHandle(string? domain, string username, string? password, [NotNullWhen(true)] out SafeAccessTokenHandle? token)
    {
        if (domain is null or { Length: 0 })
        {
            domain = ".";
        }
        if (LogonUserW(username, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out var handle))
        {
            token = new SafeAccessTokenHandle(handle);
            return true;
        }
        token = null;
        return false;
    }

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

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool LogonUserW(
        string lpszUsername,
        string? lpszDomain,
        string? lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out nint phToken);

    // Use .NET types in the simplest possible way to interop with WinAPI and avoid needing to use P/Invoke.

    private class AllowAccessRule(IdentityReference identity, int accessMask)
        : AccessRule(identity, accessMask, false, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);

    private class WindowObjectAccessSecurity(SafeHandle objectHandle)
        : NativeObjectSecurity(false, ResourceType.WindowObject, objectHandle, AccessControlSections.Access)
    {
        public void Persist(SafeHandle handle) => Persist(handle, AccessControlSections.Access);

        new public void AddAccessRule(AccessRule rule) => base.AddAccessRule(rule);

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
