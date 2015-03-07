using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;

namespace STREDIT
{
    public partial class Form1 : Form
    {

        static uint StringsPos = 0xA6F17C;
        static uint KeyPos = 0x9E30C4;
        static uint KeySizePos = 0x9E30D4;
        static uint StringsAmountPos = 0x9E30D8;
        static uint FileOffset = 0x400000;
        static byte[] DecodeKey;
        static uint _StringAmount = 0;
        static int _DecodeKeySize = 0;
        static int smallcode = 0;
        static SortedDictionary<int, int> dataz = new SortedDictionary<int, int>();
        static bool CalculateBlockSize = true;
        static int BlockSize = 0;
        static int StartPosFromBlock = int.MaxValue;
        static int EndPosFromBlock = 0;

        static byte[] StringsListStart = new byte[] {
            0x25, 0x64, 0x25, 0x30, 0x32, 0x64, 0x25, 0x30, 0x32, 0x64, 0x25, 0x30, 0x32, 0x64, 0x25, 0x30,
            0x32, 0x64, 0x25, 0x30, 0x32, 0x64, 0x2E, 0x64, 0x6D, 0x70, 0x00, 0x00
        }; // %d%02d%02d%02d%02d%02d.dmp | Next int = start from list

        static byte[] DecodeFunctionCall = new byte[] { };
        static byte[] MakeVersionString = new byte[] { };

        string EditingRowValue = "";

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void LoadFile(string pFilename)
        {
            DecodeFunctionCall = ByteStringToArray(
                 "8B 86 AA AA AA 00 0F BE  00 6A 04 B9 AA AA AA 00" + // Addr of stringpos
                 "89 45 F0 E8 EB AA AA AA  8B C8 89 4D 0C 85 C9 C6" +
                 "45 FC 01 74 1A 8B 86 AA  AA AA 00 83 21 00 40 6A" + // Addr of stringpos
                 "FF 50 E8 C4 AA AA AA AA  45 0C 89 45 0C EB 04 83" +
                 "65 0C 00 FF 75 F0 80 65  FC 00 FF 35 AA AA AA 00" + // Addr of keysize
                 "68 AA AA AA 00 FF 75 0C  E8 AA AA 00 00"); // Addr of key
            MakeVersionString = ByteStringToArray(
                "C6 45 FC 0E E8 AA AA AA  AA 8B C8 E8 AA AA AA AA" +
                "8B 00 6A AA AA");
            BinaryReader br = new BinaryReader(File.OpenRead(pFilename));
            /*
            br.BaseStream.Position = 0x00A6E160 - FileOffset;

            if (!FindAoBInFile(br, 0x00A6E160 - FileOffset, StringsListStart))
            {
                Console.WriteLine("Could not find strings...");
            }
            else
            {
                StringsPos = (uint)(br.BaseStream.Position + StringsListStart.Length);
                Console.WriteLine("StringPool::GetString is around 0x{0:X8}", StringsPos + FileOffset);
            }
            */
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Maximum = 5;
                tsLoadProgress.Value = 0;
            });
            if (!FindAoBInFile(br, 0x006074F3 - FileOffset, DecodeFunctionCall, 0xAA, true))
            {
                Console.WriteLine("Could not find Decode Function Call");
            }
            else
            {
                Console.WriteLine("StringPool::GetString is around 0x{0:X8}", br.BaseStream.Position + FileOffset);
                br.BaseStream.Position += 2;
                Console.WriteLine("Current StringPool::ms_aString: 0x{0:X8}", StringsPos);
                StringsPos = br.ReadUInt32();
                Console.WriteLine("New StringPool::ms_aString: 0x{0:X8}", StringsPos);

                Console.WriteLine("Old Key Size Pos 0x{0:X8} Old Key Pos 0x{0:X8}", KeySizePos, KeyPos);

                br.BaseStream.Position += 0x44 + 2;
                KeySizePos = br.ReadUInt32();
                br.BaseStream.Position++;
                KeyPos = br.ReadUInt32();
                StringsAmountPos = KeySizePos + 4;
                Console.WriteLine("New Key Size Pos 0x{0:X8} New Key Pos 0x{0:X8}", KeySizePos, KeyPos);
            }
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Value++;
            });
            if (!FindAoBInFile(br, 0x005B206E - FileOffset, MakeVersionString, 0xAA, true))
            {
                Console.WriteLine("Could not find Make Version String function....");
            }
            else
            {
                br.BaseStream.Position += 4;
                Console.WriteLine("StringPool::GetInstance() function should begin @ {0:X8}", CalculateAddressLocation(br));
                br.BaseStream.Position += 2;
                uint getstringfuncaddr = CalculateAddressLocation(br);
                Console.WriteLine("StringPool::GetString(int, int) function should begin @ {0:X8}", getstringfuncaddr);
                br.BaseStream.Position += 2 + 1;
                byte subver = br.ReadByte();
                byte version = 0;
                if (br.ReadByte() == 0x6A)
                { // Normal byte push.
                    version = br.ReadByte();
                }
                Console.WriteLine(">> Working with V{0}.{1}", version, subver);

                this.Invoke((MethodInvoker)delegate
                {
                    tsClientVersion.Text = string.Format("Ver. {0}.{1}", version, subver);
                });
            }
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Value++;
            });

            br.BaseStream.Position = PlainOffsetToFileOffset(StringsAmountPos);
            _StringAmount = br.ReadUInt32();
            Console.WriteLine("Strings in client: {0}", _StringAmount);
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Value++;
            });

            br.BaseStream.Position = PlainOffsetToFileOffset(KeySizePos);
            int keysize = br.ReadInt32();
            _DecodeKeySize = keysize;
            Console.WriteLine("Decode Key Size: {0} bytes", keysize);
            br.BaseStream.Position = PlainOffsetToFileOffset(KeyPos);
            DecodeKey = br.ReadBytes(keysize);
            Console.Write("Decode Key: ");
            foreach (byte b in DecodeKey)
            {
                Console.Write("{0:X2} ", b);
            }
            Console.WriteLine();
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Value++;
            });

            br.BaseStream.Position = PlainOffsetToFileOffset(StringsPos);

            DataTable dt = new DataTable("FileStrings");
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("Content", typeof(string));
            dt.Columns[0].ReadOnly = true;
            dt.Columns[1].AllowDBNull = false;

            // Read first one
            uint pos = br.ReadUInt32();
            this.Invoke((MethodInvoker)delegate
            {
                tsLoadProgress.Maximum = (int)_StringAmount;
                tsLoadProgress.Value = 0;
            });
            for (int i = 0; i < _StringAmount; i++)
            {
                dt.Rows.Add(i, Decode(br, i));
                this.Invoke((MethodInvoker)delegate
                {
                    tsLoadProgress.Value++;
                });
            }

            this.Invoke((MethodInvoker)delegate
            {
                dataGridView1.DataSource = dt;
                dataGridView1.Refresh();
            });

            Console.WriteLine("Calculating Block Size...");
            BlockSize = EndPosFromBlock - StartPosFromBlock;
            Console.WriteLine("Block Size: {0} bytes", BlockSize);
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
            return ret.ToArray();
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
        static uint CalculateAddressLocation(BinaryReader br)
        {
            Instructions type = (Instructions)br.ReadByte();
            if (type == Instructions.Call ||
                type == Instructions.JumpLong) // Call
            {
                int pos = br.ReadInt32();
                return (uint)(br.BaseStream.Position + pos + FileOffset);
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
                return (uint)(br.BaseStream.Position + pos + FileOffset);
            }
            else
            {
                throw new Exception("Could not find type of jump or call.");
            }
        }

        static bool FindAoBInFile(BinaryReader br, uint StartPos, byte[] AoB, byte skipbyte = 0xFF, bool skipbyteset = false)
        {
            br.BaseStream.Position = StartPos;
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                long cpos = br.BaseStream.Position;
                bool found = true;
                for (int i = 0; i < AoB.Length; i++)
                {
                    byte b = br.ReadByte();
                    if (skipbyteset && AoB[i] == skipbyte) continue;
                    if (b != AoB[i])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    br.BaseStream.Position = cpos;
                    Console.WriteLine("Found thing @ 0x{0:X8}", cpos + FileOffset);
                    return true;
                }
                else
                {
                    br.BaseStream.Position = ++cpos;
                }
            }
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

        static string Decode(BinaryReader br, int stringPos)
        {
            int ttt = stringPos;
            stringPos = (int)PlainOffsetToFileOffset(CalculateStringPosition(stringPos));
            if (stringPos + FileOffset < StartPosFromBlock)
            {
                StartPosFromBlock = (int)(stringPos + FileOffset);
            }
            br.BaseStream.Position = stringPos;

            int v = br.ReadInt32();
            br.BaseStream.Position = v - FileOffset;

            byte[] lulzkey = rotatel(DecodeKey, (uint)_DecodeKeySize, br.ReadSByte());

            List<byte> encryptedStringBuffer = new List<byte>();
            while (true)
            {
                byte ch = br.ReadByte();
                if (ch == 0) break;
                encryptedStringBuffer.Add(ch);
            }

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
            int l = (int)br.BaseStream.Position + (int)FileOffset;
            if (l > EndPosFromBlock)
            {
                EndPosFromBlock = l;
            }
            if (dataz.ContainsKey(l))
            {
                Console.WriteLine("DUPLICATED: {0} and {1}", dataz[l], ttt);
            }
            else
            {
                dataz.Add((int)br.BaseStream.Position + (int)FileOffset, ttt);
            }
            return ret;
        }

        static byte[] rotatel(byte[] value, uint length, int shift)
        {
            byte[] v4 = new byte[length];
            Buffer.BlockCopy(value, 0, v4, 0, (int)length);
            if ((uint)shift < 8)
            {
                smallcode = 1;
                goto label_26;
            }
            smallcode = 0;
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
            string newtext = (string)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            int len = EditingRowValue.Length - newtext.Length;
            if (len > 0)
            {
                bytesfree += len;
            }
            else if (len < 0)
            {
                if (bytesfree + len < 0)
                {
                    if (MessageBox.Show("It seems you've written more than there's free. Press OK to remove the characters on your own, or Cancel to revert your changes made.", "Oh noes", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Cancel)
                    {
                        dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = EditingRowValue;
                    }
                    else
                    {
                        dataGridView1.BeginEdit(true);
                    }
                    return;
                }
            }
            tsBytesAvailable.Text = bytesfree.ToString();
        }

        private void dataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            EditingRowValue = (string)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = openedFile;
            sfd.Filter = "EXE|*.exe";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string tmpfile = openedFile + ".tmp";
                File.Copy(openedFile, tmpfile);
                using (BinaryWriter br = new BinaryWriter(File.Open(tmpfile, FileMode.Open, FileAccess.Write, FileShare.None))) {
                    br.BaseStream.Position = PlainOffsetToFileOffset(StringsPos);
                    int startoffset = (int)(br.BaseStream.Position + (_StringAmount * 4));
                    

                }
            }
        }
    }
}
