using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Text;

public class Memory
{
    #region Constants
    /// <summary>
    /// Pass to OpenProcess to gain all access to an external process.
    /// </summary>
    public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_DECOMMIT = 0x4000;
    public const uint PAGE_READWRITE = 0x04;
    public const uint PAGE_EXECUTE = 0x10;
    public const uint PAGE_EXECUTE_READWRITE = 0x40;
    #endregion

    #region DllImports
    //From pinvoke.net
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
    private static extern bool _CloseHandle(IntPtr hObject);
    /// <summary>
    /// Close a handle object (clean up your code).
    /// </summary>
    /// <param name="hObject">The object handle to be closed.</param>
    /// <returns>Returns true if success, false if not.</returns>
    public static bool CloseHandle(IntPtr hObject) { return _CloseHandle(hObject); }

    //From pinvoke.net
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    //From pinvoke.net
    [DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

    //From pinvoke.net
    [DllImport("kernel32.dll")]
    public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", EntryPoint = "VirtualAllocEx")]
    private static extern UIntPtr _VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
    public static UIntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect)
    {
        return _VirtualAllocEx(hProcess, lpAddress, dwSize, flAllocationType, flProtect);
    }
    public static uint AllocateMemory(IntPtr hProcess, long lpAddress, uint dwSize)
    {
        return (uint)VirtualAllocEx(hProcess, (IntPtr)lpAddress, dwSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
    }
    public static uint AllocateMemory(IntPtr hProcess, uint dwSize)
    {
        return AllocateMemory(hProcess, 0, dwSize);
    }

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle")]
    private static extern IntPtr _GetModuleHandle(string lpModuleName);
    public static IntPtr GetModuleHandle(string lpModuleName) { return _GetModuleHandle(lpModuleName); }

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
    private static extern UIntPtr _GetProcAddress(IntPtr hModule, string procName);
    public static uint GetProcAddress(IntPtr hModule, string procName) { return (uint)_GetProcAddress(hModule, procName); }

    [DllImport("kernel32.dll", EntryPoint = "CreateRemoteThread")]
    private static extern IntPtr _CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, UIntPtr lpStartAddress, UIntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
    public static IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, UIntPtr lpStartAddress, UIntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId)
    {
        return _CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, lpThreadId);
    }

    public static IntPtr CreateRemoteThread(IntPtr hProcess, long lpStartAddress, long lpParameter)
    {
        return CreateRemoteThread(hProcess, IntPtr.Zero, 0, (UIntPtr)lpStartAddress, (UIntPtr)lpParameter, 0, IntPtr.Zero);
    }

    public const int INFINITE = -1;
    public const int WAIT_OBJECT_0 = 0;
    [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static extern int _WaitForSingleObject(IntPtr hObject, int milliseconds);
    public static int WaitForSingleObject(IntPtr hObject, int milliseconds) { return _WaitForSingleObject(hObject, milliseconds); }
    public static int WaitForSingleObject(IntPtr hObject) { return _WaitForSingleObject(hObject, INFINITE); }

    [DllImport("kernel32.dll", EntryPoint = "VirtualFreeEx")]
    private static extern bool _VirtualFreeEx(IntPtr hProcess, UIntPtr lpAddress, uint dwSize, uint dwFreeType);
    public static bool VirtualFreeEx(IntPtr hProcess, long lpAddress, uint dwSize, uint dwFreeType)
    {
        return _VirtualFreeEx(hProcess, (UIntPtr)lpAddress, dwSize, dwFreeType);
    }

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    private static extern IntPtr _FindWindow(string classname, string windowtitle);
    public static IntPtr FindWindow(string classname, string windowtitle)
    {
        return _FindWindow(classname, windowtitle);
    }

    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    //public static bool IsWindowVisible(IntPtr hWnd) { return _IsWindowVisible(hWnd); }

    [DllImport("user32.dll", EntryPoint = "GetWindowText")]
    private static extern int _GetWindowText(IntPtr hWnd, StringBuilder buf, int nMaxCount);
    /// <summary>
    /// Returns the window title of the specified window
    /// </summary>
    /// <param name="hWnd">A handle to the window</param>
    /// <param name="length">Length of the string to be returned</param>
    /// <returns></returns>
    public static string GetWindowTitle(IntPtr hWnd, int length)
    {
        StringBuilder str = new StringBuilder(length);
        _GetWindowText(hWnd, str, length);
        return str.ToString();
    }
    #endregion

    private static string lasterror = "";
    /// <summary>
    /// Contains a string describing the last error to occur.
    /// </summary>
    public string LastError { get { return lasterror; } /*set { lasterror = value; }*/ }

    private static bool error = false;
    /// <summary>
    /// True if something in the Memory class has errored.  If true when checked, Error is reset to false.
    /// </summary>
    public bool Error
    {
        get
        {
            if (!error)
                return false;
            else
            {
                error = false;
                return false;
            }
        }
    }

    /// <summary>
    /// The process object that contains its information
    /// </summary>
    public class MLProc
    {
        private IntPtr hwnd;
        /// <summary>
        /// The process' window handle.
        /// </summary>
        public IntPtr hWnd { get { return hwnd; } /*set { hwnd = value; }*/ }

        private IntPtr phandle;
        /// <summary>
        /// The process' process handle.
        /// </summary>
        public IntPtr pHandle { get { return phandle; }/* set { phandle = value; }*/ }

        private int dwprocessid;
        /// <summary>
        /// The process' process ID.
        /// </summary>
        public int dwProcessId { get { return dwprocessid; } /*set { dwprocessid = value; }*/ }

        private bool isopen;
        /// <summary>
        /// Returns whether or not the process is open for read/write.
        /// </summary>
        public bool IsOpen { get { return isopen; } set { isopen = value; } }

        /// <summary>
        /// Opens the process for read/write and populate the window handle, process ID, and process handle.
        /// </summary>
        /// <param name="pid">The ID of the process to be opened.</param>
        public MLProc(int pid)
        {
            if (pid == 0)
            {
                lasterror = "dwProcessId == 0";
                error = true;
                return;
            }

            dwprocessid = pid;

            phandle = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            if ((uint)phandle == 0)
            {
                lasterror = "OpenProcess failed.";
                error = true;
                return;
            }

            WindowArray winenum = new WindowArray();

            foreach (IntPtr handle in winenum)
                if (GetProcessIdByHWnd(handle) == pid)
                {
                    hwnd = handle;
                    break;
                }

            if (hwnd == IntPtr.Zero)
            {
                lasterror = "Could not find window handle.";
                error = true;
                return;
            }

            isopen = true;

            winenum = null;
            GC.Collect();
        }

        /// <summary>
        /// Opens the process for read/write and populate the window handle, process ID, and process handle.
        /// </summary>
        /// <param name="hWnd">The window handle of the main window of the process to be opened.</param>
        public MLProc(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                lasterror = "hWnd == 0";
                error = true;
                return;
            }

            hwnd = hWnd;

            GetWindowThreadProcessId(hWnd, out dwprocessid);
            if (dwprocessid == 0)
            {
                lasterror = "dwProcessId == 0";
                error = true;
                return;
            }

            phandle = OpenProcess(PROCESS_ALL_ACCESS, false, dwprocessid);
            if ((uint)phandle == 0)
            {
                lasterror = "OpenProcess failed.";
                error = true;
                return;
            }

            isopen = true;
        }

        /// <summary>
        /// Converts the three process members into string format.
        /// </summary>
        /// <returns>A string containing the values of the three process members.</returns>
        public override string ToString() { return String.Format("hWnd: 0x{0:X08} | dwProcessId: {1} | pHandle: 0x{2:X08} | IsOpen: {3}", (uint)hwnd, dwprocessid, (uint)phandle, isopen.ToString()); }
    }

    public class MLProcesses
    {
        private ArrayList processes;

        /// <summary>
        /// The number of processes that have been opened.  Processes that are closed are not removed from the total of Count.
        /// </summary>
        public int Count { get { return processes.Count; } }

        /// <summary>
        /// Opens a process for read/write and adds it to the list of processes.
        /// </summary>
        /// <param name="dwProcessId">The ID of the process to be opened.</param>
        public int Open(int dwProcessId)
        {
            int count = processes.Count;
            MLProc proc = new MLProc(dwProcessId);
            if (error)
                return -1;

            processes.Add(proc);
            return count;
        }

        /// <summary>
        /// Opens a process for read/write and adds it to the list of processes.
        /// </summary>
        /// <param name="hWnd">The window handle of the main window of the process to be opened.</param>
        public int Open(IntPtr hWnd) { return Open(GetProcessIdByHWnd(hWnd)); }

        /// <summary>
        /// Opens a process for read/write and adds it to the list of processes.
        /// </summary>
        /// <param name="classname">The classname of the main window of the process to be opened.  Can be null.</param>
        /// <param name="windowTitle">The window title of the main window of the process to be opened.  Can be null.</param>
        public int Open(string classname, string windowTitle) { return Open(FindWindow(classname, windowTitle)); }

        public int[] Open(int[] dwProcessIds)
        {
            if (dwProcessIds == null || dwProcessIds.Length == 0)
            {
                lasterror = "Could not get Process Id.";
                error = true;
                return null;
            }

            int count = processes.Count;
            int[] clientindexes = new int[dwProcessIds.Length];

            for (int i = 0; i < clientindexes.Length; i++)
                clientindexes[i] = Open(dwProcessIds[i]);

            if (error)
                return null;
            else
                return clientindexes;
        }

        /// <summary>
        /// Closes the process handle associated with the process.
        /// </summary>
        /// <param name="index">The zero-based index of the process in MLProcesses to be closed.</param>
        public void Close(int index)
        {
            MLProc proc = (MLProc)processes[index];
            CloseHandle(proc.pHandle);
            proc.IsOpen = false;
        }

        /// <summary>
        /// Closes the process handle associated with the process.
        /// </summary>
        /// <param name="hWnd">The window handle of the main window of the process to be closed.</param>
        public void Close(IntPtr hWnd)
        {
            foreach (MLProc proc in processes)
                if (proc.hWnd == hWnd)
                {
                    CloseHandle(proc.pHandle);
                    proc.IsOpen = false;
                    break;
                }
        }

        /// <summary>
        /// Closes the process handle associated with the process.
        /// </summary>
        /// <param name="dwProcessId">The ID of the process to be closed.  (int)dwProcessId must be cast to (uint).</param>
        public void Close(uint dwProcessId)
        {
            foreach (MLProc proc in processes)
                if (proc.dwProcessId == dwProcessId)
                {
                    CloseHandle(proc.pHandle);
                    proc.IsOpen = false;
                    break;
                }
        }

        /// <summary>
        /// Process object MLProc.
        /// </summary>
        /// <param name="index">The index of the process in MLProcesses.</param>
        /// <returns>MLProc object associated with the given index.</returns>
        public MLProc this[int index]
        {
            get
            {
                if (processes.Count > index)
                    return (MLProc)processes[index];
                else
                    return null;
            }
        }

        /// <summary>
        /// Process object MLProc.
        /// </summary>
        /// <param name="hWnd">The window handle associated with main window of the process.</param>
        /// <returns>MLProc object associated with the given window handle.</returns>
        public MLProc this[IntPtr hWnd]
        {
            get
            {
                foreach (MLProc proc in processes)
                    if (proc.hWnd == hWnd)
                        return proc;

                return null;
            }
        }

        /// <summary>
        /// Process object MLProc.
        /// </summary>
        /// <param name="dwProcessId">The ID of the process.</param>
        /// <returns>MLProc object associated with the given process ID.</returns>
        public MLProc this[uint dwProcessId]
        {
            get
            {
                foreach (MLProc proc in processes)
                    if ((uint)proc.dwProcessId == dwProcessId)
                        return proc;

                return null;
            }
        }

        public MLProcesses() { processes = new ArrayList(); }
        public MLProcesses(int capacity) { processes = new ArrayList(capacity); }
        public MLProcesses(ICollection c) { processes = new ArrayList(c); }
    }

    private MLProcesses procs;
    public MLProcesses Processes { get { return procs; } }

    public Memory() { procs = new MLProcesses(); }
    public Memory(int capacity) { procs = new MLProcesses(capacity); }
    public Memory(ICollection c) { procs = new MLProcesses(c); }

    #region ReadMemory
    public bool ReadMemory(int index, long Address, ref byte[] buffer, int size)
    {
        return ReadMemory(procs[index].pHandle, Address, ref buffer, size);
    }

    public bool ReadMemory(int index, long Address, ref byte[] buffer)
    {
        return ReadMemory(procs[index].pHandle, Address, ref buffer);
    }

    public uint ReadUInt(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[4];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        if (reverse) Array.Reverse(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

    public uint ReadUInt(int index, long Address) { return ReadUInt(index, Address, false); }

    public int ReadInt(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[4];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        if (reverse) Array.Reverse(buffer);
        return BitConverter.ToInt32(buffer, 0);
    }

    public int ReadInt(int index, long Address) { return ReadInt(index, Address, false); }

    public ushort ReadUShort(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[2];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        if (reverse) Array.Reverse(buffer);
        return BitConverter.ToUInt16(buffer, 0);
    }

    public ushort ReadUShort(int index, long Address) { return ReadUShort(index, Address, false); }

    public short ReadShort(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[2];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        if (reverse) Array.Reverse(buffer);
        return BitConverter.ToInt16(buffer, 0);
    }

    public short ReadShort(int index, long Address) { return ReadShort(index, Address, false); }

    public byte ReadByte(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[1];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        return buffer[0];
    }

    public byte ReadByte(int index, long Address) { return ReadByte(index, Address, false); }

    public sbyte ReadSByte(int index, long Address, bool reverse)
    {
        byte[] buffer = new byte[1];
        if (!ReadMemory(index, Address, ref buffer))
        {
            lasterror = "ReadMemory failed!";
            error = true;
            return 0;
        }
        return (sbyte)buffer[0];
    }

    public sbyte ReadSByte(int index, long Address) { return ReadSByte(index, Address, false); }

    public string ReadString(int index, long Address, int length)
    {
        byte[] buffer = new byte[length];

        ReadMemory(index, Address, ref buffer);

        string ret = Encoding.UTF8.GetString(buffer);

        if (ret.IndexOf("\0") != -1)
            ret.Remove(ret.IndexOf("\0"));

        return ret;
    }
    #endregion

    /// <summary>
    /// Enumerate open windows
    /// </summary>
    public class WindowArray : ArrayList
    {
        private delegate bool EnumWindowsCB(IntPtr handle, IntPtr param);

        [DllImport("user32")]
        private static extern int EnumWindows(EnumWindowsCB cb,
            IntPtr param);

        private static bool MyEnumWindowsCB(IntPtr hwnd, IntPtr param)
        {
            GCHandle gch = (GCHandle)param;
            WindowArray itw = (WindowArray)gch.Target;
            itw.Add(hwnd);
            return true;
        }

        /// <summary>
        /// Returns an array of all open windows and their hWnds
        /// </summary>
        public WindowArray()
        {
            GCHandle gch = GCHandle.Alloc(this);
            EnumWindowsCB ewcb = new EnumWindowsCB(MyEnumWindowsCB);
            EnumWindows(ewcb, (IntPtr)gch);
            gch.Free();
        }
    }

    #region GetProcessWindowStuff
    public static IntPtr[] FindWindowsByTitle(string windowTitle)
    {
        ArrayList handles = new ArrayList();
        WindowArray winenum = new WindowArray();

        foreach (IntPtr handle in winenum)
            if (IsWindowVisible(handle) && GetWindowTitle(handle, 255).IndexOf(windowTitle) == 0)
                handles.Add(handle);

        if (handles.Count == 0)
        {
            lasterror = windowTitle + " window was not found.";
            error = true;
            return null;
        }

        IntPtr[] hwnds = new IntPtr[handles.Count];

        for (int i = 0; i < handles.Count; i++)
            hwnds[i] = (IntPtr)handles[i];

        return hwnds;
    }

    public static IntPtr FindWindowByTitle(string windowTitle)
    {
        IntPtr[] windows = FindWindowsByTitle(windowTitle);
        if (windows == null)
            return IntPtr.Zero;

        return windows[0];
    }

    public static IntPtr[] FindWindowsByClassName(string classname)
    {
        ArrayList handles = new ArrayList();
        WindowArray winenum = new WindowArray();
        StringBuilder tmp_classname;

        foreach (IntPtr handle in winenum)
            if (IsWindowVisible(handle))
            {
                tmp_classname = new StringBuilder(256);
                GetClassName(handle, tmp_classname, 256);
                if (tmp_classname.ToString().Equals(classname))
                    handles.Add(handle);
            }

        if (handles.Count == 0)
        {
            lasterror = classname + " window was not found.";
            error = true;
            return null;
        }

        IntPtr[] hwnds = new IntPtr[handles.Count];

        for (int i = 0; i < handles.Count; i++)
            hwnds[i] = (IntPtr)handles[i];

        return hwnds;
    }

    public static IntPtr FindWindowByClassName(string classname)
    {
        IntPtr[] windows = FindWindowsByClassName(classname);
        if (windows == null)
            return IntPtr.Zero;

        return windows[0];
    }

    public static IntPtr FindWindowByProcessId(uint dwProcessId)
    {
        if (dwProcessId == 0)
        {
            lasterror = "dwProcessId == 0";
            error = true;
            return IntPtr.Zero;
        }

        WindowArray winenum = new WindowArray();
        foreach (IntPtr handle in winenum)
            if (GetProcessIdByHWnd(handle) == dwProcessId)
                return handle;

        lasterror = "Could not find window.";
        error = true;
        return IntPtr.Zero;
    }

    public static IntPtr[] FindWindowsByProcessName(string processname)
    {
        int[] pids = GetProcessIdsByProcessName(processname);
        if (pids == null || pids.Length == 0)
            return null;

        WindowArray winenum = new WindowArray();
        IntPtr[] hwnds = new IntPtr[pids.Length];

        for (int i = 0; i < pids.Length; i++)
            foreach (IntPtr handle in winenum)
                if (GetProcessIdByHWnd(handle) == pids[i])
                    hwnds[i] = handle;

        return hwnds;
    }

    public static IntPtr FindWindowByProcessName(string processname)
    {
        IntPtr[] windows = FindWindowsByProcessName(processname);
        if (windows == null)
            return IntPtr.Zero;

        return windows[0];
    }

    public static int GetProcessIdByHWnd(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            lasterror = "hWnd == 0";
            error = true;
            return 0;
        }

        int dwProcessId;
        GetWindowThreadProcessId(hWnd, out dwProcessId);

        if (dwProcessId == 0)
        {
            lasterror = "dwProcessId == 0";
            error = true;
            return 0;
        }

        return dwProcessId;
    }

    public static int GetProcessIdByProcessName(string processname)
    {
        int[] procs = GetProcessIdsByProcessName(processname);
        if (procs == null)
            return 0;

        return procs[0];
    }

    public static int[] GetProcessIdsByProcessName(string procname)
    {
        string processname = procname;

        if (processname.IndexOf(".exe") != -1)
            processname = processname.Remove(processname.Length - 4);

        System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName(processname);
        if (procs.Length == 0)
        {
            lasterror = "Process " + processname + " could not be found.";
            error = true;
            return null;
        }

        int[] pids = new int[procs.Length];

        for (int i = 0; i < procs.Length; i++)
            pids[i] = procs[i].Id;

        return pids;
    }

    public static int GetProcessIdByWindowTitle(string windowtitle)
    {
        int[] procs = GetProcessIdsByWindowTitle(windowtitle);
        if (procs == null)
            return 0;

        return procs[0];
    }

    public static int[] GetProcessIdsByWindowTitle(string windowtitle)
    {
        IntPtr[] hwnds = FindWindowsByTitle(windowtitle);
        if (hwnds == null || hwnds.Length == 0)
            return null;

        int[] pids = new int[hwnds.Length];

        for (int i = 0; i < hwnds.Length; i++)
            GetWindowThreadProcessId((IntPtr)hwnds[i], out pids[i]);

        return pids;
    }

    public static int GetProcessIdByClassname(string classname)
    {
        int[] procs = GetProcessIdsByClassName(classname);
        if (procs == null)
            return 0;

        return procs[0];
    }

    public static int[] GetProcessIdsByClassName(string classname)
    {
        IntPtr[] hwnds = FindWindowsByClassName(classname);
        if (hwnds == null || hwnds.Length == 0)
            return null;

        int[] pids = new int[hwnds.Length];

        for (int i = 0; i < hwnds.Length; i++)
            GetWindowThreadProcessId((IntPtr)hwnds[i], out pids[i]);

        return pids;
    }

    public static string GetWindowTitleFromProcessId(uint dwProcessId)
    {
        WindowArray winenum = new WindowArray();

        foreach (IntPtr handle in winenum)
            if (GetProcessIdByHWnd(handle) == dwProcessId)
                return GetWindowTitle(handle, 256);

        lasterror = "Could not find window.";
        error = true;
        return null;
    }

    public static IntPtr OpenProcess(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            lasterror = "hWnd == 0";
            error = true;
            return IntPtr.Zero;
        }

        int dwProcessId = GetProcessIdByHWnd(hWnd);
        if (dwProcessId == 0)
        {
            lasterror = "Could not get ProcessId.";
            error = true;
            return IntPtr.Zero;
        }

        return OpenProcess(PROCESS_ALL_ACCESS, false, dwProcessId);
    }

    public static IntPtr[] OpenProcesses(IntPtr[] hWnds)
    {
        if (hWnds == null || hWnds.Length == 0)
        {
            lasterror = "hWnds == 0";
            error = true;
            return null;
        }

        //int[] dwProcessIds = new int[hWnds.Length];
        IntPtr[] pHandles = new IntPtr[hWnds.Length];

        for (int i = 0; i < hWnds.Length; i++)
        {
            if (hWnds[i] == IntPtr.Zero)
            {
                lasterror = String.Format("hWnds[{0}] == 0", i);
                error = true;
                return null;
            }

            pHandles[i] = OpenProcess(hWnds[i]);

            if (pHandles[i] == IntPtr.Zero)
                return null;
        }

        return pHandles;
    }

    public static IntPtr OpenProcess(int dwProcessId)
    {
        if (dwProcessId == 0)
        {
            lasterror = "Could not get ProcessId.";
            error = true;
            return IntPtr.Zero;
        }

        return OpenProcess(PROCESS_ALL_ACCESS, false, dwProcessId);
    }

    public static IntPtr[] OpenProcesses(int[] dwProcessIds)
    {
        if (dwProcessIds == null || dwProcessIds.Length == 0)
        {
            lasterror = "Could not get ProcessId.";
            error = true;
            return null;
        }

        for (int i = 0; i < dwProcessIds.Length; i++)
            if (dwProcessIds[i] == 0)
            {
                lasterror = "Could not get ProcessId.";
                error = true;
                return null;
            }

        IntPtr[] pHandles = new IntPtr[dwProcessIds.Length];

        for (int i = 0; i < pHandles.Length; i++)
            pHandles[i] = OpenProcess(dwProcessIds[i]);

        return pHandles;
    }
    #endregion

    #region ReadWriteInject
    public static bool ReadMemory(IntPtr pHandle, long Address, ref byte[] buffer, int size)
    {
        return ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)size, IntPtr.Zero);
    }

    public static bool ReadMemory(IntPtr pHandle, long Address, ref byte[] buffer)
    {
        return ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, string value)
    {
        byte[] buffer = ASCIIEncoding.UTF8.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, double value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, float value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, sbyte value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 1);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, byte value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 1);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, short value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 2);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, ushort value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 2);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, int value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 4);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, uint value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer, 4);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, long value)
    {
        byte[] buffer = BitConverter.GetBytes(value);
        return WriteMemory(pHandle, Address, buffer);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, byte[] buffer, int size)
    {
        return WriteProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)size, IntPtr.Zero);
    }

    public static bool WriteMemory(IntPtr pHandle, long Address, byte[] buffer)
    {
        return WriteProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
    }

    public static bool InjectDll(IntPtr pHandle, string dllname)
    {
        uint pLibModule = AllocateMemory(pHandle, 0x1000);

        WriteMemory(pHandle, pLibModule, dllname);

        uint lpLoadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        //IntPtr hThread = CreateRemoteThread(pHandle, IntPtr.Zero, 0, lpLoadLibrary, pLibModule, 0, IntPtr.Zero);
        IntPtr hThread = CreateRemoteThread(pHandle, lpLoadLibrary, pLibModule);
        WaitForSingleObject(hThread, INFINITE);
        CloseHandle(hThread);

        VirtualFreeEx(pHandle, pLibModule, 0x1000, MEM_DECOMMIT);

        return true;
    }

    public static bool InjectDllIntoWindow(IntPtr hWnd, string dllname)
    {
        IntPtr pHandle = OpenProcess(hWnd);

        uint pLibModule = AllocateMemory(pHandle, 0x1000);

        WriteMemory(pHandle, pLibModule, dllname);

        uint lpLoadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        IntPtr hThread = CreateRemoteThread(pHandle, lpLoadLibrary, pLibModule);
        WaitForSingleObject(hThread, INFINITE);

        CloseHandle(hThread);
        VirtualFreeEx(pHandle, pLibModule, 0x1000, MEM_DECOMMIT);
        CloseHandle(pHandle);

        return true;
    }

    public static bool InjectDllIntoProcess(int dwProcessId, string dllname)
    {
        IntPtr pHandle = OpenProcess(dwProcessId);

        uint pLibModule = AllocateMemory(pHandle, 0x1000);

        WriteMemory(pHandle, pLibModule, dllname);

        uint lpLoadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        //IntPtr hThread = CreateRemoteThread(pHandle, IntPtr.Zero, 0, lpLoadLibrary, pLibModule, 0, IntPtr.Zero);
        IntPtr hThread = CreateRemoteThread(pHandle, lpLoadLibrary, pLibModule);
        WaitForSingleObject(hThread, INFINITE);

        CloseHandle(hThread);
        VirtualFreeEx(pHandle, pLibModule, 0x1000, MEM_DECOMMIT);
        CloseHandle(pHandle);

        return true;
    }

    public bool InjectDll(int index, string dllname)
    {
        uint pLibModule = AllocateMemory(procs[index].pHandle, 0x1000);

        WriteMemory(procs[index].pHandle, pLibModule, dllname);

        uint lpLoadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        IntPtr hThread = CreateRemoteThread(procs[index].pHandle, lpLoadLibrary, pLibModule);
        WaitForSingleObject(hThread, INFINITE);

        CloseHandle(hThread);
        VirtualFreeEx(procs[index].pHandle, pLibModule, 0x1000, MEM_DECOMMIT);

        return true;
    }
    #endregion
}