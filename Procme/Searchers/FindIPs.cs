using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Procme.Searchers
{
    class FindIPs
    {
        static byte[] IPAoB_1 = Extension.ByteStringToArray(
            "68 AA AA 00 00 " + // Port
            "68 AA AA AA 00 " + // Addr of IP
            "B9 AA AA AA 00 " + // ?
            "E8 AA AA AA FF " // Add to list
            );
        static byte[] IPAoB_2 = Extension.ByteStringToArray(
             "56 " + // ESI = Port
            "68 AA AA AA 00 " + // Addr of IP
            "B9 AA AA AA 00 " + // ?
            "E8 AA AA AA FF " // Add to list
            );

        static byte[] IPAoB_3 = Extension.ByteStringToArray(
            "08 1f 62 AA"
            );

        static byte[] IPAoB_4 = Extension.ByteStringToArray(
            "21 24"
            );


        public static List<object[]> _ips = new List<object[]>();

        public static void LoadIPs(ProcessStream ps)
        {
            _ips.Clear();
                /*
            {
                long _start = 0x10000;
                while (true)
                {
                    long _derp = Extension.FindAoB(ps.pHandle, _start, IPAoB_3);
                    if (_derp == 0) break;
                    ps.Position = _derp - 10;
                    _start = _derp + IPAoB_3.Length;
                    DLOG.WriteLine("IP?");
                    DLOG.WriteLine(ps.ReadBytes(20));
                }
                _start = 0x10000;
                while (true)
                {
                    long _derp = Extension.FindAoB(ps.pHandle, _start, IPAoB_4);
                    if (_derp == 0) break;
                    ps.Position = _derp;
                    _start = _derp + IPAoB_4.Length;
                    DLOG.WriteLine("PORT?");
                    DLOG.WriteLine(ps.ReadBytes(10));
                }
            }
*/

            int mode = 0;
            ushort port = 0;
            long posCurrent = 0x0010EEE8;
            DLOG.WriteLine("SEEKING IPS");
            DLOG.WriteLine("[INFO] Mode 1");
            long addr = Extension.FindAoB(ps.pHandle, posCurrent, IPAoB_1);
            if (addr != 0)
            {
                mode = 1;
                goto LoadEm;
            }
            DLOG.WriteLine("[INFO] Mode 2");
            addr = Extension.FindAoB(ps.pHandle, posCurrent, IPAoB_2);
            if (addr != 0)
            {
                mode = 2;
                ps.Position = addr - 4;
                port = (ushort)ps.ReadInt();
                goto LoadEm;
            }
            return;
LoadEm:
            DLOG.WriteLine("Loading IPs");
            posCurrent = addr;
            for (int i = 0; ; i++)
            {
                string ip = "";

                addr = Extension.FindAoB(ps.pHandle, posCurrent, IPAoB_1);
                if (addr != 0)
                {
                    if (addr - posCurrent > IPAoB_1.Length + 10)
                    {
                        DLOG.WriteLine("Broke with len size");
                        return;
                    }

                    ps.Position = addr;

                    if (mode == 1)
                    {
                        ps.ReadByte(); // Push
                        ps.ReadInt(); // Port

                        ps.ReadByte(); // Push
                        ps.ReadInt(); // IP (string)

                        ps.ReadByte(); // Move ECX
                        long store_addr = ps.ReadInt(); // location

                        long tmp = ps.Position;

                        store_addr += 2; // First 2 aren't needed (is 2 lol)
                        ps.Position = store_addr;
                        port = ps.ReadUShort();
                        ip = new System.Net.IPAddress(ps.ReadBytes(4)).ToString();

                        _ips.Add(new object[] { store_addr, ip, port });

                        ps.Position = tmp;
                    }
                    else if (mode == 2)
                    {
                        ps.ReadByte(); // Push port

                        ps.ReadByte(); // Push
                        ps.ReadInt(); // IP

                        ps.ReadByte(); // Move ECX
                        long store_addr = ps.ReadInt(); // location

                        long tmp = ps.Position;

                        store_addr += 2; // First 2 aren't needed (is 2 lol)
                        ps.Position = store_addr;
                        port = ps.ReadUShort();
                        ip = new System.Net.IPAddress(ps.ReadBytes(4)).ToString();

                        _ips.Add(new object[] { store_addr, ip, port });

                        ps.Position = tmp;
                    }
                    DLOG.WriteLine("{0}:{1} added", ip, port);
                    posCurrent = ps.Position;
                }
                else
                {
                    break;
                }
            }

            DLOG.WriteLine("Found {0} ips!", _ips.Count);
        }
    }
}
