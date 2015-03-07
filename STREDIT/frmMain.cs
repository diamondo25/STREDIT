using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.IO;

namespace STREDIT
{
    public partial class frmMain : Form
    {

        static uint StringsPos = 0xA6F17C;
        static uint KeyPos = 0x9E30C4;
        static uint KeySizePos = 0x9E30D4;
        static uint StringsAmountPos = 0x9E30D8;
        static uint FileOffset = 0x400000; // -1
        static byte[] DecodeKey;
        static uint _StringAmount = 0;
        static int _DecodeKeySize = 0;
        static uint FileAlignment = 0;

        int _GetString = 0;
        int _GetStringW = 0;
        int _GetBSTRT = 0;
        int _StringPoolInstance = 0;

        static Encoding _currentEncoding = null;

        List<string> LastFiles = new List<string>();

        enum StringDecodeTypes
        {
            Unknown,
            GetString,
            GetStringW,
            GetBSTR
        }

        class sBlockSize
        {
            public uint Min { get; set; }
            public uint Max { get; set; }
            public uint Size { get { return Max - Min; } }
            public sBlockSize() { Min = uint.MaxValue; Max = uint.MinValue; }
            public sBlockSize(uint pMin, uint pMax) { Min = pMin; Max = pMax ; }
        }

        static sBlockSize BlockStringPool = new sBlockSize();
        static sBlockSize BlockStrings = new sBlockSize();

        Dictionary<int, Dictionary<int, StringDecodeTypes>> StringReferences = new Dictionary<int, Dictionary<int, StringDecodeTypes>>();

        int GlobalPortPos = 0;


        string STR_DataSize = "{0:N0} Bytes";
        string STR_BytesAvailable = "{0:N0} Bytes";
        string STR_Title = "STREDIT - CraftNet";

        int ____ip_declaration_pos = 0;
        int ____ip_max_len = 0;

        List<KeyValuePair<uint, uint>> NopThis;


        static void UpdateBlock(uint pAddr, bool pPool)
        {
            if (pPool)
            {
                if (BlockStringPool.Min > pAddr) BlockStringPool.Min = pAddr;
                if (BlockStringPool.Max < pAddr) BlockStringPool.Max = pAddr;
            }
            else
            {
                if (BlockStrings.Min > pAddr)
                {
                    if (BlockStrings.Min >= 0xFFFFF00)
                    {
                        BlockStrings.Min = pAddr;
                    }
                    else if (BlockStrings.Min - pAddr <= 0x1000)
                    {
                        BlockStrings.Min = pAddr;
                    }
                    else
                    {
                        DLOG.WriteLine("[LOLTEST] Fail: {0:X8} ({1:X8})", pAddr, BlockStrings.Min - pAddr);
                    }

                }
                if (BlockStrings.Max < pAddr) BlockStrings.Max = pAddr;
            }
        }

        System.Threading.Thread loadThread = null;

        string EditingRowValue = "";

        public frmMain()
        {
            InitializeComponent();
            this.Text = STR_Title;

            this.tsBytesAvailable.Text = string.Format(STR_BytesAvailable, 0);
            this.tsBlockSize.Text = string.Format(STR_DataSize, 0);

            // Remove version
            tsClientVersion.Visible = versionLabel.Visible = seperator1.Visible = false;
            famSep.Visible = famLabel.Visible = false;

            saveToolStripMenuItem1.Enabled = false;
            cSVToolStripMenuItem1.Enabled = false;
        }

        void LoadLastFiles()
        {
            try
            {
                LastFiles.AddRange(File.ReadAllLines(Program.DATAFOLDER + "lastfiles.txt"));
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
            File.WriteAllLines(Program.DATAFOLDER + "lastfiles.txt", LastFiles.ToArray());
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
                tsmi.Click += openToolStripButton_Click;
                openToolStripMenuItem.DropDownItems.Add(tsmi);

                ToolStripSeparator tss = new ToolStripSeparator();
                openToolStripMenuItem.DropDownItems.Add(tss);
                int i = 1;
                foreach (string file in LastFiles)
                {
                    if (!File.Exists(file)) continue;
                    tsmi = new ToolStripMenuItem(i++ + " - " + file);
                    tsmi.Click += (s, e) => {
                        if (loadThread != null) return;
                        string lol = s.ToString();
                        string tmp = lol.Remove(0, lol.IndexOf("- ") + 2);

                        new System.Threading.Thread(() => LoadFile(tmp)).Start(); 
                    
                    };
                    openToolStripMenuItem.DropDownItems.Add(tsmi);
                }
            });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DLOG.WriteLine("Starting Debug and stuff. lol!");
            LoadLastFiles();
            tmp_lblPort.Visible = tmp_udPort.Visible = false;
        }

        private void AddPoint()
        {
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Value++;
            });
        }

        private void LoadFile(string pFilename)
        {
            NopThis = new List<KeyValuePair<uint, uint>>();
            try
            {
                StringReferences = new Dictionary<int, Dictionary<int, StringDecodeTypes>>();

                this.Invoke((MethodInvoker)delegate
                {
                    famSep.Visible = famLabel.Visible = false;

                    dgvStrings.DataSource = null;
                    bytesfree = 0;
                    tsBytesAvailable.Text = string.Format(STR_BytesAvailable, 0);
                    tsBlockSize.Text = string.Format(STR_DataSize, 0, 0);
                    tsLoadProgress.Maximum = 18;
                    tsLoadProgress.Value = 0;
                    saveToolStripMenuItem1.Enabled = false;
                    cSVToolStripMenuItem1.Enabled = false;
                    lvIPs.Items.Clear();
                });
                GlobalPortPos = 0;

                DLOG.WriteLine("---------------Loading File------------------");
                DLOG.WriteLine("FileName: {0}", pFilename);
                DLOG.WriteLine("---------------------------------------------");
                GC.Collect();

                openedFile = pFilename;

                BlockStringPool = new sBlockSize();
                BlockStrings = new sBlockSize();


                MemoryStream memstream = new MemoryStream(File.ReadAllBytes(pFilename));
                BinaryReader br = new BinaryReader(memstream);

                #region Seek File Header Info
                {
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
                        //FileOffset += 0x1000 + (FileAlignment * 2);
                        FileOffset += 0x800 + (FileAlignment * 2);
                    }

                    if (MessageBox.Show("Is this GMS V.118+ perhaps?", "-.-", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        FileOffset -= 0x800;
                        FileOffset += 0x1000;
                    }
                    DLOG.WriteLine("[DEBUG] Current File Offset {0:X8}", FileOffset);
                }

                #endregion
                AddPoint();
                #region Find Shit Algo

                #region Seek Decode Base Stuff

                byte[] ExactDataAoB = ByteStringToArray(
                    "D6 DE 75 86 46 64 A3 71 E8 E6 7B D3 33 30 E7 2E" + // Decode Key
                    "10 00 00 00" + // Keylength
                    "AA AA AA 00"); // Amount of strings inside client

                DLOG.WriteLine("[TEST] Searching for Key, Key Length and Amount of Strings without any file knowledge.");

                int keypos_fp = -1;

                if (FindAoBInFile(br, 0x001DEEE8, ExactDataAoB, 0xAA, true))
                {
                    DLOG.WriteLine("[INFO] Gotcha! Found @ {0:X8} (FP: {1:X8})", br.BaseStream.Position + FileOffset, br.BaseStream.Position);
                    // DebugBuffer(br);
                    keypos_fp = (int)br.BaseStream.Position;
                }
                else
                {
                    DLOG.WriteLine("[ERROR] Failed!");
                }

                #endregion
                AddPoint();

                byte[] DecodeFunctionCall = ByteStringToArray(
                     "8B 86 AA AA AA AA " +
                     "0F BE 00 " +
                     "6A 04 " +
                     "B9 AA AA AA 00" + // Addr of stringpos
                     "89 45 F0 " +
                     "E8 AA AA AA AA " +
                     "8B C8 " +
                     "89 4D 0C " +
                     "85 C9 " +
                     "C6 45 FC 01 " +
                     "74 1A " +
                     "8B 86 AA AA AA 00 " + // Addr of stringpos
                     "83 21 00 " +
                     "40 " +
                     "6A FF " +
                     "50 " +
                     "E8 AA AA AA AA " +
                     "AA 45 0C " +
                     "89 45 0C " +
                     "EB 04 " +
                     "83 65 0C 00 " +
                     "FF 75 F0 " +
                     "80 65 FC 00 " +
                     "FF 35 AA AA AA 00" + // Addr of keysize (does not exist in V.90+, default of 0x10 is used in push!)
                     "68 AA AA AA 00 " +
                     "FF 75 0C " +
                     "E8 AA AA 00 00"); // Addr of key

                byte[] MakeVersionString = ByteStringToArray(
                    "68 AA AA 00 00" + // Push string
                    "50" + // Push EAX
                    "E8 AA AA AA 00 " + // CALL GetInstance 
                    "8B C8" + // Mov ECX, EAX
                    "E8 AA AA AA AA" + // Call GetString
                    "8B 00 " + // Mov EAX, [EAX]
                    "6A AA"); // Push Version

                bool gotkeysize = false;

                DLOG.WriteLine("[DEBUG] ---------------Trying Method 1-------------");
                if (!FindAoBInFile(br, 0x006074F3 - FileOffset, DecodeFunctionCall, 0xAA, true))
                {
                    DLOG.WriteLine("[DEBUG] ---------------Trying Method 2-------------");


                    byte[] k = ByteStringToArray(
                        "C6 44 24 28 01" +
                        "85 F6" +
                        "74 AA" +
                        "8B 0C AD AA AA AA AA" +
                        "83 C1 01"); // Addr of key
                    if (!FindAoBInFile(br, 0x007074F3 - FileOffset, k, 0xAA, true))
                    {
                        DLOG.WriteLine("[DEBUG] ---------------Trying Method 3-------------");

                        k = ByteStringToArray(
                            "33 F6" + // XOR ESI ESI
                            "8B AA 24 30" +
                            "AA" +
                            "6A 10" +
                            "68 AA AA AA AA" +
                            "56"); // Addr of key
                        if (!FindAoBInFile(br, 0x00700000 - FileOffset, k, 0xAA, true))
                        {
                            DLOG.WriteLine("[DEBUG] ---------------Trying Method 4-------------");
                            k = ByteStringToArray(
                                "FF75 AA" + // PUSH DWORD PTR SS:[EBP-?], 0
                                "C645 AA AA" + // MOV BYTE PTR SS:[EBP-?], 0
                                "6A 10" +
                                "68 AA AA AA AA" +
                                "FF75 AA"); // Addr of key
                            if (!FindAoBInFile(br, 0x00700000 - FileOffset, k, 0xAA, true))
                            {
                                DLOG.WriteLine("[DEBUG] ---------------Trying Method 5-------------");
                                k = ByteStringToArray(
                                    "8B 86 " + LOCATION_FLAG_STRING + "  AA AA AA AA" + // Push strings
                                    "83 21 00" +
                                    "40" +
                                    "6A FF" +
                                    "50" +
                                    "E8 CC A0 E3 FF" +
                                    "8B 45 AA" +
                                    "89 45 AA" +
                                    "EB 04" +
                                    "83 65 AA AA" +
                                    "FF 75 F0" +
                                    "C6 45 AA AA" +
                                    "6A " + LOCATION_FLAG_STRING + "  10" + // Push key size
                                    "68 " + LOCATION_FLAG_STRING + "  AA AA AA AA" + // Push key
                                    "FF 75 0C " +
                                    "E8   AA AA AA AA" // Call decoder
                                    ); 
                                if (!FindAoBInFile(br, 0x00500000 - FileOffset, k, 0xAA, true))
                                {
                                    DLOG.WriteLine("[ERROR] >>>>>>>>>>> Could not find Decode Function Call");
                                    DLOG.WriteLine("[ERROR] No other AoB's available at the moment");
                                    MessageBox.Show("Could not find Decode Function Calls... This version is not parsable :(");
                                    return;
                                }
                                else
                                {
                                    DLOG.WriteLine("[INFO] -   >>> METHOD 5 SUCCEEDED <<<");
                                    DLOG.WriteLine("[DEBUG] StringPool::GetString is around {0:X8}", br.BaseStream.Position + FileOffset);

                                    br.BaseStream.Position = locationFlags[2];
                                    KeyPos = br.ReadUInt32();

                                    br.BaseStream.Position = locationFlags[1];
                                    byte ks = br.ReadByte();
                                    _DecodeKeySize = ks;


                                    KeySizePos = KeyPos + ks;
                                    StringsAmountPos = KeySizePos + 4;


                                    br.BaseStream.Position = locationFlags[0];
                                    StringsPos = br.ReadUInt32();
                                    gotkeysize = true;

                                    if (keypos_fp != -1)
                                    {
                                        int diff = (int)KeyPos - keypos_fp;
                                        DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                                        if (diff != FileOffset)
                                        {
                                            DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                                            FileOffset = (uint)diff;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                DLOG.WriteLine("[INFO] -   >>> METHOD 4 SUCCEEDED <<<");
                                DLOG.WriteLine("[DEBUG] StringPool::GetString is around {0:X8}", br.BaseStream.Position + FileOffset);
                                var tmpppp = br.BaseStream.Position;

                                br.BaseStream.Position += 8;
                                byte ks = br.ReadByte();
                                _DecodeKeySize = ks;
                                br.BaseStream.Position++;

                                KeyPos = br.ReadUInt32();

                                if (keypos_fp != -1)
                                {
                                    int diff = (int)KeyPos - keypos_fp;
                                    DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                                    if (diff != FileOffset)
                                    {
                                        DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                                        FileOffset = (uint)diff;
                                    }
                                }

                                KeySizePos = KeyPos + ks;
                                StringsAmountPos = KeySizePos + 4;


                                if (!FindAoBInFile(br, (uint)tmpppp - 0x300, ByteStringToArray(
                                    "74 1A " +
                                    "8B 86 AA AA AA AA " + // Addr of stringpos
                                    "83 21 00 " +
                                    "40 "), 0xAA, true))
                                {
                                    DLOG.WriteLine("[INFO] Failed seeking the location of strings reference");
                                    MessageBox.Show("For some reason, MapleStory doesn't like me and I couldn't find the strings location.. :(");
                                    return;
                                }

                                DebugBuffer(br);

                                br.BaseStream.Position += 4;
                                StringsPos = br.ReadUInt32();
                                if (FileAlignment != 0x1000)
                                {
                                    //StringsPos -= 0xC00; // FU
                                }
                                gotkeysize = true;
                            }
                        }
                        else
                        {
                            DLOG.WriteLine("[INFO] -   >>> METHOD 3 SUCCEEDED <<<");
                            DLOG.WriteLine("[DEBUG] StringPool::GetString is around {0:X8}", br.BaseStream.Position + FileOffset);
                            var tmpppp = br.BaseStream.Position - 0x1A;

                            br.BaseStream.Position += 8;
                            byte ks = br.ReadByte();
                            _DecodeKeySize = ks;
                            br.BaseStream.Position++;
                            KeyPos = br.ReadUInt32();

                            if (keypos_fp != -1)
                            {
                                int diff = (int)KeyPos - keypos_fp;
                                DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                                if (diff != FileOffset)
                                {
                                    DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                                    FileOffset = (uint)diff;
                                }
                            }

                            KeySizePos = KeyPos + ks;
                            StringsAmountPos = KeySizePos + 4;


                            br.BaseStream.Position = tmpppp + 3;
                            StringsPos = br.ReadUInt32();
                            gotkeysize = true;
                        }
                    }
                    else
                    {
                        DLOG.WriteLine("[INFO] -   >>> METHOD 2 SUCCEEDED <<<");
                        DLOG.WriteLine("[DEBUG] StringPool::GetString is around {0:X8}", br.BaseStream.Position + FileOffset);
                        br.BaseStream.Position += 5 + 2 + 2 + 3;
                        StringsPos = br.ReadUInt32();

                        br.BaseStream.Position += 0x44 + 2;
                        byte ks = br.ReadByte();

                        if (ks != 0x10) // GMS V.115+
                        {
                            br.BaseStream.Position += 6; // 0.0

                            ks = br.ReadByte();
                            _DecodeKeySize = ks;
                            br.BaseStream.Position++;

                            KeyPos = br.ReadUInt32();

                            if (FileAlignment != 0x1000)
                            {
                                FileOffset -= 0x1000 + (FileAlignment * 2); // FU
                            }

                            if (keypos_fp != -1)
                            {
                                int diff = (int)KeyPos - keypos_fp;
                                DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                                if (diff != FileOffset)
                                {
                                    DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                                    FileOffset = (uint)diff;
                                }
                            }

                            br.BaseStream.Position = KeyPos - FileOffset;
                            KeySizePos = KeyPos + ks;
                            StringsAmountPos = KeySizePos + 4;
                        }
                        else
                        {
                            _DecodeKeySize = ks;
                            br.BaseStream.Position++;
                            KeyPos = br.ReadUInt32();

                            if (FileAlignment != 0x1000)
                            {
                                FileOffset -= 0x1000 + (FileAlignment * 2); // FU
                            }

                            if (keypos_fp != -1)
                            {
                                int diff = (int)KeyPos - keypos_fp;
                                DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                                if (diff != FileOffset)
                                {
                                    DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                                    FileOffset = (uint)diff;
                                }
                            }

                            KeySizePos = KeyPos + ks;
                            StringsAmountPos = KeySizePos + 4;
                        }
                    }
                }
                else
                {
                    DLOG.WriteLine("[INFO] -   >>> METHOD 1 SUCCEEDED <<<");
                    DLOG.WriteLine("[DEBUG] StringPool::GetString is around {0:X8}", br.BaseStream.Position + FileOffset);
                    br.BaseStream.Position += 2;
                    StringsPos = br.ReadUInt32();

                    br.BaseStream.Position += 0x44 + 2;
                    KeySizePos = br.ReadUInt32();
                    br.BaseStream.Position++;
                    KeyPos = br.ReadUInt32();
                    StringsAmountPos = KeySizePos + 4;

                    if (keypos_fp != -1)
                    {
                        int diff = (int)KeyPos - keypos_fp;
                        DLOG.WriteLine("[FileOffset] Diff: {0:X8} ({1:X8} - {2:X8})", diff, KeyPos, keypos_fp);
                        if (diff != FileOffset)
                        {
                            DLOG.WriteLine("[FileOffset] NOTICED DIFFERENT FILEOFFSET. CHANGING.");
                            FileOffset = (uint)diff;
                        }
                    }

                }

                #endregion
                AddPoint();

                #region StringDecoders

                {

                    {
                        DLOG.WriteLine("Searching string decoders, AoB 1");
                        byte[] FindGetString = ByteStringToArray(
                            "55 " + // Push EBP
                            "8B EC " + // Mov EBP ESP
                            "51 " + // Push ECX
                            "83 65 FC 00  " + // And var4 0
                            "6A 00 " + // Push 0
                            "FF 75 0C " + // Push Arg4
                            "FF 75 08" + // Push Arg0
                            "E8 AA AA AA AA " + // Call GetString or GetStringW!! (only these 2)
                            "8B 45 08  " + // Move eax Arg0 
                            "C9 " + // Leave
                            "C2 08 00" // Retn 8
                            );

                        byte[] FindIsGetStringW = ByteStringToArray(
                            "75 68 " + // jnz
                            "8B 86 AA AA AA AA  " + // Push StringPool strings
                            "0F BE 00" // movsx eax, byte ptr [eax]
                            );

                        int CallOffset = Array.FindIndex(FindGetString, (byte b) => { return b == 0xE8; });

                        uint curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindGetString, 0xAA, true))
                        {
                            var tmp = (int)br.BaseStream.Position;
                            br.BaseStream.Position += CallOffset;
                            int pos = CalculateAddressLocation(br);
                            if (pos < 0) continue;
                            br.BaseStream.Position = pos - FileOffset;
                            if (FindAoBInFile(br, (uint)br.BaseStream.Position, FindIsGetStringW, 0xAA, true, 100))
                            {
                                _GetStringW = (int)(tmp + FileOffset);
                                DLOG.WriteLine("Found GetStringW @ {0:X8}", (int)(tmp + FileOffset));
                            }
                            else
                            {
                                _GetString = (int)(tmp + FileOffset);
                                DLOG.WriteLine("Found GetString @ {0:X8}", (int)(tmp + FileOffset));
                            }
                            curpoozzz = (uint)(tmp + FindGetString.Length);
                        }
                    }
                    AddPoint();
                    if (_GetString == 0 || _GetStringW == 0)
                    {
                        // Search 2

                        DLOG.WriteLine("Searching string decoders, AoB 2 (1 failed?)");

                        byte[] FindGetString = ByteStringToArray(
                            "51 " + // Push ECX
                            "8B 44 24 AA " +  // Mov EAX, ESP+4+??
                            "56 " + // Push ESI
                            "8B 74 24 AA  " + // Mov ESI, ESP+8+??
                            "6A 00 " + // Push 0
                            "50 " + // Push EAX
                            "56 " + // Push ESI
                            "C7   AA AA AA    AA AA AA 00 " + // Mov ESP + ???? + ?????, ????
                            "E8 AA AA AA AA " + // Call GetString or GetStringW!! (only these 2)
                            "8B C6  " + // Mov EAX, ESI
                            "5E " + // Pop ESI
                            "59 " + // Pop ECX
                            "C2 08 00" // Retn 8
                            );

                        byte[] FindIsGetStringW = ByteStringToArray(
                            "83 EC 08 " + // sub ESP, 8
                            "53 " + // Push EBX 
                            "55 " + // Push EBP
                            "56 " + // Push ESI
                            "57 "   // Push EDI
                            );

                        int CallOffset = Array.FindIndex(FindGetString, (byte b) => { return b == 0xE8; });

                        uint curpoozzz = 1000;
                        while (FindAoBInFile(br, curpoozzz, FindGetString, 0xAA, true))
                        {
                            var tmp = (int)br.BaseStream.Position;
                            br.BaseStream.Position += CallOffset;
                            int pos = CalculateAddressLocation(br);
                            if (pos < 0) continue;
                            br.BaseStream.Position = pos - FileOffset;
                            if (FindAoBInFile(br, (uint)br.BaseStream.Position, FindIsGetStringW, 0xAA, true, 100))
                            {
                                _GetStringW = (int)(tmp + FileOffset);
                                DLOG.WriteLine("Found GetStringW @ {0:X8}", (int)(tmp + FileOffset));
                            }
                            else
                            {
                                _GetString = (int)(tmp + FileOffset);
                                DLOG.WriteLine("Found GetString @ {0:X8}", (int)(tmp + FileOffset));
                            }
                            curpoozzz = (uint)(tmp + FindGetString.Length);
                        }
                    }
                }

                AddPoint();

                List<int> Callers = new List<int>();
                {
                    int c1 = 0, c2 = 0, c3 = 0, c4 = 0;

                    byte[] FindCalls = ByteStringToArray(
                        "68 AA AA AA AA " + // Push string value
                        "50" + // Push EAX
                        "E8 AA AA AA AA " + // CALL GetInstance 
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );

                    var DetermineType = (Func<long, object[]>)delegate(long Address)
                    {
                        string t = "";
                        StringDecodeTypes sdt = StringDecodeTypes.Unknown;
                        if (Address == _GetStringW)
                        {
                            c1++;
                            sdt = StringDecodeTypes.GetStringW;
                        }
                        else if (Address == _GetString)
                        {
                            c2++;
                            sdt = StringDecodeTypes.GetString;
                        }
                        else if (_GetBSTRT == 0 || _GetBSTRT == Address)
                        {
                            c3++;
                            if (_GetBSTRT == 0)
                            {
                                _GetBSTRT = (int)Address;
                                DLOG.WriteLine("Found GetBSTR! {0:X8}", _GetBSTRT);
                            }
                            sdt = StringDecodeTypes.GetBSTR;
                        }
                        else
                        {
                            c4++;
                            sdt = StringDecodeTypes.Unknown;
                        }
                        t = sdt.ToString("G");
                        return new object[] { t, sdt };
                    };

                    DLOG.WriteLine("Starting seeking all GetString calls.");
                    uint curpoozzz = 1000;
                    //string kutshitfuck = string.Format("{0,-14} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    while (FindAoBInFile(br, curpoozzz, FindCalls, 0xAA, true))
                    {

                        br.ReadByte();
                        int StringID = br.ReadInt32();
                        br.ReadByte();
                        long InstanceAddr = CalculateAddressLocation(br); // GetInstance()
                        br.ReadInt16();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br); // GetString[W]()

                        if (DecodeFunctionAddr > 0)
                        {
                            string t = "1 ";
                            var ret = DetermineType(DecodeFunctionAddr);
                            t += (string)ret[0];
                            StringDecodeTypes sdt = (StringDecodeTypes)ret[1];

                            if (sdt != StringDecodeTypes.Unknown)
                            {
                                if (!StringReferences.ContainsKey(StringID))
                                    StringReferences.Add(StringID, new Dictionary<int, StringDecodeTypes>());
                                StringReferences[StringID].Add((int)tmp, sdt);

                                Callers.Add((int)tmp);
                                if (_StringPoolInstance == 0)
                                {
                                    _StringPoolInstance = (int)InstanceAddr;
                                }
                            }

                            //kutshitfuck += string.Format("{0,-14} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, StringID, InstanceAddr, DecodeFunctionAddr);
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }

                    DLOG.WriteLine("Wide: {0}; Western: {1}; BSTR: {2}; Unknown: {3}", c1, c2, c3, c4);
                    AddPoint();

                    //kutshitfuck += string.Format("{0,-14} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    c1 = c2 = c3 = c4 = 0;
                    DLOG.WriteLine("Starting seeking all 'different' GetString calls.");

                    byte[] FindCalls2 = ByteStringToArray(
                        "68 AA AA AA AA " + // Push string value
                        "68 AA AA AA AA " + // Push Random Value?
                        "E8 AA AA AA AA " + // CALL GetInstance 
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );

                    curpoozzz = 1000;
                    while (FindAoBInFile(br, curpoozzz, FindCalls2, 0xAA, true))
                    {

                        br.ReadByte();
                        int StringID = br.ReadInt32();
                        br.ReadByte();
                        br.ReadInt32(); // Some argument?
                        long InstanceAddr = CalculateAddressLocation(br); // GetInstance()
                        br.ReadInt16();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br); // GetString[W]()

                        if (DecodeFunctionAddr > 0)
                        {
                            string t = "2 ";
                            var ret = DetermineType(DecodeFunctionAddr);
                            t += (string)ret[0];
                            StringDecodeTypes sdt = (StringDecodeTypes)ret[1];

                            if (sdt != StringDecodeTypes.Unknown)
                            {
                                if (!StringReferences.ContainsKey(StringID))
                                    StringReferences.Add(StringID, new Dictionary<int, StringDecodeTypes>());
                                StringReferences[StringID].Add((int)tmp, sdt);

                                Callers.Add((int)tmp);
                                if (_StringPoolInstance == 0)
                                {
                                    _StringPoolInstance = (int)InstanceAddr;
                                }
                            }

                            //kutshitfuck += string.Format("{0,-14} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, StringID, InstanceAddr, DecodeFunctionAddr);
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }

                    DLOG.WriteLine("Wide: {0}; Western: {1}; BSTR: {2}; Unknown: {3}", c1, c2, c3, c4);
                    AddPoint();

                    //kutshitfuck += string.Format("{0,-14} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    c1 = c2 = c3 = c4 = 0;
                    DLOG.WriteLine("Starting seeking all 'even more different' GetString calls.");

                    byte[] FindCalls3 = ByteStringToArray(
                        "68 AA AA AA AA " + // Push string value
                        "50" + // Push EAX
                        "C6 AA AA AA" + // mov things
                        "E8 AA AA AA AA " + // CALL GetInstance 
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );

                    curpoozzz = 1000;
                    while (FindAoBInFile(br, curpoozzz, FindCalls3, 0xAA, true))
                    {

                        br.ReadByte();
                        int StringID = br.ReadInt32();
                        br.ReadByte();
                        br.ReadInt32(); // Move stuff... not really an integer but w/e
                        long InstanceAddr = CalculateAddressLocation(br); // GetInstance()
                        br.ReadInt16();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br); // GetString[W]()

                        if (DecodeFunctionAddr > 0)
                        {
                            string t = "3 ";
                            var ret = DetermineType(DecodeFunctionAddr);
                            t += (string)ret[0];
                            StringDecodeTypes sdt = (StringDecodeTypes)ret[1];

                            if (sdt != StringDecodeTypes.Unknown)
                            {
                                if (!StringReferences.ContainsKey(StringID))
                                    StringReferences.Add(StringID, new Dictionary<int, StringDecodeTypes>());
                                StringReferences[StringID].Add((int)tmp, sdt);

                                Callers.Add((int)tmp);
                                if (_StringPoolInstance == 0)
                                {
                                    _StringPoolInstance = (int)InstanceAddr;
                                }
                            }

                            //kutshitfuck += string.Format("{0,-14} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, StringID, InstanceAddr, DecodeFunctionAddr);
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }
                    DLOG.WriteLine("Wide: {0}; Western: {1}; BSTR: {2}; Unknown: {3}", c1, c2, c3, c4);
                    AddPoint();



                    //kutshitfuck += string.Format("{0,-14} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    c1 = c2 = c3 = c4 = 0;
                    DLOG.WriteLine("Starting seeking all 'even more different2' GetString calls.");

                    byte[] FindCalls4 = ByteStringToArray(
                        "68 AA AA AA AA " + // Push string value
                        "50" + // Push EAX
                        "C7 AA AA AA AA AA AA" + // mov register + offset, value (int)
                        "E8 AA AA AA AA " + // CALL GetInstance 
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );

                    curpoozzz = 1000;
                    while (FindAoBInFile(br, curpoozzz, FindCalls4, 0xAA, true))
                    {

                        br.ReadByte();
                        int StringID = br.ReadInt32();
                        br.ReadByte(); // Push Eax

                        br.ReadByte(); // Instruction
                        br.ReadInt16(); // Register + argument
                        br.ReadInt32(); // Value

                        long InstanceAddr = CalculateAddressLocation(br); // GetInstance()
                        br.ReadInt16();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br); // GetString[W]()

                        if (DecodeFunctionAddr > 0)
                        {
                            string t = "4 ";
                            var ret = DetermineType(DecodeFunctionAddr);
                            t += (string)ret[0];
                            StringDecodeTypes sdt = (StringDecodeTypes)ret[1];

                            if (sdt != StringDecodeTypes.Unknown)
                            {
                                if (!StringReferences.ContainsKey(StringID))
                                    StringReferences.Add(StringID, new Dictionary<int, StringDecodeTypes>());
                                StringReferences[StringID].Add((int)tmp, sdt);

                                Callers.Add((int)tmp);
                                if (_StringPoolInstance == 0)
                                {
                                    _StringPoolInstance = (int)InstanceAddr;
                                }
                            }

                            //kutshitfuck += string.Format("{0,-14} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, StringID, InstanceAddr, DecodeFunctionAddr);
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }
                    DLOG.WriteLine("Wide: {0}; Western: {1}; BSTR: {2}; Unknown: {3}", c1, c2, c3, c4);
                    AddPoint();




                    //kutshitfuck += string.Format("{0,-14} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    c1 = c2 = c3 = c4 = 0;
                    DLOG.WriteLine("Starting seeking all 'even more different3' GetString calls.");

                    byte[] FindCalls5 = ByteStringToArray(
                        "68 AA AA AA AA " + // Push string value
                        "50" + // Push EAX
                        "89 AA AA" + // mov argument value, register :/
                        "E8 AA AA AA AA " + // CALL GetInstance 
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );

                    curpoozzz = 1000;
                    while (FindAoBInFile(br, curpoozzz, FindCalls5, 0xAA, true))
                    {

                        br.ReadByte();
                        int StringID = br.ReadInt32();

                        br.ReadByte(); // Push Eax

                        br.ReadByte(); // Instruction
                        br.ReadInt16(); // Argument value, register

                        long InstanceAddr = CalculateAddressLocation(br); // GetInstance()
                        br.ReadInt16();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br); // GetString[W]()

                        if (DecodeFunctionAddr > 0)
                        {
                            string t = "4 ";
                            var ret = DetermineType(DecodeFunctionAddr);
                            t += (string)ret[0];
                            StringDecodeTypes sdt = (StringDecodeTypes)ret[1];

                            if (sdt != StringDecodeTypes.Unknown)
                            {
                                if (!StringReferences.ContainsKey(StringID))
                                    StringReferences.Add(StringID, new Dictionary<int, StringDecodeTypes>());
                                StringReferences[StringID].Add((int)tmp, sdt);

                                Callers.Add((int)tmp);
                                if (_StringPoolInstance == 0)
                                {
                                    _StringPoolInstance = (int)InstanceAddr;
                                }
                            }

                            //kutshitfuck += string.Format("{0,-14} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, StringID, InstanceAddr, DecodeFunctionAddr);
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }
                    DLOG.WriteLine("Wide: {0}; Western: {1}; BSTR: {2}; Unknown: {3}", c1, c2, c3, c4);
                    AddPoint();

                    //File.WriteAllText("dmp.txt", kutshitfuck);
                }

                {
                    int c1 = 0, c2 = 0, c3 = 0;

                    byte[] FindCalls = ByteStringToArray(
                        "8B C8" + // Mov ECX, EAX
                        "E8 AA AA AA AA" // Call GetString
                        );
                    uint curpoozzz = 1000;
                    //string kutshitfuck = string.Format("{0,-10} | {1,10} | {2,6} | {3,10} | {4,10}\r\n", "Type", "Location", "ID", "Instance", "Function");
                    while (FindAoBInFile(br, curpoozzz, FindCalls, 0xAA, true))
                    {
                        br.ReadByte();
                        br.ReadByte();
                        var tmp = br.BaseStream.Position;
                        long DecodeFunctionAddr = CalculateAddressLocation(br);

                        if (DecodeFunctionAddr > 0)
                        {
                            if (Callers.Contains((int)tmp))
                            {
                                ////kutshitfuck += string.Format("{0,-10} | {1,10} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", "AlrdyFnd", tmp, "Unk", 0, DecodeFunctionAddr);
                            }
                            else
                            {
                                string t = "";

                                if (DecodeFunctionAddr == _GetStringW)
                                {
                                    c1++;
                                    t += "WideString";
                                }
                                else if (DecodeFunctionAddr == _GetString)
                                {
                                    c2++;
                                    t += "WesternStr";
                                }
                                else if (_GetBSTRT == DecodeFunctionAddr)
                                {
                                    c3++;
                                    t += "STR_BSTR";
                                }
                                else
                                {
                                    curpoozzz = (uint)(br.BaseStream.Position);
                                    continue;
                                }

                                //kutshitfuck += string.Format("{0,-10} | {1,10:X8} | {2,6} | {3,10:X8} | {4,10:X8}\r\n", t, tmp + FileOffset, -1, "Unk", DecodeFunctionAddr);
                            }
                        }
                        curpoozzz = (uint)(br.BaseStream.Position);
                    }

                    //File.WriteAllText("dmp2.txt", kutshitfuck);
                    DLOG.WriteLine("New? Wide: {0}; Western: {1}; BSTR: {2}", c1, c2, c3);
                    AddPoint();
                }

                #endregion

                DLOG.WriteLine("Strings found that are referenced: {0}", StringReferences.Count);
                DLOG.WriteLine("Addresses found with GetString calls: {0}", Callers.Count);
                DLOG.WriteLine("StringPool::GetString: {0:X8}, StringPool::GetStringW: {1:X8}, StringPool::GetBSTR: {2:X8}, StringPool::GetInstance: {3:X8}", _GetString, _GetStringW, _GetBSTRT, _StringPoolInstance);

                #region STOPPED Find Version
                /*
                if (!FindAoBInFile(br, 0x0044206E - FileOffset, MakeVersionString, 0xAA, true))
                {
                    DLOG.WriteLine("Could not find Make Version String function....");
                }
                else
                {
                    br.BaseStream.Position += 6;
                    DLOG.WriteLine("StringPool::GetInstance() function should begin @ {0:X8}", CalculateAddressLocation(br));
                    br.BaseStream.Position += 2;
                    uint getstringfuncaddr = CalculateAddressLocation(br);
                    DLOG.WriteLine("StringPool::GetString(int, int) function should begin @ {0:X8}", getstringfuncaddr);
                    br.BaseStream.Position += 3;
                    DLOG.WriteLine("Current Pos: {0:X8}", br.BaseStream.Position + FileOffset);
                    byte subver = br.ReadByte();
                    byte version = 0;
                    if (br.ReadByte() == 0x6A)
                    { // Normal byte push.
                        version = br.ReadByte();
                    }
                    DLOG.WriteLine(">> Working with V{0}.{1}", version, subver);

                    this.Invoke((MethodInvoker)delegate
                    {
                        tsClientVersion.Text = string.Format("Ver. {0}.{1}", version, subver);
                    });
                }*/

                #endregion

                AddPoint();
                #region Seek Connection Info
                if (MessageBox.Show("Skip seeking IPs?", "-.-", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
                {
                    byte[] IPAoB = ByteStringToArray(
                        "68 AA AA AA 00 " + // Port
                        "68 AA AA AA 00 " + // Addr of IP
                        "B9 AA AA AA AA " + // ?
                        "E8 AA AA AA FF " // Add to list
                        );
                    DLOG.WriteLine("[TEST] Searching for IP's + Ports inside client");
                    uint posCurrent = 0x0010EEE8;
                    uint port = 0;
                    byte mode = 0;
                    if (!FindAoBInFile(br, posCurrent, IPAoB, 0xAA, true))
                    {
                        IPAoB = ByteStringToArray(
                        "56 " + // ESI = Port
                        "68 AA AA AA 00 " + // Addr of IP
                        "B9 AA AA AA 00 " + // ?
                        "E8 AA AA AA FF " // Add to list
                        );
                        while (true)
                        {
                            if (FindAoBInFile(br, posCurrent, IPAoB, 0xAA, true))
                            {
                                br.BaseStream.Position -= 4;
                                GlobalPortPos = (int)br.BaseStream.Position;

                                port = br.ReadUInt32();
                                if (port < ushort.MinValue || port > ushort.MaxValue)
                                {
                                    DLOG.WriteLine("[INFO] This one is not correct. Lets seek another one. ({0}!)", port);
                                    posCurrent = (uint)(br.BaseStream.Position + 120);
                                    continue;
                                }

                                this.Invoke((MethodInvoker)delegate
                                {
                                    lvIPs.Columns[1].Width = 0;
                                    tmp_lblPort.Visible = tmp_udPort.Visible = true;
                                    tmp_udPort.Value = port;
                                });
                                DLOG.WriteLine("[INFO] Mode 2. Base Port: {0}", port);
                                mode = 2;
                                break;
                            }
                            else
                            {
                                DLOG.WriteLine("[INFO] Could not find correct AoB :|");
                                break;
                            }
                        }
                    }
                    else
                    {
                        DLOG.WriteLine("[INFO] Mode 1.");
                        this.Invoke((MethodInvoker)delegate
                        {
                            lvIPs.Columns[1].Width = 80;
                            tmp_lblPort.Visible = tmp_udPort.Visible = false;
                        });
                        mode = 1;
                    }
                    AddPoint();
                    posCurrent = (uint)br.BaseStream.Position;
                    uint thisIPStartsAt = 0;

                    #region Seek IP Addrs
                    if (mode != 0)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            lvIPs.Items.Clear();
                        });
                        for (int i = 1; ; i++)
                        {
                            if (FindAoBInFile(br, posCurrent, IPAoB, 0xAA, true))
                            {
                                if (br.BaseStream.Position - posCurrent > IPAoB.Length + 3)
                                {
                                    DLOG.WriteLine("[?] Broke with check");
                                    break;
                                }
                                thisIPStartsAt = (uint)br.BaseStream.Position;
                                DLOG.WriteLine("[INFO] Found LoginServer IP:PORT list push @ {0:X8} (FP: {1:X8})", br.BaseStream.Position + FileOffset, br.BaseStream.Position);
                                if (mode == 1)
                                {
                                    br.ReadByte();
                                    int portpos = (int)br.BaseStream.Position;
                                    port = br.ReadUInt32();
                                    if (port < 0 || port > 0xFFFF)
                                    {
                                        DLOG.WriteLine("[WTF] Strange port found! {0}", port);
                                        break;
                                    }
                                    br.ReadByte();

                                    uint ip_pos = br.ReadUInt32();
                                    var tmp = br.BaseStream.Position;
                                    DLOG.WriteLine("[DEBUG] {0:X8} > {1:X8}", ip_pos, ip_pos - FileOffset);

                                    br.BaseStream.Position = ip_pos - FileOffset;
                                    DebugBuffer(br);
                                    string ip = GetString(br, false);
                                    int len = ip.Length;
                                    while (br.ReadByte() == 0x00)
                                    {
                                        len += 1;
                                    }
                                    br.BaseStream.Position = tmp;

                                    DLOG.WriteLine("[WIN] IP {0} found! File location: {1:X8} ({3}:{2})", i, ip_pos, port, ip);

                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        var lvi = new ListViewItem(new string[] { ip, port.ToString() });
                                        lvi.Tag = new object[] { (int)(ip_pos - FileOffset), len, (int)portpos, thisIPStartsAt, IPAoB.Length };
                                        lvIPs.Items.Add(lvi);
                                    });
                                    {
                                        var tmpIPOffset = (int)(ip_pos - FileOffset);
                                        /*
                                         * IP1 = 0
                                         * IP2 = IP1 - 12
                                         * IP3 = IP2 - 12
                                         * */

                                        if (____ip_declaration_pos == 0)
                                        {
                                            ____ip_declaration_pos = tmpIPOffset;
                                            ____ip_max_len = len;
                                        }
                                        else
                                        {
                                            var offsets = Math.Abs(tmpIPOffset - ____ip_declaration_pos);
                                            if (offsets == len + 1) // len + 1, because the last byte is set to 0
                                            {
                                                // Correct offset
                                                if (____ip_declaration_pos > tmpIPOffset) // Last was above other
                                                {
                                                    ____ip_declaration_pos = tmpIPOffset;
                                                    ____ip_max_len += offsets;
                                                }
                                                else if (____ip_declaration_pos < tmpIPOffset) // Last was underneath other
                                                {
                                                    // IP1 = 0 , len 12
                                                    // IP2 = 13 , len 12
                                                    // Result = 13 + len
                                                    ____ip_max_len = offsets + len;
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Not correct offset");
                                            }
                                        }
                                    }
                                }
                                else if (mode == 2) // esi holds port.
                                {
                                    br.ReadByte();
                                    br.ReadByte();

                                    uint ip_pos = br.ReadUInt32();
                                    var tmp = br.BaseStream.Position;
                                    br.BaseStream.Position = ip_pos - FileOffset;
                                    DebugBuffer(br);
                                    string ip = GetString(br, false);
                                    int len = ip.Length;
                                    while (br.ReadByte() == 0x00)
                                    {
                                        len += 1;
                                    }
                                    br.BaseStream.Position = tmp;

                                    DLOG.WriteLine("[WIN] IP {0} found! File location: {1:X8} ({3}:{2})", i, ip_pos, port, ip);

                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        var lvi = new ListViewItem(new string[] { ip, port.ToString() });
                                        lvi.Tag = new object[] { (int)(ip_pos - FileOffset), len, (int)0, thisIPStartsAt, IPAoB.Length };
                                        lvIPs.Items.Add(lvi);
                                    });

                                    {
                                        var tmpIPOffset = (int)(ip_pos - FileOffset);
                                        /*
                                         * IP1 = 0
                                         * IP2 = IP1 - 12
                                         * IP3 = IP2 - 12
                                         * */

                                        if (____ip_declaration_pos == 0)
                                        {
                                            ____ip_declaration_pos = tmpIPOffset;
                                            ____ip_max_len = len;
                                        }
                                        else
                                        {
                                            var offsets = Math.Abs(tmpIPOffset - ____ip_declaration_pos);
                                            if (offsets == len + 1) // len + 1, because the last byte is set to 0
                                            {
                                                // Correct offset
                                                if (____ip_declaration_pos > tmpIPOffset) // Last was above other
                                                {
                                                    ____ip_declaration_pos = tmpIPOffset;
                                                    ____ip_max_len += offsets;
                                                }
                                                else if (____ip_declaration_pos < tmpIPOffset) // Last was underneath other
                                                {
                                                    // IP1 = 0 , len 12
                                                    // IP2 = 13 , len 12
                                                    // Result = 13 + len
                                                    ____ip_max_len = offsets + len;
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Not correct offset");
                                            }
                                        }
                                    }
                                }
                                posCurrent = (uint)br.BaseStream.Position;
                            }
                            else
                            {
                                DLOG.WriteLine("[ERROR] Stopped! Found {0} IP's!", i - 1);
                                break;
                            }
                        }
                    }

                    #endregion
                }
                #endregion


                AddPoint();


                this.Invoke((MethodInvoker)delegate
                {
                    btn_ForceUse.Enabled = lvIPs.Items.Count > 1;
                });
                DLOG.WriteLine("[DEBUG] Current File Offset {0:X8}", FileOffset);
                DLOG.WriteLine("[DEBUG] Strings amount Pos {0:X8}", StringsAmountPos);
                br.BaseStream.Position = PlainOffsetToFileOffset(StringsAmountPos);
                DebugBuffer(br);
                _StringAmount = br.ReadUInt32();
                if (_StringAmount < 0 || _StringAmount > 20000)
                {
                    string msg = "Strange place for strings found I guess: " + ((br.BaseStream.Position - 4) + FileOffset).ToString("X8") + " FP:" + (br.BaseStream.Position - 4).ToString("X8") + " (Amount of strings found: " + _StringAmount + ")";
                    DLOG.WriteLine("[ERROR] " + msg);
                    MessageBox.Show(msg);
                    return;
                }
                DLOG.WriteLine("[DEBUG] Strings in client: {0}", _StringAmount);
                AddPoint();

                if (!gotkeysize)
                {
                    br.BaseStream.Position = PlainOffsetToFileOffset(KeySizePos);
                    int keysize = br.ReadInt32();
                    DLOG.WriteLine("[DEBUG] Decode Key Size: {0} bytes", keysize);
                    if (16 != keysize)
                    {
                        DLOG.WriteLine(string.Format("[ERROR] Wups. 2 different size of keys! {0} and {1}", _DecodeKeySize, keysize));
                        MessageBox.Show(string.Format("Wups. 2 different size of keys! {0} and {1}", _DecodeKeySize, keysize));
                        return;
                    }
                    _DecodeKeySize = keysize;
                }

                br.BaseStream.Position = PlainOffsetToFileOffset(KeyPos);
                DecodeKey = br.ReadBytes(_DecodeKeySize);
                DLOG.Write("[DEBUG] Decode Key: ");
                foreach (byte b in DecodeKey)
                {
                    DLOG.Write("{0:X2} ", b);
                }
                DLOG.WriteLine();
                AddPoint();

                // Encoding question
                frmEncoding ec = new frmEncoding();
                ec.ShowDialog();
                _currentEncoding = Encoding.GetEncoding(frmEncoding.ChosenEncoding);


                br.BaseStream.Position = PlainOffsetToFileOffset(StringsPos);

                DataTable dt = new DataTable("FileStrings");
                dt.Columns.Add("ID", typeof(int)).ReadOnly = true;
                dt.Columns.Add("Content", typeof(string));
                dt.Columns.Add("Non-Western", typeof(bool)).ReadOnly = true;
                dt.Columns.Add("Found in client (Not reliable)", typeof(bool)).ReadOnly = true;
                dt.Columns.Add("Decode Type", typeof(StringDecodeTypes)).ReadOnly = true;
                dt.Columns.Add("Address", typeof(String)).ReadOnly = true;


                bool unicode = false;
                for (int i = 0; i < _StringAmount; i++)
                {
                    string decoded = Decode(br, i, out unicode);
                    StringDecodeTypes decodeType = StringDecodeTypes.Unknown;
                    bool containsKey = StringReferences.ContainsKey(i);
                    string address = "-";
                    if (containsKey)
                    {
                        decodeType = StringReferences[i].First((a) => { address = (a.Key + FileOffset).ToString("X8"); return true; }).Value; // Awesomeness itself
                    }
                    dt.Rows.Add(i, decoded, unicode, containsKey, decodeType, address);
                }

                AddPoint();

                var currentBlockSize = BlockStrings.Size;
                DLOG.WriteLine("[INFO] -   Strings Block Occupied Size: {0} bytes (from {1:X8} to {2:X8})", BlockStrings.Size, BlockStrings.Min, BlockStrings.Max);

                br.BaseStream.Position = BlockStrings.Max - FileOffset; // I still don't get why
                if (br.ReadByte() == 0xFE)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        famSep.Visible = famLabel.Visible = true;
                    });
                    // Ha! Already modified
                    // MessageBox.Show("[NOTE] Already Modified Client!");
                    DLOG.WriteLine("[WARN] Already Modified Client!");
                    while (br.ReadByte() == 0xFF) ;
                }
                else
                {
                    //MessageBox.Show("[INFO] If you save the file now, you'll get a lot more space available.");
                }
                br.BaseStream.Position -= 1; // No space available.
                AddPoint();
                BlockStrings.Max = (uint)(br.BaseStream.Position + FileOffset);

                bytesfree = (int)(BlockStrings.Size - currentBlockSize);
                DLOG.WriteLine("[INFO] -   Strings Block Total Size: {0} bytes (from {1:X8} to {2:X8})", BlockStrings.Size, BlockStrings.Min, BlockStrings.Max);
                DLOG.WriteLine("[INFO] -   StringPool Block Size: {0} bytes (from {1:X8} to {2:X8})", BlockStringPool.Size, BlockStringPool.Min, BlockStringPool.Max);
                DLOG.WriteLine("[INFO] -   Bytes Free: {0} bytes", bytesfree);

                this.Invoke((MethodInvoker)delegate
                {
                    dgvStrings.DataSource = dt;
                    tsBlockSize.Text = string.Format(STR_DataSize, BlockStrings.Size, BlockStringPool.Size);
                    tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
                    this.Text = STR_Title + " - File: " + pFilename;
                    saveToolStripMenuItem1.Enabled = true;
                    cSVToolStripMenuItem1.Enabled = true;
                });

                //frmMain.StringsOutput += string.Format("{0:X8} - {1:X8}\r\n", BlockStrings.Min - FileOffset, BlockStrings.Max - FileOffset);

                //File.WriteAllText("test.txt", frmMain.StringsOutput);

                AddNewFile(pFilename);
                loadThread = null;
            }
            catch (Exception ex)
            {
                DLOG.WriteLine("EXC: Exception written to exlog.txt!");
                MessageBox.Show(ex.ToString());
                File.AppendAllText(Program.DATAFOLDER + "exlog.txt", "---- " + DateTime.Now + " --" + "\r\n");
                File.AppendAllText(Program.DATAFOLDER + "exlog.txt", "--------------Exception--------------\r\n" + ex.ToString() + "\r\n\r\n\r\n\r\n");
            }
        }


        static byte[] ByteStringToArray(params string[] pInputStrings)
        {
            string input = string.Join(" ", pInputStrings).Replace(" ", "").Trim();

            if (input.Length % 2 == 1) throw new Exception("Could not convert Byte String to Array. Size incorrect");


            byte[] bytes = new byte[input.Length / 2];
            for (int i = 0; i < input.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(input.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
            }

            InitializeAoBLocationFlags(ref bytes);

            DLOG.Write("[FindAOB] IDA AOB: ");
            foreach (byte b in bytes)
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

            return bytes;
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

        static List<long> locationFlags, locationFlagsInAoB;
        static string LOCATION_FLAG_STRING = "464C4147464C4147";
        static readonly byte[] LOCATION_FLAG_BYTES = new byte[] { 0x46, 0x4C, 0x41, 0x47, 0x46, 0x4C, 0x41, 0x47 };

        static void InitializeAoBLocationFlags(ref byte[] AoB)
        {
            locationFlags = new List<long>();
            locationFlagsInAoB = new List<long>();

            // Filter offsets in AoB
            byte[] bufferred = new byte[AoB.Length];
            int i = 0, j = 0;
            int newOffset = 0;
            for (; i < bufferred.Length; i++)
            {
                if (AoB[i] == LOCATION_FLAG_BYTES[j])
                {
                    j++;
                    if (j == LOCATION_FLAG_BYTES.Length)
                    {
                        j = 0;
                        locationFlagsInAoB.Add(i - LOCATION_FLAG_BYTES.Length);
                    }
                }
                else
                {
                    if (j > 0)
                    {
                        for (var k = j; k > 0; k--)
                        {
                            bufferred[newOffset] = AoB[i - k];
                            newOffset++;
                        }
                    }
                    j = 0;
                    bufferred[newOffset] = AoB[i];
                    newOffset++;
                }
            }

            Array.Resize<byte>(ref bufferred, newOffset);

            AoB = bufferred;
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

                        foreach (var offset in locationFlagsInAoB)
                            locationFlags.Add((realpos - AoB.Length) + offset);
                        return true;
                    }
                    if (length != 0) return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            DLOG.WriteLine("[FindAOB] ------- EOF ---------");
            br.BaseStream.Position = StartPos;
            return false;
        }

        static uint PlainOffsetToFileOffset(uint off)
        {
            return off - FileOffset;
        }

        static uint CalculateStringPosition(int x)
        {
            if (x > _StringAmount) throw new Exception("Shit. String not found!");
            return StringsPos + (4 * (uint)x);
        }


        static string Decode(BinaryReader br, int stringPos, out bool _bstr_td_text)
        {
            int ttt = stringPos;
            stringPos = (int)PlainOffsetToFileOffset(CalculateStringPosition(stringPos));
            UpdateBlock((uint)(stringPos + FileOffset), true);
            br.BaseStream.Position = stringPos;

            int v = br.ReadInt32();
            UpdateBlock((uint)v, false);
            br.BaseStream.Position = v - FileOffset;


            byte[] lulzkey = rotatel(DecodeKey, (uint)_DecodeKeySize, br.ReadSByte());


            List<byte> encryptedStringBuffer = new List<byte>();
            while (true)
            {
                byte ch = br.ReadByte();
                if (ch == 0) break;
                encryptedStringBuffer.Add(ch);
            }

            // lolfail
            UpdateBlock((uint)(br.BaseStream.Position + FileOffset), false);

            string ret = "";
            int i = 0;
            for (; i < encryptedStringBuffer.Count; i++)
            {
                byte lul = lulzkey[i % _DecodeKeySize];
                byte esb = encryptedStringBuffer[i];

                if (esb == lul)
                    esb = lul;
                else
                    esb ^= lul;
                if (esb == '\r') ret += @"\r";
                else if (esb == '\n') ret += @"\n";
                else ret += (char)esb;
            }
            
            _bstr_td_text = false;
            for (i = 0; i < ret.Length; i++)
            {
                if (ret[i] > sbyte.MaxValue)
                {
                    _bstr_td_text = true;
                    break;
                }
            }
           
            if (_bstr_td_text)
            {
                byte[] buffer = new byte[ret.Length];
                for (i = 0; i < ret.Length; i++)
                {
                    buffer[i] = (byte)ret[i];
                }
                ret = _currentEncoding.GetString(buffer);
            }

            //StringsOutput += string.Format("{0,6} ({3:X8} -> {4:X8} - {5:X8}) :{1,5}:{2}\r\n", ttt, ret.Length, ret, stringPos, v - FileOffset, br.BaseStream.Position);

            return ret;
        }

        static byte[] rotatel(byte[] value, uint length, int shift)
        {
            byte[] v4 = new byte[length];
            Buffer.BlockCopy(value, 0, v4, 0, (int)length);
            if ((uint)shift < 8)
            {
                goto label_26;
            }
            uint v5 = (((uint)shift >> 3) % length);
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

        string openedFile = "";
        private void openToolStripButton_Click(object sender, EventArgs e)
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

        int bytesfree = 0;

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // check howmany chars edited
            var obj = dgvStrings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewTextBoxCell;
            if (obj.Value is DBNull)
            {
                obj.Value = "";
            }
            var encoding = CheckIfNonWestern(EditingRowValue) ? _currentEncoding : Encoding.ASCII;
            int len = EditingRowValue.Length - encoding.GetByteCount((string)obj.Value);
            bytesfree += len;
            tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var obj = dgvStrings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewTextBoxCell;
            
            EditingRowValue = (string)obj.Value;

            var encoding = CheckIfNonWestern(EditingRowValue) ? _currentEncoding : Encoding.ASCII;

            obj.MaxInputLength = encoding.GetByteCount(EditingRowValue) + bytesfree;
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = openedFile;
                sfd.Filter = "EXE|*.exe";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.Cursor = Cursors.WaitCursor;
                    new System.Threading.Thread(() =>
                    {
                        string tmpfile = openedFile + ".tmp";
                        File.Delete(tmpfile);
                        File.Copy(openedFile, tmpfile);
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "----------------------------------------------------------------\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Write operation starting at " + DateTime.Now + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "BlockStrings.Min = " + BlockStrings.Min.ToString("X8") + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "BlockStrings.Max = " + BlockStrings.Max.ToString("X8") + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "BlockStringPool.Min = " + BlockStringPool.Min.ToString("X8") + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "BlockStringPool.Max = " + BlockStringPool.Max.ToString("X8") + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "File Offset = " + FileOffset.ToString("X8") + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "StringsPos = " + StringsPos.ToString("X8") + "\r\n");

                        this.Invoke((MethodInvoker)delegate
                        {
                            tsLoadProgress.Maximum = lvIPs.Items.Count;
                            tsLoadProgress.Value = 0;
                        });

                        using (MemoryStream mems = new MemoryStream(File.ReadAllBytes(tmpfile)))
                        {
                            using (BinaryWriter br = new BinaryWriter(mems))
                            {
                                File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Nopping data (if needs to be done)\r\n");
                                foreach (var kvp in NopThis)
                                {
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", string.Format("Nopping {0:X8} (len {1})\r\n", kvp.Key, kvp.Value));
                                    br.BaseStream.Position = kvp.Key;
                                    for (uint i = 0; i < kvp.Value; i++)
                                        br.Write((byte)0x90);
                                    br.BaseStream.Position = kvp.Key;
                                }

                                File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Writing IPs to client\r\n");
                                this.Invoke((MethodInvoker)delegate
                                {
                                    foreach (ListViewItem lvi in lvIPs.Items)
                                    {
                                        var tagobjects = lvi.Tag as object[];
                                        br.BaseStream.Position = (int)(tagobjects[0]); // Addr
                                        foreach (char c in lvi.SubItems[0].Text)
                                        {
                                            br.Write(c);
                                        }
                                        for (int i = lvi.SubItems[0].Text.Length; i < (int)(tagobjects[1]); i++)
                                        {
                                            // Filling in the empty bytes
                                            br.Write((byte)0x00);
                                        }
                                        if ((int)(tagobjects[2]) != 0)
                                        {
                                            // Change Port
                                            br.BaseStream.Position = (int)(tagobjects[2]);
                                            br.Write(uint.Parse(lvi.SubItems[1].Text));
                                            File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Written IP '" + lvi.SubItems[0].Text + "' and port '" + lvi.SubItems[1].Text + "' to client\r\n");
                                        }
                                        else
                                        {
                                            File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Written IP '" + lvi.SubItems[0].Text + "' to client\r\n");
                                        }
                                        tsLoadProgress.Value++;
                                    }
                                    if (GlobalPortPos != 0)
                                    {
                                        br.BaseStream.Position = GlobalPortPos;
                                        br.Write((uint)tmp_udPort.Value);
                                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Written global port '" + tmp_udPort.Value + "' to client\r\n");
                                    }
                                });


                                /*
                                //File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Rewriting korean text function calls to Korean Supported calls\r\n");
                                //File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "nope, chuck testa\r\n");
                                foreach (DataRow dr in ((DataTable)dataGridView1.DataSource).Rows)
                                {
                                    int id = (int)dr.ItemArray[0];
                                    string s = (string)dr.ItemArray[1];
                                    if (CheckIfNonWestern(s))
                                    {
                                        //if (StringReferences.ContainsKey(id))
                                        if (s.StartsWith("Ver"))
                                        {
                                            if (!StringReferences.ContainsKey(id))
                                            {
                                                DLOG.WriteLine("Found non-referenced non-western string: ({1}) {0}", s, id);
                                                continue;
                                            }
                                            DLOG.WriteLine("Found referenced string: ({1}) {0}", s, id);
                                            foreach (int add in StringReferences[id])
                                            {
                                                br.BaseStream.Position = add;
                                                DLOG.WriteLine("Rewriting thing @ {0:X8}", add + FileOffset);
                                                CreateAddressLocation(br, _GetStringW, Instructions.Call);
                                            }
                                        }
                                        else
                                        {
                                            // DLOG.WriteLine("Found non-referenced non-western string: ({1}) {0}", s, id);
                                        }
                                    }
                                }
                                */
                                File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Writing strings to client\r\n");

                                br.BaseStream.Position = StringsPos - FileOffset;
                                int startoffset = (int)(BlockStrings.Min - FileOffset);
                                Random rnd = new Random();
                                foreach (DataRow dr in ((DataTable)dgvStrings.DataSource).Rows)
                                {
                                    int id = (int)dr.ItemArray[0];
                                    string s = (string)dr.ItemArray[1];
                                    bool unicode = (bool)dr.ItemArray[2];
                                    WriteString(br, (byte)(rnd.Next() % 255), id, s, unicode, startoffset, out startoffset);
                                }

                                if (br.BaseStream.Position == BlockStrings.Max - FileOffset)
                                {
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Block is not changed or same length!" + "\r\n");
                                }
                                else
                                {
                                    // Fill left over data with 0xFF's lol
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Adding ending byte at " + br.BaseStream.Position.ToString("X8") + "\r\n");
                                    br.Write((byte)0xFE); // Start byte :)
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Filling " + ((BlockStrings.Max - FileOffset) - br.BaseStream.Position).ToString("X8") + " leftover bytes." + "\r\n");
                                    while (br.BaseStream.Position < BlockStrings.Max - FileOffset)
                                    {
                                        br.Write((byte)0xFF);
                                    }
                                }


                            }
                            File.WriteAllBytes(tmpfile, mems.ToArray());
                        }
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Write operation ended at " + DateTime.Now + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Deleting old file and moving temp file\r\n");
                        File.Delete(sfd.FileName);
                        File.Move(tmpfile, sfd.FileName);
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Operation ended at " + DateTime.Now + "\r\n");
                        File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "----------------------------------------------------------------" + "\r\n");
                        this.Invoke((MethodInvoker)delegate
                        {
                            MessageBox.Show("Save was successfull!", "STREDIT");
                            this.Cursor = Cursors.Arrow;
                        });
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                File.AppendAllText(Program.DATAFOLDER + "exlog.txt", "---- " + DateTime.Now + " --" + "\r\n");
                File.AppendAllText(Program.DATAFOLDER + "exlog.txt", "--------------Exception--------------\r\n" + ex.ToString() + "\r\n\r\n\r\n\r\n");
                DLOG.WriteLine("[EXCEPTION] Exception written to exlog.txt!");
                MessageBox.Show("Something bad happend while saving the file! Please post the exlog.txt file at the RaGEZONE forum thread.");
            }
        }

        void WriteString(BinaryWriter bw, byte shift, int i, string value, bool unicode, int offset, out int newoffset)
        {
            var spoff = (BlockStringPool.Min - FileOffset) + (i * 4);
            bw.BaseStream.Position = spoff;
            bw.Write((int)(offset + FileOffset));
            bw.BaseStream.Position = offset;

            byte[] barr;
            if (CheckIfNonWestern(value))
            {
                barr = _currentEncoding.GetBytes(value);
            }
            else
            {
                barr = Encoding.ASCII.GetBytes(value);
            }

            byte[] data = Encrypt(barr, DecodeKey, shift);
            bw.Write(data);
            //File.AppendAllText(Program.DATAFOLDER + "test2.txt", string.Format("{0,5} ({3:X8} -> {1:X8} - {2:X8}) : {4}\r\n", i, offset - FileOffset, bw.BaseStream.Position - FileOffset, spoff - FileOffset, value));
            //deeerpdump += string.Format("{0,5} ({3:X8} -> {1:X8} - {2:X8}) : {4}\r\n", i, offset - FileOffset, bw.BaseStream.Position - FileOffset, spoff - FileOffset, value);
            newoffset = (int)bw.BaseStream.Position;
        }

        byte[] Encrypt(byte[] value, byte[] key, byte shift)
        {
            byte[] lulzkey = rotatel(DecodeKey, (uint)_DecodeKeySize, shift);
            List<byte> buf = new List<byte>();

            buf.Add(shift);
            int j = 0;
            for (int i = 0; i < value.Length; i++)
            {
                byte lul = lulzkey[j % _DecodeKeySize];
                byte esb = value[i];
                byte esb2 = (i + 2 >= value.Length ? (byte)';' : value[i + 1]);
                byte tw = 0;

                if (esb == '\\' && esb2 == 'n')
                {
                    tw = (byte)'\n';
                    i++;
                }
                else if (esb == '\\' && esb2 == 'r') {
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

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = openedFile + ".csv";
            sfd.AddExtension = true;
            sfd.Filter = "CSV|*.csv|All|*.*";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string tmp = "";
                foreach (DataRow dr in ((DataTable)dgvStrings.DataSource).Rows)
                {
                    int id = (int)dr.ItemArray[0];
                    string s = (string)dr.ItemArray[1];
                    tmp += string.Format("{0};\"{1}\"\r\n", id, s.Replace("\"", "\"\""));
                }
                File.WriteAllText(sfd.FileName, tmp);
            }
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (a != null)
                {
                    string s = a.GetValue(0).ToString();
                    LoadFile(s);
                    this.Activate();        // in the case Explorer overlaps this form
                }
            }
            catch (Exception)
            {
                //Trace.WriteLine("Error in DragDrop function: " + ex.Message);
                // don't show MessageBox here - Explorer is waiting !
            }
        }

        private void frmMain_DragEnter(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop) && loadThread == null)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void runCMD_Click(object sender, EventArgs e)
        {
            if (dgvStrings.DataSource == null) return;
            try
            {
                textBox1.BackColor = Color.White;
                ((DataTable)dgvStrings.DataSource).DefaultView.RowFilter = textBox1.Text;
            }
            catch
            {
                textBox1.BackColor = Color.Red;
                ((DataTable)dgvStrings.DataSource).DefaultView.RowFilter = "";
                
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (loadThread != null) loadThread.Abort();
        }

        System.Threading.Thread importCSVthread = null;

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgvStrings.DataSource == null || importCSVthread != null) return;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "CSV|*.csv";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataTable dt = dgvStrings.DataSource as DataTable;
                string[] lines = File.ReadAllLines(ofd.FileName);
                if (lines.Length != dt.Rows.Count)
                {
                    MessageBox.Show("Incompatible CSV file.");
                    DLOG.WriteLine("[LOAD CSV] Failed. Currently {0} rows, and file has {1}", dt.Rows.Count, lines.Length);
                }
                else
                {
                    DLOG.WriteLine("[LOAD CSV] Working!");
                    importCSVthread = new System.Threading.Thread(() =>
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            Dictionary<int, string> changed = new Dictionary<int, string>();
                            foreach (string line in lines)
                            {
                                int id = int.Parse(line.Substring(0, line.IndexOf(';')));
                                string name = line.Substring(line.IndexOf(';') + 1);
                                name = name.Trim('"');
                                name = name.Replace("\"\"", "\"");
                                if (dt.Rows[id][1] as string != name)
                                {
                                    int l = (dt.Rows[id][1] as string).Length - name.Length;
                                    bytesfree += l;
                                    changed.Add(id, name);
                                }
                            }

                            if (bytesfree < 0)
                            {
                                MessageBox.Show("There's not enough room in this client! Try to save the client once to get some space free!");
                            }
                            else
                            {
                                foreach (var kvp in changed)
                                {
                                    dt.Rows[kvp.Key][1] = kvp.Value;
                                }
                                tsBytesAvailable.Text = string.Format("{0:N0} Bytes", bytesfree);
                            }
                        });
                        importCSVthread = null;
                    });
                    importCSVthread.Start();
                }
            }
        }

        private void openUpDlogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("" + Program.DATAFOLDER + "dlog.txt"))
            {
                System.Diagnostics.Process.Start("notepad", "" + Program.DATAFOLDER + "dlog.txt");
            }
            else
            {
                MessageBox.Show("File not found: " + Program.DATAFOLDER + "dlog.txt");
            }
        }

        private void openUpExlogtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("" + Program.DATAFOLDER + "exlog.txt"))
            {
                System.Diagnostics.Process.Start("notepad", "" + Program.DATAFOLDER + "exlog.txt");
            }
            else
            {
                MessageBox.Show("File not found: " + Program.DATAFOLDER + "exlog.txt");
            }
        }

        private void openUpLtxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(Program.DATAFOLDER + "Saving File Log.txt"))
            {
                System.Diagnostics.Process.Start("notepad", Program.DATAFOLDER + "Saving File Log.txt");
            }
            else
            {
                MessageBox.Show("File not found: " + Program.DATAFOLDER + "Saving File Log.txt");
            }
        }

        private void lvIPs_SubItemClicked(object sender, ListViewEx.SubItemEventArgs e)
        {
            var objs = e.Item.Tag as object[];
            if ((int)objs[2] == 0 && e.SubItem != 0) return;
            if (e.SubItem == 0)
            {
                this.inv_txtIpEdit.MaxLength = (int)objs[1];
                lvIPs.StartEditing(this.inv_txtIpEdit, e.Item, e.SubItem);
            }
            else
            {
                lvIPs.StartEditing(this.inv_updownPort, e.Item, e.SubItem);
            }
        }

        private void btnSearchHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("You can use an astrisk (*) in front and/or at the end of the search query\r\n" +
                "to search only things at the front or at the back of a sentence.\r\n" +
                "\r\n" +
                "For example:\r\n" +
                "blaat* - Will search for rows that start with 'blaat' (including 'blaat' itself)\r\n" +
                "*blaat - Will search for rows results that end with 'blaat' (including 'blaat' itself)\r\n" +
                "blaat  - Will search for rows results that are equal to 'blaat'\r\n" +
                "\r\n" +
                "You can use 'ID:' infront of your query to only look at the string ID. This increases the search speed." +
                "For example:\r\n" +
                "ID:9876 - Will highlight the string with ID 9876, if found.", 
                "STREDIT Search Help");
        }

        int currentRow = 0;
        private void btnSearchQuery_Click(object sender, EventArgs e)
        {
            var FindInRow = (Func<string, DataGridViewRow, bool>)delegate(string what, DataGridViewRow row)
            {
                what = what.ToLower();
                if (what.StartsWith("id:"))
                {
                    int id = int.Parse(what.Substring("ID:".Length).Trim());
                    if ((int)row.Cells[0].Value == id)
                    {
                        return true;
                    }
                    return false; // Exception
                }
                if (what.EndsWith("*"))
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        if (row.Cells[i].Value.ToString().ToLower().StartsWith(what.Trim('*')))
                        {
                            return true;
                        }
                    }
                }
                if (what.StartsWith("*"))
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        if (row.Cells[i].Value.ToString().ToLower().EndsWith(what.Trim('*')))
                        {
                            return true;
                        }
                    }
                }
                if (what.EndsWith("*") && what.StartsWith("*"))
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        if (row.Cells[i].Value.ToString().ToLower().Contains(what.Trim('*')))
                        {
                            return true;
                        }
                    }
                }
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    if (row.Cells[i].Value.ToString().ToLower().Equals(what.Trim('*')))
                    {
                        return true;
                    }
                }
                return false;
            };


            if ((ckSearchUp.Checked && currentRow == 0) || (!ckSearchUp.Checked && currentRow == dgvStrings.Rows.Count - 1))
            {
                MessageBox.Show("Nothing found.");
                return;
            }

            if (ckSearchUp.Checked)
            {
                for (int i = currentRow - 1; i > 0; i--)
                {
                    if (FindInRow(txtQuery.Text, dgvStrings.Rows[i]))
                    {
                        SelectRow(i);
                        return;
                    }
                }
            }
            else
            {
                for (int i = currentRow + 1; i < dgvStrings.Rows.Count; i++)
                {
                    if (FindInRow(txtQuery.Text, dgvStrings.Rows[i]))
                    {
                        SelectRow(i);
                        return;
                    }
                }
            }

            MessageBox.Show("Nothing found.");
        }

        void SelectRow(int id, int column = 0)
        {
            currentRow = id;
            var srow = dgvStrings.Rows[id];
            srow.Selected = true;
            dgvStrings.CurrentCell = srow.Cells[column];
        }

        private void dgvStrings_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvStrings.SelectedRows.Count == 0) return;
            currentRow = dgvStrings.SelectedRows[0].Index;
        }

        private void oneTimeTimer_Tick(object sender, EventArgs e)
        {
            oneTimeTimer.Enabled = false;
            DLOG.WriteLine("Checking for updates...");
            CraftNetTools.AppUpdates.Check();
            DLOG.WriteLine("Done!");
        }

        private void famLabel_Click(object sender, EventArgs e)
        {
            if (dgvStrings.DataSource == null) return;
            DataTable dt = dgvStrings.DataSource as DataTable;
            long len = 0;
            foreach (DataRow der in dt.Rows)
            {
                var text = (string)der.ItemArray[1];
                var encoding = CheckIfNonWestern(text) ? _currentEncoding : Encoding.ASCII;
                len += 1; // Starting encoding thing
                len += encoding.GetByteCount(Text);
                len += 1; // Ending zero byte
            }
            derpLabel.Text = "Current Size: " + len.ToString("N0") + " bytes";
            derpLabel.AutoToolTip = true;
            derpLabel.ToolTipText = "This is the amount of bytes used\r\nwhen you save the client right\r\nthis moment.";
        }

        private void btn_ForceUse_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Pressing OK will remove the other IP's and will only use 1 instead!\r\nThis will grant you more size :D!", "!!!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
            {
                ListViewItem readd = null;
                foreach (ListViewItem lvi in lvIPs.Items)
                {
                    // lvi.Tag = new object[] { (int)(ip_pos - FileOffset), len, (int)portpos, thisIPStartsAt, (uint)(br.BaseStream.Position - thisIPStartsAt) };
                    var data = lvi.Tag as object[];
                    if ((int)data[0] == ____ip_declaration_pos) // Just to get the 'first' ip in memory
                    {
                        readd = lvi;
                        continue;
                    }
                    NopThis.Add(new KeyValuePair<uint, uint>((uint)data[3], (uint)(int)data[4])); // moet weg gehaald worden als bestand gesaved word
                }
                lvIPs.Items.Clear();

                //((object[])readd.Tag)[0] = ____ip_declaration_pos;
                ((object[])readd.Tag)[1] = ____ip_max_len;

                lvIPs.Items.Add(readd);
            }
        }

        private void goToCraftNetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.craftnet.nl/");
        }

        private void toEnumerationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = openedFile + ".h";
            sfd.AddExtension = true;
            sfd.Filter = "C Header file|*.h";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string tmp = "enum StringPoolStrings {\r\n";
                foreach (DataRow dr in ((DataTable)dgvStrings.DataSource).Rows)
                {
                    int id = (int)dr.ItemArray[0];
                    string s = (string)dr.ItemArray[1]; // Text
                    s = StripEverything(s.ToLower()).ToUpper();
                    if (s.Length > 80)
                        s = s.Substring(0, 80);

                    tmp += string.Format("\tSP_{0}_{1} = {0},\r\n", id, s);
                }
                tmp += "\tDUMMY = 0xFFFFFFFE\r\n";
                tmp += "};\r\n";
                File.WriteAllText(sfd.FileName, tmp);
            }

        }

        private string StripEverything(string pInput)
        {
            string output = "";

            foreach (char c in pInput)
            {
                if (c == ' ' || c == '\\' || c == '/' || c == '[' || c == ']' || c == '-') output += '_';
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) output += c;
            }
            return output;
        }

        private void dgvStrings_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
