using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MapleBypass
{
    public static partial class Injector
    {

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress,
          IntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, Int32 dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(IntPtr handle, Int32 milliseconds);

        public static Int32 GetProcessId(String proc)
        {
            Process[] ProcList;
            ProcList = Process.GetProcessesByName(proc);
            return ProcList[0].Id;
        }

        public static void InjectDLL(IntPtr hProcess, String strDLLName)
        {
            IntPtr bytesout;

            Int32 LenWrite = strDLLName.Length + 1;
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40);
            byte[] buff = new byte[strDLLName.Length];
            for (int i = 0; i < strDLLName.Length; i++) buff[i] = (byte)strDLLName[i];

            WriteProcessMemory(hProcess, AllocMem, buff, (uint)LenWrite, out bytesout);
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                return;
            }

            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            if (hThread == null)
            {
                return;
            }
            int Result = WaitForSingleObject(hThread, 10 * 1000);
            if (Result == 0x00000080L || Result == 0x00000102L || Result == -1)
            {
                if (hThread != null)
                {
                    CloseHandle(hThread);
                }
                return;
            }

            Thread.Sleep(1000);

            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);
            if (hThread != null)
            {

                CloseHandle(hThread);
            }
            return;
        }

        public static bool RemoveMutex(int ProcID)
        {
            if (ProcID >= 0)
            {
                IntPtr hProcess = OpenProcess(0x1F0FFF, 1, ProcID);
                if (hProcess == null)
                {
                    Console.WriteLine("Failed getting process?");
                    return false;
                }
                else
                {
                    IntPtr handle = GetModuleHandle("Kernel32.dll");
                    if (handle == IntPtr.Zero)
                    {
                        CloseHandle(hProcess);
                        return true;
                    }

                    UIntPtr addr = GetProcAddress(handle, "CreateMutexA");
                    if (addr == UIntPtr.Zero)
                    {
                        CloseHandle(hProcess);
                        return true;
                    }

                    uint oldProtect;
                    IntPtr convertedAddr = unchecked((IntPtr)(long)(ulong)addr);
                    if (!VirtualProtectEx(hProcess, convertedAddr, new UIntPtr(0x0A), 0x80, out oldProtect))
                    {
                        CloseHandle(hProcess);
                        return false;
                    }

                    IntPtr writtenBytes;
                    if (!WriteProcessMemory(hProcess, convertedAddr, new byte[] { 0xC2, 0x1C, 0x00 }, (uint)3, out writtenBytes))
                    {
                        CloseHandle(hProcess);
                        return false;
                    }

                    if (!VirtualProtectEx(hProcess, convertedAddr, new UIntPtr(0x0A), 0x10, out oldProtect))
                    {
                        CloseHandle(hProcess);
                        return false;
                    }
                    return true;
                }
            }

            return false;
        }

        public static bool Inject(string pProcessName, string pDllPath)
        {
            Int32 ProcID = GetProcessId(pProcessName);
            if (ProcID >= 0)
            {
                IntPtr hProcess = OpenProcess(0x1F0FFF, 1, ProcID);
                if (hProcess == null)
                {
                    Console.WriteLine("Failed getting process?");
                }
                else
                {
                    InjectDLL(hProcess, pDllPath);
                    return true;
                }
            }
            return false;
        }
    }
}