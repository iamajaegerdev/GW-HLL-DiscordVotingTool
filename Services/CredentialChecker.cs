using System.Runtime.InteropServices;

namespace Services
{
    public class CredentialChecker
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CREDENTIAL
        {
            public uint Flags;

            public uint Type;

            public nint TargetName;

            public nint Comment;

            //public FILETIME LastWritten;

            public uint CredentialBlobSize;

            public nint CredentialBlob;

            public uint Persist;

            public uint AttributeCount;

            public nint Attributes;

            public nint TargetAlias;

            public nint UserName;
        }

        //internal struct FILETIME
        //{
        //    public uint dwLowDateTime;

        //    public uint dwHighDateTime;
        //}

        internal const uint CRED_TYPE_GENERIC = 1u;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CredRead(string target, uint type, uint reservedFlag, out nint credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern void CredFree(nint buffer);

        public static bool CheckCredentials(string target)
        {
            bool num = CredRead(target, 1u, 0u, out nint credPtr);
            if (num)
            {
                CredFree(credPtr);
            }
            return num;
        }
    }
}