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
using STREDIT_OLDMS;

namespace STREDIT
{
    public partial class frmMain : Form
    {
        static uint FileAlignment = 0;
        static uint FileOffset = 0;
        List<string> LastFiles = new List<string>();


        string STR_DataSize = "{1:N0} Bytes (Pool: {0:N0} Bytes)";
        string STR_BytesAvailable = "{0:N0} Bytes";
        string STR_Title = "STREDIT - Old versions only - CraftNet";

        int _resourceTableLoc = 0;
        int _resourceDataLoc = 0;
        int _stringDataLoc = 0;
        int _stringDataEndLoc = 0;
        int _resourceTableBlockEnd = 0;
        int _strings_start_pos = 0;
        int _strings_end_pos = 0;
        int bytesfree = 0;

        bool multiLanguages = false;

        System.Threading.Thread loadThread = null;

        string EditingRowValue = "";

        public frmMain()
        {
            InitializeComponent();
            this.Text = STR_Title;

            this.tsBytesAvailable.Text = string.Format(STR_BytesAvailable, 0);
            this.tsBlockSize.Text = string.Format(STR_DataSize, 0, 0);

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
            try
            {
        
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
        
                DLOG.WriteLine("---------------Loading File------------------");
                DLOG.WriteLine("FileName: {0}", pFilename);
                DLOG.WriteLine("---------------------------------------------");
                GC.Collect();

                openedFile = pFilename;

        

                MemoryStream memstream = new MemoryStream(File.ReadAllBytes(pFilename));
                BinaryReader br = new BinaryReader(memstream);

                // DOS header
                {
                    // Identify MS-DOS EXE
                    if (br.ReadChar() != 'M' || br.ReadChar() != 'Z')
                    {
                        MessageBox.Show("Not a valid executable! 1");
                        return;
                    }

                    // Go to offset 0x3C, where the position is of the NT header
                    br.BaseStream.Position = 0x3C;
                    int NTheaderPos = br.ReadInt32();
                    DLOG.WriteLine("NT Header offset: {0:X8}", NTheaderPos);

                    br.BaseStream.Position = NTheaderPos;

                }

                AddPoint();
                int sections = 0;
                // NT header
                {
                    // Check for PE\0\0
                    if (br.ReadChar() != 'P' || br.ReadChar() != 'E' || br.ReadByte() != 0 || br.ReadByte() != 0)
                    {
                        MessageBox.Show("Not a valid executable! 2");
                        return;
                    }

                    // File info

                    ushort fh_machine = br.ReadUInt16();
                    sections = br.ReadUInt16();
                    int fh_datestamp = br.ReadInt32();
                    br.ReadInt32(); // Pointer To Symbol Table
                    br.ReadInt32(); // Number Of Symbols
                    ushort fh_optional_header_size = br.ReadUInt16(); // SizeOfOptionalHeaders
                    br.ReadUInt16(); // Characteristics

                    if (fh_optional_header_size == 0)
                    {
                        MessageBox.Show("Not a valid executable! 3");
                        return;
                    }

                    // Optional headers
                    {
                        {
                            if (br.ReadUInt16() != 0x10B)
                            {
                                MessageBox.Show("Not a valid executable! 4");
                                return;
                            }

                            DLOG.WriteLine("Compiled with somthing version {0}.{1}", br.ReadByte(), br.ReadByte());

                            br.ReadInt32(); // SizeOfCode;
                            br.ReadInt32(); // SizeOfInitializedData
                            br.ReadInt32(); // SizeOfUninitializedData
                            br.ReadInt32(); // AddressOfEntryPoint
                            br.ReadInt32(); // BaseOfCode
                            br.ReadInt32(); // BaseOfData
                        }

                        // Optional Header Windows NT
                        {
                            FileAlignment = br.ReadUInt32(); // ImageBase
                            br.ReadInt32(); // SectionAlignment
                            FileAlignment += br.ReadUInt32(); // FileAlignment
                            FileOffset = FileAlignment;

                            DLOG.WriteLine("FileAlignment is now {0:X8}", FileAlignment);

                            DLOG.WriteLine("> {0:X8}", br.BaseStream.Position);
                            br.ReadUInt16(); // Major OS Version
                            br.ReadUInt16(); // Minor OS Version
                            br.ReadUInt16(); // Major Image Version
                            br.ReadUInt16(); // Minor Image Version
                            br.ReadUInt16(); // Major SubSystem Version
                            br.ReadUInt16(); // Minor SubSystem version
                            br.ReadInt32(); // Reserved
                            br.ReadInt32(); // Size Of Image
                            br.ReadInt32(); // Size of Headers

                            DLOG.WriteLine("> {0:X8}", br.BaseStream.Position);
                            br.ReadInt32(); // Checksum
                            br.ReadInt16(); // Subsystem
                            br.ReadInt16(); // DLL 

                            br.ReadInt32(); // SizeOfStackReserver
                            br.ReadInt32(); // SizeOfStackCommit
                            br.ReadInt32(); // SizeOfHeapReserve
                            br.ReadInt32(); // SizeOfHeapCommit
                            br.ReadInt32(); // LoaderFlag
                            br.ReadInt32(); // NumberOfRVAAndSizes

                            DLOG.WriteLine("> {0:X8}", br.BaseStream.Position);
                        }
                    }
                }

                AddPoint();



                {
                    br.ReadInt64(); // Export table position and size
                    br.ReadInt64(); // Import table position and size
                    DLOG.WriteLine("> {0:X8}", br.BaseStream.Position);
                    _resourceTableLoc = br.ReadInt32();
                     _resourceTableBlockEnd = _resourceTableLoc + br.ReadInt32();

                    // br.ReadInt64(); // Resource table position and size
                    br.ReadInt64(); // Exception table position and size
                    br.ReadInt64(); // Certificate table position and size
                    br.ReadInt64(); // Base Relocation table position and size
                    br.ReadInt64(); // Debug table position and size
                    br.ReadInt64(); // Architecture table position and size
                    br.ReadInt64(); // Global Pointer table position and size
                    br.ReadInt64(); // TLS table position and size
                    br.ReadInt64(); // Load Config table position and size
                    br.ReadInt64(); // Bound Import table position and size
                    br.ReadInt64(); // IAT table position and size
                    br.ReadInt64(); // Delay Import Descriptor table position and size
                    br.ReadInt64(); // Com+ Runtime Header table position and size
                    br.ReadInt64(); // Reserved
                }


                AddPoint();

                bool foundResources = false;
                if (true)
                {
                    // Read Sections
                    for (int i = 0; i < sections; i++)
                    {
                        DLOG.WriteLine("> {0:X8}", br.BaseStream.Position);

                        string name = "";
                        char[] namebuff = br.ReadChars(8);
                        foreach (char c in namebuff)
                            if (c == (char)0)
                                break;
                            else
                                name += c;
                        br.ReadInt32(); // Vsize
                        int vloc = br.ReadInt32(); // Vloc

                        br.ReadInt32();
                        int loc = br.ReadInt32();

                        br.ReadInt32(); // Pointer to Relocations
                        br.ReadInt32(); // Pointer to Line numbers
                        br.ReadInt16(); // Number of relocations
                        br.ReadInt16(); // Number of line numbers
                        br.ReadInt32(); // characteristics

                        DLOG.WriteLine("Section {0} is at {1:X8}", name, loc);
                        if (name == ".rsrc")
                        {
                            DLOG.WriteLine("Resource found. Dataloc: {0:8}", _resourceDataLoc);
                            _resourceTableLoc = loc;
                            _resourceDataLoc = vloc - loc;
                            foundResources = true;
                        }
                    }
                }
                if (!foundResources)
                {
                    MessageBox.Show("Couldn't find resource section.. :(");
                    return;
                }


                AddPoint();

                STREDIT_OLDMS.ResourceDirectory rd = new STREDIT_OLDMS.ResourceDirectory();
                {
                    // Go to resource section
                    br.BaseStream.Position = _resourceTableLoc;
                    rd.Read(br, _resourceTableLoc);
                }

                AddPoint();

                // Search for CStringDecoder::DelayedLoad

                int resource_type = 0;
                int resource_id = 0;
                {
                    byte[] aob = ByteStringToArray(
                        "6A 00", // Push 0
                        "FF 15  AA AA AA 00", // Call LoadResource
                        "89 45 F4", // Move AEX to local value
                        "6A  AA", // Push resource type
                        "68  AA AA 00 00", // Push resource ID
                        "8B 4D F4", // Move local value to ECX
                        "51", // Push ECX
                        "FF 15  AA AA AA 00", // Call FindResource
                        "");

                    if (FindAoBInFile(br, 0, aob, 0xAA, true))
                    {
                        // oh yiss
                        br.BaseStream.Position += 2 + 2 + 4 + 3 + 1;
                        resource_type = br.ReadByte();
                        br.BaseStream.Position += 1;
                        resource_id = br.ReadInt32();
                    }
                    else
                    {
                        aob = ByteStringToArray(
                            "6A 00", // Push 0
                            "FF 15  AA AA AA 00", // Call LoadResource
                            "6A  AA", // Push resource type
                            "8B F8", // Move EAX to EDI
                            "68  AA AA 00 00", // Push resource ID
                            "57", // Push EDI
                            "FF 15  AA AA AA 00", // Call FindResource
                            "");
                        if (FindAoBInFile(br, 0, aob, 0xAA, true))
                        {
                            // oh yiss
                            br.BaseStream.Position += 2 + 2 + 4 + 1;
                            resource_type = br.ReadByte();
                            br.BaseStream.Position += 2 + 1;
                            resource_id = br.ReadInt32();
                        }
                        else
                        {
                            aob = ByteStringToArray(
                            "6A  AA", // Push resource type
                            "68  AA AA 00 00", // Push resource ID
                            "6A  00", // Push 0
                            "FF 15  AA AA AA 00", // Call FindResource
                            "");
                            if (FindAoBInFile(br, 0, aob, 0xAA, true))
                            {
                                // oh yiss
                                br.BaseStream.Position += 1;
                                resource_type = br.ReadByte();
                                br.BaseStream.Position += 1;
                                resource_id = br.ReadInt32();
                            }
                            else
                            {
                                MessageBox.Show("Could not find CStringDecoder::DelayedLoad");
                                return;
                            }
                        }
                    }
                }

                AddPoint();
                DLOG.WriteLine("Resource Type = {0}", resource_type);
                DLOG.WriteLine("Resource ID = {0}", resource_id);
                STREDIT_OLDMS.ResourceEntry re = rd.IDEntries[resource_type].GetEntryInfo(resource_id);
                int datapos = 0;
                {
                    if (re == null)
                    {
                        MessageBox.Show("Could not find resource data.");
                        return;
                    }

                    datapos = re.OffsetToData;
                    _stringDataLoc = datapos - _resourceDataLoc;
                    _stringDataEndLoc = _stringDataLoc + re.Size;
                    DLOG.WriteLine("Resource Data Loc: {0:X8} - {1:X8} = {2:X8}", _stringDataLoc, _stringDataEndLoc, _stringDataEndLoc - _stringDataLoc);
                }

                DataTable dt = new DataTable("FileStrings");

                int languages = 1;
                int amount = GetValueAtIndex(br, 0);
                if (amount < 20)
                {
                    DLOG.WriteLine("Guessing multi-language client, such as EMS. {0}", amount);
                    languages = amount;
                    amount = GetValueAtIndex(br, 4);

                    int stringsOffset = amount * languages;
                    stringsOffset *= 2;
                    stringsOffset = 8 + (stringsOffset * 4);

                    DLOG.WriteLine("Amount of strings: {0}", amount);
                    DLOG.WriteLine("Offset {0}", stringsOffset);

                    dt.Columns.Add("ID", typeof(int)).ReadOnly = true;
                    for (int i = 0; i < languages; i++)
                        dt.Columns.Add("Content" + (i + 1), typeof(string)).ReadOnly = true;

                    int offset = 8;

                    object[][] rows = new object[amount][];
                    for (int i = 0; i < amount; i++)
                        rows[i] = new object[1 + languages];

                    for (int lang = 0; lang < languages; lang++)
                    {
                        for (int id = 0; id < amount; id++)
                        {
                            int stringPos = GetValueAtIndex(br, offset + 0);
                            int stringLength = GetValueAtIndex(br, offset + 4);

                            var str = DecodeEMS(br, _stringDataLoc + stringsOffset + stringPos, stringPos, stringLength);
                            rows[id][1 + lang] = str;

                            offset += 8;

                        }
                    }


                    Console.WriteLine("Offset: {0:x8}", offset);

                    for (int i = 0; i < amount; i++)
                    {
                        rows[i][0] = i;

                        var dr = dt.NewRow();
                        dr.ItemArray = rows[i];
                        dt.Rows.Add(dr);
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        tsBlockSize.Text = string.Format(STR_DataSize, (amount + 1) * 8, _resourceTableBlockEnd - _resourceTableLoc);
                    });
                }
                else
                {
                    DLOG.WriteLine("Amount of strings: {0}", amount);

                    dt.Columns.Add("ID", typeof(int)).ReadOnly = true;

                    dt.Columns.Add("Content", typeof(string));
                    dt.Columns.Add("Is Bstr", typeof(bool)).ReadOnly = true;


                    

                    _strings_start_pos = 0x10000000;
                    _strings_end_pos = 0;
                    int _indexes_start_pos = 0x10000000;
                    int _indexes_end_pos = 0x0;
                    for (int i = 1; i <= amount; i++)
                    {
                        int loc = GetValueAtIndex(br, i * 4);
                        if (loc == 0) continue;
                        loc = _stringDataLoc + (i * 4);
                        if (_indexes_start_pos > loc) _indexes_start_pos = loc;
                        if (_indexes_end_pos < loc) _indexes_end_pos = loc;


                        bool bstr = false;
                        int start = 0, end = 0;
                        string text = Decode(br, i, out bstr, out start, out end);
                        if (text == null) continue;

                        if (_strings_start_pos > start)
                            _strings_start_pos = start;
                        if (_strings_end_pos < end)
                            _strings_end_pos = end;

                        dt.Rows.Add(i, text, bstr);
                    }

                    bytesfree = _stringDataEndLoc - _strings_end_pos;

                    int real_block_len = _resourceTableBlockEnd - _resourceTableLoc;
                    DLOG.WriteLine("Resource size: {0}", real_block_len);
                    DLOG.WriteLine("Index List size: {0}", amount * 4);
                    DLOG.WriteLine("Index List size 2: {0}", _indexes_end_pos - _indexes_start_pos);
                    DLOG.WriteLine("String Pool size: {0}", _strings_end_pos - _strings_start_pos);
                    this.Invoke((MethodInvoker)delegate
                    {
                        tsBlockSize.Text = string.Format(STR_DataSize, amount * 4, _strings_end_pos - _strings_start_pos);
                    });

                    List<Tuple<int, string>> lines = new List<Tuple<int, string>>();
                    foreach (DataRow row in dt.Rows) lines.Add(new Tuple<int, string>(row.Field<int>(0), row.Field<string>(1)));

                    lines.Sort((a, b) =>
                    {
                        return b.Item2.Length - a.Item2.Length;
                    });

                    DLOG.WriteLine("Longest line: {0} == {2} long ({1})", lines[0].Item1, lines[0].Item2, lines[0].Item2.Length);
                    DLOG.WriteLine("Second Longest line: {0} == {2} long ({1})", lines[1].Item1, lines[1].Item2, lines[1].Item2.Length);
                    DLOG.WriteLine("Third Longest line: {0} == {2} long ({1})", lines[2].Item1, lines[2].Item2, lines[2].Item2.Length);
                }

                multiLanguages = languages > 1;

                this.Invoke((MethodInvoker)delegate
                {
                    dgvStrings.DataSource = dt;
                    //dgvStrings.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    // dgvStrings.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                    tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
                    this.Text = STR_Title + " - File: " + pFilename;
                    saveToolStripMenuItem1.Enabled = !multiLanguages;
                    cSVToolStripMenuItem1.Enabled = !multiLanguages;
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

        int GetValueAtIndex(BinaryReader pBR, int pIndex)
        {
            long curpos = pBR.BaseStream.Position;

            pBR.BaseStream.Position = _stringDataLoc + pIndex;
            int val = pBR.ReadInt32();

            pBR.BaseStream.Position = curpos;
            return val;
        }

        string DecodeEMS(BinaryReader pBR, int pResourceLocation, int pLocation, int pSize)
        {
            pBR.BaseStream.Position = pResourceLocation;
            byte[] characters = new byte[pSize];
            for (int i = 0; i < pSize; i++)
            {
                int location = pLocation + i;

                byte b = pBR.ReadByte();
                int shifter = (8 - location % 8);
                int firstPart = (b << shifter);
                int secondPart = ((ushort)(b << shifter) >> 8);
                int result = (byte)(firstPart | secondPart) ^ 0xAA;
                characters[i] = (byte)result;
            }

            bool isbstr = false;

            for (int j = 0; j < pSize; j++)
            {
                if (characters[j] > sbyte.MaxValue)
                {
                    isbstr = true;
                    break;
                }
            }

            string text = "";
            try
            {
                if (isbstr)
                    text = Encoding.GetEncoding(28591).GetString(characters);
                else
                    text = ASCIIEncoding.ASCII.GetString(characters);
            }
            catch (Exception)
            {
                text = "INVALID";
            }

            return text;
        }

        string Decode(BinaryReader pBR, int pIndex, out bool pIsBstr, out int pStartLoc, out int pEndLoc)
        {

            pIsBstr = false;
            pStartLoc = 0;
            pEndLoc = 0;
            int pos = GetValueAtIndex(pBR, 4 * pIndex);
            if (pos == 0)
            {
                return null;
            }
            //DLOG.WriteLine("Pos offset at {0:X8}", (4 * pIndex) + _stringDataLoc);

            // [INT, len] [BYTE(len), xor1] [BYTE(len), xor2] [INT, checksum]


            //DLOG.WriteLine("Len at {0:X8}", _stringDataLoc + pos);
            int length = GetValueAtIndex(pBR, pos);

            pStartLoc = _stringDataLoc + pos; // Start @ Length int
            pEndLoc = _stringDataLoc + 2 * length + 4 + pos + 4; // End @ new string

            int stringdata = _stringDataLoc + pos + 4 + 4;

            uint ucs = (uint)GetValueAtIndex(pBR, 2 * length + 4 + pos);
            //DLOG.WriteLine("{0:X8} checksum pos", _stringDataLoc + 2 * length + 4 + pos);

            byte[] stringbuf1 = new byte[length];
            byte[] stringbuf2 = new byte[length];
            byte[] outputbuf = new byte[length];

            int offset1 = pos + _stringDataLoc + 4;
            int offset2 = pos + length + _stringDataLoc + 4;

            for (int j = 0; j < length; j++)
            {
                pBR.BaseStream.Position = offset1 + j;
                byte tmp = pBR.ReadByte();
                stringbuf1[j] = tmp;

                pBR.BaseStream.Position = offset2 + j;
                tmp = pBR.ReadByte();
                stringbuf2[j] = tmp;
            }

            uint ucs2 = 0xBAADF00D;
            int offset = 0;
            for (int j = 0; j < (length >> 2); j++)
            {
                uint x = BitConverter.ToUInt32(stringbuf1, offset);
                uint y = BitConverter.ToUInt32(stringbuf2, offset);
                uint tmp = (y).RollLeft(5);
                uint val = tmp ^ x;
                outputbuf[offset] = (byte)((val >> 0) & 0xFF);
                outputbuf[offset + 1] = (byte)((val >> 8) & 0xFF);
                outputbuf[offset + 2] = (byte)((val >> 16) & 0xFF);
                outputbuf[offset + 3] = (byte)((val >> 24) & 0xFF);
                ucs2 = y + (x ^ ucs2).RollRight(5);
                offset += 4;
            }

            int ubegin = 4 * (length >> 2);
            int uremained = length - ubegin;

            for (int j = 0; j < uremained; j++)
            {
                int loc = ubegin + j;
                byte x = stringbuf1[loc];
                byte y = stringbuf2[loc];
                outputbuf[loc] = (byte)(x ^ y); // 0?
                ucs2 = y + (x ^ ucs2).RollRight(5);
            }

            if (pIndex == 1)
            {
                DLOG.WriteLine("Idx {0} checksum {1:X8} calculated {2:X8}", pIndex, ucs, ucs2);
            }



            bool isbstr = false;

            for (int j = 0; j < length; j++)
            {
                if (outputbuf[j] > sbyte.MaxValue)
                {
                    isbstr = true;
                    break;
                }
            }

            string text = "";
            try
            {
                if (isbstr)
                    text = Encoding.GetEncoding(949).GetString(outputbuf);
                else
                    text = ASCIIEncoding.ASCII.GetString(outputbuf);
            }
            catch (Exception)
            {
                text = "INVALID";
            }

            if (ucs != ucs2)
            {
                DLOG.WriteLine("INVALID CHECKSUM for id {0} {1:X8} != {2:X8}. Result: {3}", pIndex, ucs, ucs2, text);
                return null;
            }

            pIsBstr = isbstr;

            return text;
        }

        void WriteString(BinaryWriter pBW, string pString, int pIndex, ref int pOffset)
        {
            int index_offset = _stringDataLoc + (pIndex * 4);
            pBW.BaseStream.Position = index_offset;
            pBW.Write((int)pOffset - _stringDataLoc);

            pBW.BaseStream.Position = pOffset;



            byte[] barr;
            if (CheckIfNonWestern(pString))
                barr = Encoding.GetEncoding(949).GetBytes(pString);
            else
                barr = Encoding.ASCII.GetBytes(pString);
            int length = barr.Length;

            pBW.Write((int)length);
            
            // Build Xor's
            // First one can be zero
            byte[] key = new byte[length];
            // new Random().NextBytes(key);
            byte[] xor1 = new byte[length];
            byte[] xor2 = new byte[length];

            uint checksum = 0xBAADF00D;
            int j = 0;
            int offset = 0;
            for (; j < (length >> 2); j++)
            {
                uint x = BitConverter.ToUInt32(barr, offset);
                uint y = BitConverter.ToUInt32(key, offset);


                Buffer.BlockCopy(BitConverter.GetBytes(x), 0, xor1, offset, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(y), 0, xor2, offset, 4);

                checksum = y + (x ^ checksum).RollRight(5);
                offset += 4;
            }

            j = offset;
            for (; j < length; j++)
            {
                byte x = (byte)barr[j];
                byte y = (byte)key[j];

                xor1[j] = x;
                xor2[j] = y;

                checksum = y + (x ^ checksum).RollRight(5);
            }

            pBW.Write(xor1);
            pBW.Write(xor2);

            pBW.Write((uint)checksum);

            pOffset = (int)pBW.BaseStream.Position;
        }

        static byte[] ByteStringToArray(params string[] pInput)
        {
            string input = string.Join(" ", pInput);
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

        static bool CheckIfNonWestern(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > sbyte.MaxValue) return true;
            }
            return false;
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
                        DLOG.WriteLine("[FindAOB] Found at {0:X8} ({1:X8})", realpos, realpos + FileOffset);
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

                        using (MemoryStream mems = new MemoryStream(File.ReadAllBytes(tmpfile)))
                        {
                            using (BinaryWriter br = new BinaryWriter(mems))
                            {
                                File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Writing strings to client\r\n");

                                int startoffset = _strings_start_pos;
                                Random rnd = new Random();
                                foreach (DataRow dr in ((DataTable)dgvStrings.DataSource).Rows)
                                {
                                    int id = (int)dr.ItemArray[0];
                                    string s = (string)dr.ItemArray[1];
                                    WriteString(br, s, id, ref startoffset);
                                }

                                if (br.BaseStream.Position == _strings_end_pos)
                                {
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Block is not changed or same length!" + "\r\n");
                                }
                                else
                                {
                                    // Fill left over data with 0xFF's lol
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Adding ending byte at " + br.BaseStream.Position.ToString("X8") + "\r\n");
                                    br.Write((byte)0xFE); // Start byte :)
                                    File.AppendAllText(Program.DATAFOLDER + "Saving File Log.txt", "Filling " + (_stringDataEndLoc - br.BaseStream.Position).ToString("X8") + " leftover bytes." + "\r\n");
                                    while (br.BaseStream.Position < _stringDataEndLoc)
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
                                    bytesfree += l * 2;
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
                                tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
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

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // check howmany chars edited
            var obj = dgvStrings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewTextBoxCell;
            if (obj.Value is DBNull)
            {
                obj.Value = "";
            }
            int len = EditingRowValue.Length - ((string)obj.Value).Length;
            bytesfree += len * 2;
            tsBytesAvailable.Text = string.Format(STR_BytesAvailable, bytesfree);
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var obj = dgvStrings.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewTextBoxCell;

            EditingRowValue = (string)obj.Value;
            obj.MaxInputLength = EditingRowValue.Length + bytesfree;
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
            if (multiLanguages)
            {
                MessageBox.Show("I cannot let you do that. Too complex.");
                return;
            }
            DataTable dt = dgvStrings.DataSource as DataTable;
            long len = 0;
            foreach (DataRow der in dt.Rows)
            {
                len += 4; // Length
                string str = ((string)der.ItemArray[1]);
                byte[] barr;
                if (CheckIfNonWestern(str))
                {
                    barr = Encoding.GetEncoding(949).GetBytes(str);
                }
                else
                {
                    barr = Encoding.ASCII.GetBytes(str);
                }

                len += barr.Length * 2; // !!

                len += 4; // Checksum
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

        private void dgvStrings_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
