using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


using System.IO;

namespace Procme
{
    class Program
    {
        static List<object[]> _strings = new List<object[]>();
        /*
        static List<IntPtr> processes = new List<IntPtr>();

        static void Main(string[] args)
        {
            while (true)
            {
                IntPtr[] d = Memory.FindWindowsByProcessName("MapleStory.exe");
                if (d != null)
                {
                    foreach (var proc in d)
                    {
                        if (!processes.Contains(proc))
                        {
                            processes.Add(proc);
                            MapleBypass.Injector.RemoveMutex(proc.ToInt32());
                        }
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }
        */
        static void Main(string[] args)
        {
            Console.Title = "STREDIT - CONSOLE";
            Change ch;
            bool runAndSave = false;
            if (args.Length >= 1 && System.IO.File.Exists(args[0]))
            {
                runAndSave = args.Length >= 2;
                ch = Change.Load(args[0]);
            }
            else
            {
                ch = new Change();
            }



            IntPtr processID;
            while (true)
            {
                processID = Memory.FindWindowByTitle("MapleStory");
                if (processID == IntPtr.Zero) processID = Memory.FindWindowByProcessName("MapleStory.exe");
                if (processID == IntPtr.Zero) processID = Memory.FindWindowByClassName("MapleStoryClass");

                if (processID != IntPtr.Zero) break;
                DLOG.WriteLine("Couldn't find MapleStory client. Waiting 2 seconds");
                System.Threading.Thread.Sleep(2000);
            }

            DLOG.WriteLine("Found client!");

            ProcessStream ps = new ProcessStream(processID);
            IntPtr handle = ps.pHandle;
            /*
            {
                using (
                MemoryStream mem = new MemoryStream())
                {
                    BinaryWriter bw = new BinaryWriter(mem);
                    ps.Position = 0;
                    long MaxAddress = 0x7fffffff;
                    long address = 0;
                    long data = 0;
                    do
                    {
                        MEMORY_BASIC_INFORMATION m;
                        int result = Extension.VirtualQuery_(System.Diagnostics.Process.GetCurrentProcess().Handle, address, out m);
                        ps.Position = 0;
                        Console.WriteLine("{0}-{1} : {2} bytes {3}", m.BaseAddress, (uint)m.BaseAddress + (uint)m.RegionSize - 1, m.RegionSize,(AllocationProtect)m.AllocationProtect);

                        byte[] dijefoihew = ps.ReadBytes(10);

                        data += m.RegionSize.ToInt32();

                        if (address == (long)m.BaseAddress + (long)m.RegionSize)
                            break;
                        address = (long)m.BaseAddress + (long)m.RegionSize;
                    } while (address <= MaxAddress);
                    Console.WriteLine(data);
                    byte[] buff = new byte[bw.BaseStream.Length];
                    bw.BaseStream.Read(buff, 0, buff.Length);
                    File.WriteAllBytes("dump.exe", buff);
                }
            }
            */
            {
                byte[] key;
                int amount, keypos;
                int _strings_start = 0x7FFFFFFF, _strings_end = 0;
                Searchers.FindDecodeFunction.Find(ps, out key, out amount, out keypos);
                DLOG.WriteLine("Amount of keys: {0}, starting @ {1:X8}", amount, keypos);
                DLOG.WriteLine(key);
                DLOG.WriteLine();
                {
                    for (int i = 0; i < amount; i++)
                    {
                        bool isBstr;
                        int tmp1, tmp2;
                        string result = Extension.DecodeString(ps, key, keypos + (i * 4), out isBstr, out tmp1, out tmp2);
                        _strings.Add(new object[] { result, isBstr });
                        if (tmp1 < _strings_start) _strings_start = tmp1;
                        if (tmp2 > _strings_end) _strings_end = tmp2;
                    }
                    Console.Clear();
                    Searchers.FindIPs.LoadIPs(ps);
                    DLOG.WriteLine("STREDIT Console - Ready!");
                    DLOG.WriteLine("Loaded {0} strings.", _strings.Count);
                }

                DLOG.WriteLine();

                if (runAndSave)
                {
                    foreach (var kvp in ch.Changed)
                    {
                        _strings[kvp.Key][0] = kvp.Value;
                    }

                    int currentOffset = 0;
                    int i = 0;
                    foreach (object[] kv in _strings)
                    {
                        currentOffset += Extension.WriteString(ps, key, keypos + (i * 4), _strings_start + currentOffset, (string)kv[0], (bool)kv[1]);
                        i++;
                    }
                }
                else 
                {
                    while (true)
                    {
                        string cmd = Console.ReadLine();
                        string[] cmd_args = cmd.Split(' ');
                        if (cmd_args.Length >= 1)
                        {
                            switch (cmd_args[0])
                            {
                                case "edit":
                                case "write":
                                    {
                                        int id = int.Parse(cmd_args[1]);
                                        string val = string.Join(" ", cmd_args, 2, cmd_args.Length - 2);
                                        if (id < _strings.Count)
                                        {
                                            _strings[id][0] = val;
                                            if (!ch.Changed.ContainsKey(id)) ch.Changed.Add(id, val);
                                            ch.Changed[id] = val;
                                            DLOG.WriteLine("Set value for {0} to {1}", id, val);
                                        }
                                        break;
                                    }
                                case "read":
                                case "get":
                                    {
                                        int id = int.Parse(cmd_args[1]);
                                        if (id < _strings.Count)
                                        {
                                            DLOG.WriteLine("Value of {0}: {1}", id, _strings[id][0]);
                                        }
                                        break;
                                    }
                                case "save":
                                    {
                                        DLOG.WriteLine("Saving");
                                        int currentOffset = 0;
                                        int i = 0;
                                        foreach (object[] kv in _strings)
                                        {
                                            currentOffset += Extension.WriteString(ps, key, keypos + (i * 4), _strings_start + currentOffset, (string)kv[0], (bool)kv[1]);
                                            i++;
                                        }
                                        DLOG.WriteLine("Done");
                                        break;
                                    }
                                case "dump":
                                    {
                                        DLOG.WriteLine("Saving to dump.txt...");
                                        string dmp = "";
                                        int i = 0;
                                        foreach (object[] kv in _strings)
                                        {
                                            dmp += string.Format("{0,-6} - {1}\r\n", i, kv[0]);
                                            i++;
                                        }
                                        File.WriteAllText("dmp.txt", dmp);
                                        DLOG.WriteLine("Done");
                                        break;
                                    }
                                case "savexml":
                                    {
                                        DLOG.WriteLine("Saving to XML...");
                                        ch.Save("change.xml");
                                        DLOG.WriteLine("Done");
                                        break;
                                    }
                                case "editips":
                                    {
                                        string ip = cmd_args[1];
                                        System.Net.IPAddress o;
                                        if (System.Net.IPAddress.TryParse(ip, out o))
                                        {
                                            foreach (var k in Searchers.FindIPs._ips)
                                            {
                                                // new object[] { store_addr, ip, port }
                                                ps.Position = (long)k[0];

                                                DLOG.WriteLine(ps.ReadBytes(0x10));
                                                ps.Position = (long)k[0];

                                                byte[] port = BitConverter.GetBytes((ushort)k[2]);
                                                //Array.Reverse(port);
                                                ps.WriteMemory(port);
                                                ps.WriteMemory(o.GetAddressBytes()); // IP

                                                ps.Position = (long)k[0];
                                                DLOG.WriteLine(ps.ReadBytes(0x10));
                                            }
                                        }
                                        else
                                        {
                                            DLOG.WriteLine("Incorrect IP entered!");
                                        }
                                        break;
                                    }
                                case "loadips":
                                    {
                                        Searchers.FindIPs.LoadIPs(ps);
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

    }
}
