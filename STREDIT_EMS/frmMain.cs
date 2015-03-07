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
        string STR_Title = "STREDIT - EMS only - CraftNet";

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
                    if (BlockStrings.Min >= 0xFFF0000)
                    {
                        BlockStrings.Min = pAddr;
                    }
                    else if (BlockStrings.Min - pAddr <= 0x100000)
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

                AddPoint();

                // ---------- FIND AOB -------------

                byte[] ExactDataAoB = ByteStringToArray(
                    "D6 DE 75 86 46 64 A3 71  E8 E6 7B D3 33 30 E7 2E" + // Decode Key
                    "10 00 00 00" + // Keylength ^
                    "AA AA AA 00"); // Amount of strings inside client

                DLOG.WriteLine("[TEST] Searching for Key, Key Length and Amount of Strings without any file knowledge.");


                int langpoolstart = -1;
                if (FindAoBInFile(br, 0x001DEEE8, ExactDataAoB, 0xAA, true))
                {
                    DLOG.WriteLine("[INFO] Gotcha! Found @ {0:X8} (FP: {1:X8})", br.BaseStream.Position + FileOffset, br.BaseStream.Position);
                    // DebugBuffer(br);
                    KeyPos = (uint)br.BaseStream.Position;
                    br.ReadBytes(16); // Key
                    _DecodeKeySize = br.ReadInt32(); // Key size lol


                    _StringAmount = br.ReadUInt32();
                    langpoolstart = (int)br.BaseStream.Position;
                    DLOG.WriteLine("[INFO] Lang pools {0:X8} {1:X8}", langpoolstart, langpoolstart + FileOffset);
                }
                else
                {
                    DLOG.WriteLine("[ERROR] Failed!");
                    MessageBox.Show("Are you sure this is EMS?");
                    return;
                }

                // String Pool address
                // 8B 03 89 74 24 28 39 34  A8 0F ? ? 00 00 00 8B 0C AD ? ? ? ?


                byte[] StringDoesExistCheck = ByteStringToArray(
                    "8B 03" +
                    "89 74 24 28" +
                    "39 34 A8" + // Compare
                    "0F AA AA 00 00 00" + // Jump not zero
                    "8B 0C AD AA AA AA AA"); // Move correct address to ECX

                if (FindAoBInFile(br, 0x001DEEE8, StringDoesExistCheck, 0xAA, true))
                {
                    DLOG.WriteLine("[INFO] Found 'Check if string exist' thing at {0:X8} (FP: {1:X8})", br.BaseStream.Position + FileOffset, br.BaseStream.Position);
                    // DebugBuffer(br);
                    br.BaseStream.Position += 2 + 4 + 3 + 6 + 3;

                    StringsPos = br.ReadUInt32();

                    DLOG.WriteLine("[INFO] String pool should be at {0:X8} (FP: {1:X8})", StringsPos, StringsPos - FileOffset);
                }
                else
                {
                    DLOG.WriteLine("[ERROR] Failed!");
                    MessageBox.Show("Are you sure this is EMS?");
                    return;
                }

                DLOG.WriteLine("[DEBUG] Strings in client: {0}", _StringAmount);
                AddPoint();


                br.BaseStream.Position = KeyPos;
                DecodeKey = br.ReadBytes(_DecodeKeySize);
                DLOG.Write("[DEBUG] Decode Key: ");
                foreach (byte b in DecodeKey)
                {
                    DLOG.Write("{0:X2} ", b);
                }
                DLOG.WriteLine();
                AddPoint();


                DataTable dt = new DataTable("FileStrings");
                dt.Columns.Add("ID", typeof(int)).ReadOnly = true;
                dt.Columns.Add("English", typeof(string));
                dt.Columns.Add("French", typeof(string));
                dt.Columns.Add("German", typeof(string));
                dt.Columns.Add("Spanish", typeof(string));
                dt.Columns.Add("Dutch", typeof(string));
                //dt.Columns.Add("Content", typeof(string));
                //dt.Columns.Add("Unicode", typeof(bool)).ReadOnly = true;

                Dictionary<byte, List<int>> languageMap = new Dictionary<byte, List<int>>();


                br.BaseStream.Position = langpoolstart;
                int usedStrings = 0;

                for (int i = 0; i < _StringAmount; i++)
                {
                    int id = br.ReadInt32();
                    if (i > 0 && id == 0)
                    {
                        break;
                    }
                    usedStrings++;
                }

                br.BaseStream.Position = langpoolstart;

                for (byte l = 0; l < 5; l++)
                {
                    DebugBuffer(br);
                    List<int> idlist = new List<int>();
                    for (int i = 0; i < usedStrings; i++)
                    {
                        idlist.Add(br.ReadInt32());
                    }

                    br.ReadInt32();
                    while (idlist[1] != br.ReadInt32()) ;

                    br.BaseStream.Position -= 8;

                    languageMap.Add(l, idlist);
                }

                br.BaseStream.Position = PlainOffsetToFileOffset(StringsPos);
                bool unicode = false;
                for (int i = 0; i < usedStrings; i++)
                {
                    string decoded1 = Decode(br, languageMap[0][i], out unicode);
                    string decoded2 = Decode(br, languageMap[1][i], out unicode);
                    string decoded3 = Decode(br, languageMap[2][i], out unicode);
                    string decoded4 = Decode(br, languageMap[3][i], out unicode);
                    string decoded5 = Decode(br, languageMap[4][i], out unicode);
                    dt.Rows.Add(i, decoded1, decoded2, decoded3, decoded4, decoded5);
                }

                AddPoint();

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
            //DebugBuffer(br);

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
                //ret = Encoding.UTF8.GetString(buffer);
                //ret = Encoding.GetEncoding(8859).GetString(buffer);
                ret = Encoding.GetEncoding(28591).GetString(buffer);
            }

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
            int len = EditingRowValue.Length - ((string)obj.Value).Length;
            bytesfree += len;
            tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var obj = dgvStrings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewTextBoxCell;
            
            EditingRowValue = (string)obj.Value;
            obj.MaxInputLength = EditingRowValue.Length + bytesfree;
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {

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
                barr = Encoding.GetEncoding(949).GetBytes(value);
                //barr = Encoding.Convert(Encoding.GetEncoding(949), Encoding.ASCII, Encoding.GetEncoding(949).GetBytes(value));
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
                len += 1; // Starting encoding thing
                len += ((string)der.ItemArray[1]).Length;
                len += 1; // Ending zero byte
            }
            derpLabel.Text = "Current Size: " + len.ToString("N0") + " bytes";
            derpLabel.AutoToolTip = true;
            derpLabel.ToolTipText = "This is the amount of bytes used\r\nwhen you save the client right\r\nthis moment.";
        }

        private void btn_ForceUse_Click(object sender, EventArgs e)
        {
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
    }
}
