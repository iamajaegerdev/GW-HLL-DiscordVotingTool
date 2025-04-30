using System.Runtime.InteropServices;

internal static class CredentialManagerHelpers
{

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredFree([In] nint buffer);
}