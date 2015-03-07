using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Procme
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    public enum AllocationProtect : uint
    {
        PAGE_EXECUTE = 0x00000010,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
        PAGE_NOACCESS = 0x00000001,
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_GUARD = 0x00000100,
        PAGE_NOCACHE = 0x00000200,
        PAGE_WRITECOMBINE = 0x00000400
    }

    public static class Extension
    {
        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);


        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
        ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr
        phtok);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name,
        ref long pluid);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;



        public static int VirtualQuery_(IntPtr hProcess, long lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
        {
            return VirtualQueryEx(hProcess, (IntPtr)lpAddress, out lpBuffer, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
        }

        public static string ToStringBARR(this byte[] input)
        {
            string ret = "";
            foreach (byte b in input) ret += b.ToString("X2") + ' ';
            return ret;
        }

        public static byte[] ByteStringToArray(string input)
        {
            List<byte> ret = new List<byte>();
            input = input.Replace(" ", "");
            input = input.Trim();
            if (input.Length % 2 == 1) throw new Exception("Could not convert Byte String to Array. Size incorrect");
            for (int i = 0; i < input.Length; i += 2)
            {
                ret.Add(byte.Parse(input.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return ret.ToArray();
        }
        
        public static long FindAoB(IntPtr pHandle, long pStart, byte[] AoB, byte skipbyte = 0xAA, bool skipbyteset = true)
        {
            long pMaxLen = 0x07ffffff;
            const long BUFF_SIZE = 0x10000;
            DateTime now = DateTime.Now;
            while (pStart < pMaxLen)
            {
                long lenAvailable = Math.Min(pMaxLen - pStart, BUFF_SIZE);
                byte[] data = new byte[lenAvailable];
                Memory.ReadMemory(pHandle, pStart, ref data, (int)lenAvailable);

                int tmpint = 0;
                int res = Array.FindIndex(data, 0, data.Length, (byte b) =>
                {
                    if (skipbyteset && AoB[tmpint] == skipbyte)
                        tmpint += 1;
                    else if (b == AoB[tmpint])
                        tmpint += 1;
                    else
                        tmpint = 0;
                    return tmpint == AoB.Length;
                });

                if (res >= 0)
                {
                    DLOG.WriteLine("{0} miliseconds", (DateTime.Now - now).TotalMilliseconds);
                    return pStart + res + 1 - tmpint;
                }
                pStart -= tmpint; // To compensate w/ missing bytes
                pStart += lenAvailable;
            }
            DLOG.WriteLine("{0} miliseconds", (DateTime.Now - now).TotalMilliseconds);
            DLOG.WriteLine("-- Not found.");
            return 0;
        }

        static Random rnd = new Random();
        public static int WriteString(ProcessStream ps, byte[] pDecodeKey, int pArrayPos, int pStringPos, string pValue, bool pIsBstr)
        {
            ps.Position = pArrayPos;
            ps.WriteMemory(pStringPos);

            ps.Position = pStringPos;
            byte[] data;
            if (pIsBstr)
            {
                data = Encoding.GetEncoding(949).GetBytes(pValue);
            }
            else
            {
                data = Encoding.ASCII.GetBytes(pValue);
            }
            
            byte[] encrypted = EncryptString(data, pDecodeKey, (byte)rnd.Next(255));
            ps.WriteMemory(encrypted);

            return encrypted.Length;
        }

        public static byte[] EncryptString(byte[] value, byte[] pDecodeKey, byte shift)
        {
            int len = pDecodeKey.Length;
            byte[] lulzkey = RotateLeft(pDecodeKey, len, shift);
            List<byte> buf = new List<byte>();

            buf.Add(shift);
            int j = 0;
            for (int i = 0; i < value.Length; i++)
            {
                byte lul = lulzkey[j % len];
                byte esb = value[i];
                byte esb2 = (i + 2 >= value.Length ? (byte)';' : value[i + 1]);
                byte tw = 0;

                if (esb == '\\' && esb2 == 'n')
                {
                    tw = (byte)'\n';
                    i++;
                }
                else if (esb == '\\' && esb2 == 'r')
                {
                    tw = (byte)'\r';
                    i++;
                }
                else tw = (byte)esb;

                if (tw == lul)
                {
                    tw = (byte)lul;
                }
                else
                {
                    tw = (byte)(tw ^ lul);
                }

                buf.Add(tw);
                j++;
            }
            buf.Add(0x00);
            return buf.ToArray();
        }

        public static string DecryptString(byte[] value, byte[] pDecodeKey, byte shift)
        {
            int len = pDecodeKey.Length;
            byte[] lulzkey = RotateLeft(pDecodeKey, len, shift);
            string ret = "";

            int j = 0;
            for (int i = 1; i < value.Length; i++)
            {
                byte lul = lulzkey[j % len];
                byte esb = value[i];
                byte esb2 = (i + 2 >= value.Length ? (byte)';' : value[i + 1]);

                if (esb == lul)
                {
                    esb = (byte)lul;
                }
                else
                {
                    esb = (byte)(esb ^ lul);
                }

                if (esb == '\r')
                {
                    ret += @"\r";
                }
                else if (esb == '\n')
                {
                    ret += @"\n";
                }
                else
                {
                    ret += (char)esb;
                }
                j++;
            }

            return ret;
        }

        public static string DecodeString(ProcessStream ps, byte[] pDecodeKey, int pStringPos, out bool pIsBstr, out int pStringStart, out int pStringEnd)
        {
            ps.Position = pStringPos;
            int realPos = ps.ReadInt();

            ps.Position = realPos;

            pStringStart = (int)ps.Position;

            sbyte shiftByte = ps.ReadSByte();
            int decodeKeyLen = pDecodeKey.Length;
            byte[] lulzkey = RotateLeft(pDecodeKey, decodeKeyLen, (int)shiftByte);

            List<byte> encryptedStringBuffer = new List<byte>();
            while (true)
            {
                byte ch = ps.ReadByte();
                if (ch == 0) break;
                encryptedStringBuffer.Add(ch);
            }

            pStringEnd = (int)ps.Position;

            string ret = "";
            int i = 0;
            for (; i < encryptedStringBuffer.Count; i++)
            {
                byte lul = lulzkey[i % decodeKeyLen];
                byte esb = encryptedStringBuffer[i];

                if (esb == lul)
                {
                    esb = lul;
                }
                else
                {
                    esb ^= lul;
                }
                if (esb == '\r') ret += @"\r";
                else if (esb == '\n') ret += @"\n";
                else ret += (char)esb;
            }

            pIsBstr = false;
            for (i = 0; i < ret.Length; i++)
            {
                if (ret[i] > sbyte.MaxValue)
                {
                    pIsBstr = true;
                    break;
                }
            }

            if (pIsBstr)
            {
                byte[] buffer = new byte[ret.Length];
                for (i = 0; i < ret.Length; i++)
                {
                    buffer[i] = (byte)ret[i];
                }
                ret = Encoding.GetEncoding(949).GetString(buffer);
            }

            return ret;
        }

        public static byte[] RotateLeft(byte[] value, int length, int shift)
        {
            byte[] v4 = new byte[length];
            Buffer.BlockCopy(value, 0, v4, 0, length);
            if ((uint)shift < 8)
            {
                goto label_26;
            }
            uint v5 = (((uint)shift >> 3) % (uint)length);
            if (v5 != 0)
            {

                if (length != 0)
                {
                    for (int i = 0; i < length; i++)
                    {
                        v4[i] = value[(i + v5) % length];
                    }
                }
            }
        label_26:
            bool v12 = (shift & 7) == 0;
            shift &= 7;
            if (!v12)
            {
                int v14 = 8 - shift;
                int v13 = 0;
                int v18 = 8 - shift;
                int v20 = 0;
                if (length > 1)
                {
                    byte tmp = v4[0];
                    tmp >>= v14;
                    v13 = v20 = tmp;
                }
                int v15 = 0;
                if (length != 0)
                {
                    for (int i = v15; i < length; i++)
                    {
                        byte b = 0;
                        if (i != length - 1)
                            b = (byte)(v4[i + 1] >> v18);
                        byte c = (byte)(v4[i] << shift);
                        c |= b;
                        v4[i] = c;
                    }
                    v13 = v20;
                }
                v4[length - 1] |= (byte)v13;
            }
            return v4;
        }
    }
}
