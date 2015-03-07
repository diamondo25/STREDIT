using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.IO;

namespace OpcodeFinder
{
    public partial class Form1 : Form
    {
        List<string> LastFiles = new List<string>();
        static uint FileOffset = 0x400000; // -1
        static uint FileAlignment = 0;
        string openedFile = "";


        public Form1()
        {
            InitializeComponent();

            BinaryReader br = new BinaryReader(new MemoryStream(new byte[] { 0x01, 0x71, 0x0C }));
            FollowCommands(br, (Action)delegate { });
        }

        System.Threading.Thread loadThread = null;


        void LoadLastFiles()
        {
            try
            {
                LastFiles.AddRange(File.ReadAllLines("lastfiles.txt"));
            }
            catch { }
            try
            {
                ReloadList();
            }
            catch { }
        }

        void SaveLastFiles()
        {
            File.WriteAllLines("lastfiles.txt", LastFiles.ToArray());
        }

        void AddNewFile(string pFilename)
        {
            LastFiles.Remove(pFilename);
            List<string> tmp = new List<string>(LastFiles);
            LastFiles.Clear();
            LastFiles.Add(pFilename);
            LastFiles.AddRange(tmp);

            SaveLastFiles();
            ReloadList();
        }

        void ReloadList()
        {
            this.Invoke((MethodInvoker)delegate
            {
                openToolStripMenuItem.DropDownItems.Clear();
                ToolStripMenuItem tsmi = new ToolStripMenuItem("Open new file...");
                tsmi.Click += openToolStripMenuItem_Click;
                openToolStripMenuItem.DropDownItems.Add(tsmi);

                ToolStripSeparator tss = new ToolStripSeparator();
                openToolStripMenuItem.DropDownItems.Add(tss);
                int i = 1;
                foreach (string file in LastFiles)
                {
                    if (!File.Exists(file)) continue;
                    tsmi = new ToolStripMenuItem(i++ + " - " + file);
                    tsmi.Click += (s, e) =>
                    {
                        if (loadThread != null) return;
                        string lol = s.ToString();
                        string tmp = lol.Remove(0, lol.IndexOf("- ") + 2);

                        new System.Threading.Thread(() => LoadFile(tmp)).Start();

                    };
                    openToolStripMenuItem.DropDownItems.Add(tsmi);
                }
            });
        }

        string tmpTitleBar = "";

        private void LoadFile(string pFilename)
        {
            try
            {

                DLOG.WriteLine("---------------Loading File------------------");
                DLOG.WriteLine("FileName: {0}", pFilename);
                DLOG.WriteLine("---------------------------------------------");
                GC.Collect();

                openedFile = pFilename;


                var SetTitleBar = (Action<string>)delegate(string pNewTitle)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        if (pNewTitle == "")
                        {
                            this.Text = tmpTitleBar;
                            tmpTitleBar = "";
                        }
                        else
                        {
                            if (tmpTitleBar == "") tmpTitleBar = this.Text;
                            this.Text = "Loading... : " + pNewTitle;
                        }
                    });
                };

                MemoryStream memstream = new MemoryStream(File.ReadAllBytes(pFilename));
                BinaryReader br = new BinaryReader(memstream);

                {
                    SetTitleBar("Loading File");
                    var filetype = Encoding.ASCII.GetString(br.ReadBytes(2));
                    DLOG.WriteLine("[DEBUG] File Type: {0}", filetype);
                    if (filetype != "MZ")
                    {
                        MessageBox.Show("Not an executable file?");
                        return;
                    }

                    br.BaseStream.Position = 0x3C; // e_lfanew
                    var NTHeadersStart = br.ReadInt32();
                    DLOG.WriteLine("NT headers start at {0:X8}", NTHeadersStart);
                    br.BaseStream.Position = NTHeadersStart;
                    br.BaseStream.Position += 0x34; // Image Base
                    FileOffset = br.ReadUInt32();
                    br.BaseStream.Position += 4; // Skip Section Alignment, get File Alignment
                    FileAlignment = br.ReadUInt32();

                    DLOG.WriteLine("[DEBUG] File has Alignment of {0:X8}", FileAlignment);
                    if (FileAlignment != 0x1000)
                    {
                        FileOffset += 0x1000 + (FileAlignment * 2);
                    }
                    DLOG.WriteLine("[DEBUG] Current File Offset {0:X8}", FileOffset);
                }

                {
                    SetTitleBar("Searching for Packet Handlers");
                    DataTable dt = new DataTable("FileStrings");
                    dt.Columns.Add("Address", typeof(string)).ReadOnly = true;
                    dt.Columns.Add("Handler Function Address", typeof(string)).ReadOnly = true;
                    dt.Columns.Add("Opcode Range", typeof(string)).ReadOnly = true;

                    var AddHandlerRow = (Action<int, int, ushort, ushort>)delegate(int pAddr, int pHandlerAddr, ushort pOpStart, ushort pOpEnd)
                    {
                        string val1 = (pAddr + FileOffset).ToString("X8"), val2 = (pHandlerAddr + FileOffset).ToString("X8"), val3;

                        if (pHandlerAddr + FileOffset == 0)
                        {
                            val2 = "Unknown";
                        }

                        if (pOpStart != pOpEnd)
                            val3 = string.Format("{0:X4} - {1:X4} ({0} - {1})", pOpStart, pOpEnd);
                        else
                            val3 = string.Format("{0:X4} ({0})", pOpStart);

                        dt.Rows.Add(val1, val2, val3);
                    };


                    {
                        byte[] FindCompareLowerHigher = ByteStringToArray(
                            "3D AA AA 00 00" +
                            "7C 1B" + // Lower Than
                            "3D AA AA 00 00" +
                            "7F 14" // Higher Than
                            );

                        uint curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindCompareLowerHigher, 0xAA, true))
                        {
                            br.ReadByte();
                            int min = br.ReadInt32();
                            br.ReadInt16();
                            br.ReadByte();
                            int max = br.ReadInt32();

                            //AddHandlerRow((int)br.BaseStream.Position, (ushort)min, (ushort)max);
                            curpoozzz = (uint)br.BaseStream.Position;
                        }
                    }


                    {
                        ushort currentOpcode = 0;
                        ushort result = 0;
                        var GetDerpAction = (Func<bool>)delegate
                        {
                            result = 0;
                            byte action = br.ReadByte();
                            if (action == 0x81) // substract (int?)
                            {
                                br.ReadByte();
                                ushort sub = (ushort)br.ReadInt32();
                                if (currentOpcode == 0)
                                {
                                    result = sub;
                                }
                                else
                                {
                                    result = (ushort)(currentOpcode + sub);
                                }
                                ushort action2 = br.ReadUInt16();
                                if (action2 == 0x840F) // Jump Equal Zero
                                {
                                    int offset = br.ReadInt32();
                                    AddHandlerRow((int)br.BaseStream.Position, offset + (int)br.BaseStream.Position, result, result);
                                    return true;
                                }
                            }
                            else if (action == 0x83) // substract (byte)
                            {
                                br.ReadByte();
                                ushort sub = (ushort)br.ReadByte();
                                if (currentOpcode == 0)
                                {
                                    result = sub;
                                }
                                else
                                {
                                    result = (ushort)(currentOpcode + sub);
                                }

                                ushort action2 = br.ReadUInt16();
                                if (action2 == 0x840F) // Jump Equal Zero
                                {
                                    int offset = br.ReadInt32();
                                    AddHandlerRow((int)br.BaseStream.Position, offset + (int)br.BaseStream.Position, result, result);
                                    return true;
                                }
                                else if (action2 % 0x100 == 0x0074) // Jump Equal Zero (byte jump)
                                {
                                    int offset = br.ReadSByte();
                                    AddHandlerRow((int)br.BaseStream.Position, offset + (int)br.BaseStream.Position, result, result);
                                    return true;
                                }
                            }
                            else if (action == 0x4A) // Decrease with 1
                            {
                                // O.o
                                if (currentOpcode == 0)
                                {
                                    result = 1;
                                }
                                else
                                {
                                    result = (ushort)(currentOpcode + 1);
                                }
                                ushort action2 = br.ReadUInt16();
                                if (action2 == 0x840F) // Jump Equal Zero
                                {
                                    int offset = br.ReadInt32();
                                    AddHandlerRow((int)br.BaseStream.Position, offset + (int)br.BaseStream.Position, result, result);
                                    return true;
                                }
                            }
                            else if (action == 0x48) // Decrease with 1 (byte?)
                            {
                                // O.o
                                if (currentOpcode == 0)
                                {
                                    result = 1;
                                }
                                else
                                {
                                    result = (ushort)(currentOpcode + 1);
                                }
                                while (br.ReadByte() == 0x48)
                                {
                                    result += 1;
                                }

                                br.BaseStream.Position -= 1;

                                int offset = CalculateAddressLocation(br);

                                AddHandlerRow((int)br.BaseStream.Position, offset - (int)FileOffset, result, result);
                                return true;
                            }
                            return false;
                        };

                        byte[] FindBigIF = ByteStringToArray(
                            "8B AA" +// Moving 
                            "81 EA AA AA 00 00" // Substract value from register (min)
                            );

                        uint curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindBigIF, 0xAA, true))
                        {
                            br.ReadInt16();
                            while (GetDerpAction())
                            {
                                DLOG.WriteLine("Found something @ {0:X8}: {1:X4}", br.BaseStream.Position + FileOffset, result);
                                currentOpcode = result;
                            }
                            curpoozzz = (uint)br.BaseStream.Position;
                        }

                        byte[] FindSmallIF = ByteStringToArray(
                            "8B AA" +// Moving 
                            "83 E8 AA" // Substract value from register (min) (small)
                            );

                        currentOpcode = 0;
                        curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindSmallIF, 0xAA, true))
                        {
                            br.ReadInt16();
                            while (GetDerpAction())
                            {
                                DLOG.WriteLine("Found something @ {0:X8}: {1:X4}", br.BaseStream.Position + FileOffset, result);
                                currentOpcode = result;
                            }
                            curpoozzz = (uint)br.BaseStream.Position;
                        }
                    }

                    {
                        /**
                         * 
                         * Switches en Jump Tables. Leuke dingen
                         * Wat er eerst gebeurt:
                         *  - Switch value - minimum ( LEA register, [value - minimum] ) (signed)
                         *  - Kijken of deze iig niet boven X is en lager dan 0 (CMP + JA)
                         *  - Zowel: ga naar address, aangegeven door waarde van JA
                         *  - Zoniet: ga verder en JUMP met Offset + (register * 4) (dus zal dus bij register = 3, 3 * 4 = 12, 12 bytes verder
                         *          dan de offset van de jumptable kijken; een offset van de switch case)
                         *          | Note 1: Jumptables hebben voor missende cases gewoon een case toegevoegt -> de default case
                         *          | Note 2: Het zal _nooit_ voorkomen dat er 1 mist. Anders crasht de app lol.
                         * 
                         * */

                        // Signed check
                        byte[] FindSwitch = ByteStringToArray(
                            "8D AA AA" + // LEA
                            "83 AA AA" + // Compare value with last byte
                            "0F 87 AA AA 00 00" + // Default Jump (jump if set), addr offset last 4 bytes
                            "FF 24 95 AA AA AA AA" // Jump Table Location
                            );

                        uint curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindSwitch, 0xAA, true))
                        {
                            br.ReadByte();
                            br.ReadByte();
                            byte minus = 0;
                            var tmpMinus = br.ReadSByte();
                            minus = (byte)Math.Abs(tmpMinus);

                            br.ReadByte();
                            br.ReadByte();
                            byte aantal = br.ReadByte();

                            br.ReadByte();
                            br.ReadByte();
                            int jumpOffset1 = br.ReadInt32() + (int)br.BaseStream.Position;

                            br.ReadInt16();
                            br.ReadByte();
                            int jumpTable = br.ReadInt32() - (int)FileOffset;

                            var tmp = br.BaseStream.Position;

                            for (int i = 0; i < aantal; i++)
                            {
                                ushort realcaseval = (ushort)(minus + i);
                                br.BaseStream.Position = jumpTable + (i * 4);
                                int realAddr = (int)(br.ReadInt32() - FileOffset);
                                if (realAddr != jumpOffset1)
                                {
                                    // Is a real case; not default
                                    br.BaseStream.Position = realAddr;
                                    int chkval = br.ReadInt32();
                                    if ((uint)chkval == 0x830C75FF) // Normal
                                    {
                                        br.ReadInt16();
                                    }
                                    else if ((uint)chkval == 0x8B0C75FF) // Special 1
                                    {
                                        AddHandlerRow(realAddr, (int)(0-FileOffset), realcaseval, realcaseval);
                                        DLOG.WriteLine("Special 1 {0:X2} {1:X8}", realcaseval, br.BaseStream.Position + FileOffset);
                                        continue;
                                    }
                                    else
                                    {
                                        DLOG.WriteLine("??? {0:X2} {1:X8}", realcaseval, br.BaseStream.Position + FileOffset);
                                        break;
                                    }
                                    int handlerAddr = CalculateAddressLocation(br);
                                    AddHandlerRow(realAddr, handlerAddr - (int)FileOffset, realcaseval, realcaseval);
                                }
                                else
                                {
                                    DLOG.WriteLine("Switch case {0:X2} is not used here! {1:X8}", realcaseval, br.BaseStream.Position + FileOffset);
                                }
                            }

                            DLOG.WriteLine("Found switch. Min: {0:X2} ({0}) Max: {1:X2} ({1}) MinMaxCheckFailAddr: {2:X8} Addr: {3:X8}", minus, minus + aantal, jumpOffset1 + FileOffset, br.BaseStream.Position + FileOffset);

                            curpoozzz = (uint)tmp;
                        }
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        dgvRecv.DataSource = dt;
                    });
                }

                AddNewFile(pFilename);
                loadThread = null;
            }
            catch (Exception ex)
            {
                DLOG.WriteLine("EXC: Exception written to exlog.txt!");
                MessageBox.Show(ex.ToString());
                File.AppendAllText("exlog.txt", "---- " + DateTime.Now + " --" + "\r\n");
                File.AppendAllText("exlog.txt", "--------------Exception--------------\r\n" + ex.ToString() + "\r\n\r\n\r\n\r\n");
            }
        }


        static byte[] ByteStringToArray(string input)
        {
            List<byte> ret = new List<byte>();
            input = input.Replace(" ", "");
            input = input.Trim();
            if (input.Length % 2 == 1) throw new Exception("Could not convert Byte String to Array. Size incorrect");
            for (int i = 0; i < input.Length; i += 2)
            {
                ret.Add(byte.Parse(input.Substring(i, 2), System.Globalization.NumberStyles.HexNumber));
            }

            DLOG.Write("[FindAOB] IDA AOB: ");
            foreach (byte b in ret)
            {
                if (b == 0xAA)
                {
                    DLOG.Write("? ");
                }
                else
                {
                    DLOG.Write("{0:X2} ", b);
                }
            }
            DLOG.WriteLine();
            return ret.ToArray();
        }


        static void DebugBuffer(BinaryReader pBR, long pPos = 0)
        {
            var tmp = pBR.BaseStream.Position;
            if (pPos != 0)
                pBR.BaseStream.Position = pPos;
            DLOG.WriteLine("[FDEBUG] ---------------------------------------------------------");
            DLOG.WriteLine("[FDEBUG] Current Position {0:X8} ({1:X8})", pBR.BaseStream.Position, pBR.BaseStream.Position + FileOffset);
            DLOG.Write("[FDEBUG] - Data: ");
            var data = pBR.ReadBytes(50);
            foreach (byte b in data) DLOG.Write("{0:X2} ", b);
            pBR.BaseStream.Position = tmp;
            DLOG.WriteLine();
            DLOG.WriteLine("[FDEBUG] ---------------------------------------------------------");
        }

        static void DebugBuffer(BinaryWriter pBR, long pPos = 0)
        {
            DebugBuffer(new BinaryReader(pBR.BaseStream), pPos);
        }

        static string GetString(BinaryReader pBR, bool unicode)
        {
            string ret = "";
            while (true)
            {
                char c = pBR.ReadChar();
                if (unicode)
                    pBR.ReadByte();
                if (c == 0) break;
                ret += c;
            }
            return ret;
        }

        static bool CheckIfNonWestern(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > sbyte.MaxValue) return true;
            }
            return false;
        }

        enum Instructions : byte
        {
            JumpZero = 0x74,
            JumpNotZero = 0x75,
            JumpIfAbove = 0x77,
            JumpGreaterOrEqual = 0x7D,
            JumpLessOrEqual = 0x7E,
            Jump = 0xEB,
            Call = 0xE8,
            JumpLong = 0xE9,
            JumpSpecial = 0x0F,
        };
        static int CalculateAddressLocation(BinaryReader br)
        {
            Instructions type = (Instructions)br.ReadByte();
            if (type == Instructions.Call ||
                type == Instructions.JumpLong) // Call
            {
                int pos = br.ReadInt32();
                return (int)(br.BaseStream.Position + pos + FileOffset);
            }
            else if (
                type == Instructions.Jump ||
                type == Instructions.JumpZero ||
                type == Instructions.JumpNotZero ||
                type == Instructions.JumpIfAbove ||
                type == Instructions.JumpLessOrEqual ||
                type == Instructions.JumpGreaterOrEqual)
            {
                byte pos = br.ReadByte();
                return (int)(br.BaseStream.Position + pos + FileOffset);
            }
            else if (
                type == Instructions.JumpSpecial)
            {
                byte t = br.ReadByte();
                if (t >= 0x80 && t <= 0x8F)
                {
                    int pos = br.ReadInt32();
                    return (int)(br.BaseStream.Position + pos + FileOffset);
                }
                else
                {
                    throw new Exception("Special jump not recognized?");
                }
            }
            else
            {
                throw new Exception("Could not find type of jump or call.");
            }
        }

        static void CreateAddressLocation(BinaryWriter bw, int foAddr, Instructions type)
        {
            bw.Write((byte)type);
            int tmp = (int)(foAddr - FileOffset);
            if (type == Instructions.Call ||
                type == Instructions.JumpLong) // Call
            {
                bw.Write((int)(tmp - bw.BaseStream.Position));
            }
            else if (
                type == Instructions.Jump ||
                type == Instructions.JumpZero ||
                type == Instructions.JumpNotZero ||
                type == Instructions.JumpIfAbove ||
                type == Instructions.JumpLessOrEqual ||
                type == Instructions.JumpGreaterOrEqual)
            {
                bw.Write((byte)(tmp - bw.BaseStream.Position));
            }
            else
            {
                throw new Exception("Could not find type of jump or call.");
            }
        }

        static bool FindAoBInFile(BinaryReader br, uint StartPos, byte[] AoB, byte skipbyte = 0xFF, bool skipbyteset = false, int length = 0)
        {
            br.BaseStream.Position = StartPos;
            try
            {
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    byte[] tmpBuffer = br.ReadBytes(length == 0 ? (int)Math.Min(1024 * 10, br.BaseStream.Length - br.BaseStream.Position) : length);
                    int tmpint = 0;
                    int res = Array.FindIndex(tmpBuffer, 0, tmpBuffer.Length, (byte b) =>
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
                        long realpos = (br.BaseStream.Position - tmpBuffer.Length) + (res - tmpint) + 1;
                        br.BaseStream.Position = realpos;
                        DLOG.WriteLine("[FindAOB] Found @ {0:X8}", realpos + FileOffset);
                        return true;
                    }
                    if (length != 0) return false;
                }
            }
            catch { }
            DLOG.WriteLine("[FindAOB] ------- EOF ---------");
            br.BaseStream.Position = StartPos;
            return false;
        }

        static uint PlainOffsetToFileOffset(uint off)
        {
            return off - FileOffset;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "EXE files|*.exe";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                openedFile = ofd.FileName;
                new System.Threading.Thread(() => LoadFile(ofd.FileName)).Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadLastFiles();
        }

        bool FlagIsset(byte pVal, byte pFlag)
        {
            return ((pVal & pFlag) == 1);
        }

        string GetSourceDestinationType(byte pRMByte, bool p8Bit)
        {
            byte c = pRMByte;
            if (p8Bit)
            {
                switch (c)
                {
                    case 0x00: return "AL";
                    case 0x01: return "CL";
                    case 0x02: return "DL";
                    case 0x03: return "BL";
                    case 0x04: return "AH";
                    case 0x05: return "CH";
                    case 0x06: return "DH";
                    case 0x07: return "BH";
                    default: throw new Exception("WHAT DE FCUUUUUUUUUK (8 bit)");
                }
            }
            else
            {
                switch (c)
                {
                    case 0x00: return "EAX";
                    case 0x01: return "ECX";
                    case 0x02: return "EDX";
                    case 0x03: return "EBX";
                    case 0x04: return "ESP";
                    case 0x05: return "EBP";
                    case 0x06: return "ESI";
                    case 0x07: return "EDI";
                    default: throw new Exception("WHAT DE FCUUUUUUUUUK (32 bit)");
                }
            }
        }

        class BitSet
        {
            public byte OpCode { get; set; }
            public bool BitSet1 { get { return (OpCode & 0x01) != 0; } }
            public bool BitSet2 { get { return (OpCode & 0x02) != 0; } }
            public bool BitSet3 { get { return (OpCode & 0x04) != 0; } }
            public bool BitSet4 { get { return (OpCode & 0x08) != 0; } }
            public bool BitSet5 { get { return (OpCode & 0x10) != 0; } }
            public bool BitSet6 { get { return (OpCode & 0x20) != 0; } }
            public bool BitSet7 { get { return (OpCode & 0x40) != 0; } }
            public bool BitSet8 { get { return (OpCode & 0x80) != 0; } }

            public string ToString(byte start = 7, byte end = 0)
            {
                string ret = "";
                for (int i = start; i >= end; i--)
                {
                    ret += ((OpCode >> i) & 1) == 0 ? "0" : "1";
                    ret += " ";
                }
                return ret.Trim();
            }
        }

        void FollowCommands(BinaryReader pBR, Delegate pActionAfterCommand)
        {
            for (int i = 0; ; i++)
            {
                if (pBR.BaseStream.Position == pBR.BaseStream.Length) break;
                // 8 bits instruction destination thing
                // [X X]      [X X X]      [X X X]
                //  MOD         REG          RM

                byte opcode = pBR.ReadByte();

                BitSet bs = new BitSet { OpCode = opcode };

                bool isExpansionOpcode = opcode == 0x0F;

                byte realOpcode = opcode;
                byte addOpcode = (byte)(realOpcode >> 3);

                if (!isExpansionOpcode)
                {
                    if (addOpcode == 0)
                    {
                        byte derp = pBR.ReadByte();
                        byte from, to;
                        bool destinationOffsetted = true;
                        if (!bs.BitSet2) // mem -> reg
                        {
                            from = (byte)(derp & 0x07);
                            to = (byte)((derp >> 3) & 0x07);
                        }
                        else
                        {
                            from = (byte)((derp >> 3) & 0x07);
                            to = (byte)(derp & 0x07);
                            destinationOffsetted = false;
                        }

                        string destName = GetSourceDestinationType(to, !bs.BitSet1);
                        string sourceName = GetSourceDestinationType(from, !bs.BitSet1);

                        bs.OpCode = derp;

                        if (!bs.BitSet8 && !bs.BitSet7) // Register Indirect
                        {
                        }
                        else if (!bs.BitSet8 && bs.BitSet7) // One Byte displacement
                        {
                            sbyte offset = pBR.ReadSByte();
                            string addmelol = offset < 0 ? "+" : "-";
                            addmelol += Math.Abs(offset).ToString();

                            if (destinationOffsetted) destName = string.Format("[{0}{1}]", destName, addmelol);
                            else sourceName = string.Format("[{0}{1}]", sourceName, addmelol);
                        }
                        else if (bs.BitSet8 && !bs.BitSet7) // 4 Byte Displacement
                        {
                            int offset = pBR.ReadInt32();
                            string addmelol = offset < 0 ? "+" : "-";
                            addmelol += Math.Abs(offset).ToString();

                            if (destinationOffsetted) destName = string.Format("[{0}{1}]", destName, addmelol);
                            else sourceName = string.Format("[{0}{1}]", sourceName, addmelol);
                        }
                        else if (bs.BitSet8 && bs.BitSet7) // Register Addressing Mode
                        {

                        }
                        DLOG.WriteLine("ADD {0}, {1}", destName, sourceName);

                    }
                    switch (realOpcode)
                    {
                        default:
                            {
                                DLOG.WriteLine("Unknown opcode: {0:X2}", realOpcode);
                                break;
                            }
                    }
                }
                else
                {
                    realOpcode = pBR.ReadByte();
                    switch (realOpcode)
                    {
                        default:
                            {
                                DLOG.WriteLine("Unknown opcode: (expansion) {0:X2}", realOpcode);
                                break;
                            }
                    }
                }
            }
        }
    }
}
