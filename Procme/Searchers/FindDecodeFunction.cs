using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Procme.Searchers
{
    class FindDecodeFunction
    {
        static byte[] _AoB_1 = Extension.ByteStringToArray(
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
             "68 AA AA AA 00 " + // Addr of key
             "FF 75 0C " +
             "E8 AA AA 00 00");


        static byte[] _AoB_2 = Extension.ByteStringToArray(
            "C6 44 24 AA AA" +
            "85 F6" +
            "74 AA" +
            "8B 0C AD AA AA AA AA" + // Addr of key
            "83 C1 01");

        static byte[] _AoB_3 = Extension.ByteStringToArray(
            "33 F6" + // XOR ESI ESI
            "8B 44 24 AA" +
            "50" +
            "6A 10" + // Key Len
            "68 AA AA AA AA" +// Addr of key
            "56");

        public static void Find(ProcessStream pStream, out byte[] pKey, out int pAmountOfStrings, out int pStringArrayListPosition)
        {
            DLOG.WriteLine("SEEKING DECODE FUNCTION");
            int _key_pos = 0, _key_size = 0, _key_size_pos = 0, _strings_amount_pos = 0;
            pAmountOfStrings = pStringArrayListPosition = 0;
            pKey = new byte[0];

            DLOG.Write("[METHOD 1] ");
            long addr = Extension.FindAoB(pStream.pHandle, 0x00200000, _AoB_1);
            if (addr != 0)
            {
                DLOG.WriteLine("Found addr = {0:X8}", addr);
                pStream.Position = addr;

                pStream.Position += 2;

                pStringArrayListPosition = pStream.ReadInt();

                pStream.Position += 0x44 + 2;

                _key_size_pos = pStream.ReadInt();

                pStream.Position += 1;

                _key_pos = pStream.ReadInt();
                _strings_amount_pos = _key_size_pos + 4;
                goto ParseData;
            }

            DLOG.Write("[METHOD 2] ");
            addr = Extension.FindAoB(pStream.pHandle, 0x00200000, _AoB_2);
            if (addr != 0)
            {
                DLOG.WriteLine("Found addr = {0:X8}", addr);
                pStream.Position = addr;

                pStream.Position += 5 + 2 + 2 + 3;

                pStringArrayListPosition = pStream.ReadInt();

                pStream.Position += 0x44 + 2;

                _key_size = pStream.ReadByte(); // Key Size O,o

                pStream.Position += 1;

                _key_pos = pStream.ReadInt();
                _strings_amount_pos = _key_pos + _key_size + 4; // 4 = key size, once again

                pStream.Position = _key_pos;
                DLOG.WriteLine(pStream.ReadBytes(30));

                goto ParseData;
            }

            DLOG.Write("[METHOD 3] ");
            addr = Extension.FindAoB(pStream.pHandle, 0x00200000, _AoB_3);
            if (addr != 0)
            {
                DLOG.WriteLine("Found addr = {0:X8}", addr);
                pStream.Position = addr;

                pStream.Position += 8;

                _key_size = pStream.ReadByte(); // Key Size

                pStream.Position += 1;

                _key_pos = pStream.ReadInt();
                _key_size_pos = _key_pos + _key_size;
                _strings_amount_pos = _key_pos + _key_size + 4; // 4 = key size, once again

                pStringArrayListPosition = pStream.ReadInt();

                pStream.Position += 0x44 + 2;

                goto ParseData;
            }

ParseData:
            DLOG.Write("Gathering data needed...");
            if (_key_size_pos != 0)
            {
                // Read keysize!
                pStream.Position = _key_size_pos;
                _key_size = pStream.ReadInt();
            }
            // Read key
            pStream.Position = _key_pos;
            pKey = pStream.ReadBytes(_key_size);

            // Read amount of strings
            pStream.Position = _strings_amount_pos;
            pAmountOfStrings = pStream.ReadInt();
            DLOG.WriteLine("Done!");
        }
    }
}
