using System.Runtime.InteropServices;
using System.Text;

namespace Services
{
    public static class CredentialManager
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;

            public uint Type;

            public nint TargetName;

            public nint Comment;

            public FILETIME LastWritten;

            public uint CredentialBlobSize;

            public nint CredentialBlob;

            public uint Persist;

            public uint AttributeCount;

            public nint Attributes;

            public nint TargetAlias;

            public nint UserName;
        }

        private struct FILETIME
        {
            public uint dwLowDateTime;

            public uint dwHighDateTime;
        }

        private const uint CRED_TYPE_GENERIC = 1u;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, uint type, uint flags, out nint credential);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredFree([In] nint buffer);

        public static void SaveCredential(string target, string token)
        {
            byte[] credentialBlob = Encoding.Unicode.GetBytes(token);
            CREDENTIAL cREDENTIAL = default;
            cREDENTIAL.Type = 1u;
            cREDENTIAL.TargetName = Marshal.StringToCoTaskMemUni(target);
            cREDENTIAL.CredentialBlobSize = (uint)credentialBlob.Length;
            cREDENTIAL.CredentialBlob = Marshal.AllocCoTaskMem(credentialBlob.Length);
            cREDENTIAL.Persist = 2u;
            cREDENTIAL.UserName = Marshal.StringToCoTaskMemUni(Environment.UserName);
            CREDENTIAL credential = cREDENTIAL;
            Marshal.Copy(credentialBlob, 0, credential.CredentialBlob, credentialBlob.Length);
            if (!CredWrite(ref credential, 0u))
            {
                throw new Exception("Failed to save credential.");
            }
            Marshal.FreeCoTaskMem(credential.TargetName);
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
            Marshal.FreeCoTaskMem(credential.UserName);
        }

        public static string RetrieveCredential(string target)
        {
            if (!CredRead(target, 1u, 0u, out var credentialPtr))
            {
                throw new Exception("Failed to retrieve credential.");
            }
            if (credentialPtr == nint.Zero)
            {
                throw new Exception("Credential pointer is null.");
            }

            // Attempt to marshal the pointer to a CREDENTIAL structure
            CREDENTIAL? credentialStruct = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

            // Check if the marshaled structure is null
            if (credentialStruct == null)
            {
                throw new Exception("Failed to marshal credential structure.");
            }

            CREDENTIAL credential = credentialStruct.Value;

            byte[] credentialBlob = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, credentialBlob, 0, (int)credential.CredentialBlobSize);
            string credentialString = Encoding.Unicode.GetString(credentialBlob);
            CredFree(credentialPtr);
            return credentialString;
        }
    }
}