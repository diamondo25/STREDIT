using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace STREDIT_OLDMS
{
    class ResourceDirectory
    {
        public int BaseOffset { get; private set; }
        public int Characteristics { get; private set; }
        public ushort MajorVersion { get; private set; }
        public ushort MinorVersion { get; private set; }
        public ushort AmountONamefEntries { get; private set; }
        public ushort AmountOfIDEntries { get; private set; }
        public Dictionary<int, ResourceDirectoryEntry> IDEntries { get; private set; }

        public void Read(BinaryReader pBR, int pStartOffset)
        {
            IDEntries = new Dictionary<int, ResourceDirectoryEntry>();


            Characteristics = pBR.ReadInt32();
            pBR.ReadInt32();
            MajorVersion = pBR.ReadUInt16();
            MinorVersion = pBR.ReadUInt16();
            AmountONamefEntries = pBR.ReadUInt16();
            AmountOfIDEntries = pBR.ReadUInt16();

            for (int i = 0; i < AmountOfIDEntries; i++)
            {
                ResourceDirectoryEntry rde = new ResourceDirectoryEntry();
                rde.Read(pBR, pStartOffset);

                IDEntries.Add(rde.Name, rde);
            }

            foreach (var kvp in IDEntries)
            {
                pBR.BaseStream.Position = pStartOffset + kvp.Value.OffsetToData;
                kvp.Value.ReadInner(pBR, pStartOffset);
            }
        }
    }

    class ResourceDirectoryEntry
    {
        public int Name { get; private set; }
        public int OffsetToData { get; private set; }
        public bool IsLeaf { get; private set; }
        public object InnerObject { get; private set; }

        public void Read(BinaryReader pBR, int pStartOffset)
        {
            InnerObject = null;

            Name = pBR.ReadInt32();
            uint temp = pBR.ReadUInt32();
            IsLeaf = (temp & 0x80000000) == 0;
            if (!IsLeaf)
            {
                temp -= 0x80000000;
            }
            OffsetToData = (int)temp;
        }

        public void ReadInner(BinaryReader pBR, int pStartOffset)
        {
            pBR.BaseStream.Position = pStartOffset + OffsetToData;
            if (IsLeaf)
            {
                // Final Object
                ResourceEntry re = new ResourceEntry();
                re.Read(pBR, pStartOffset);

                InnerObject = re;
            }
            else
            {
                ResourceDirectory rd = new ResourceDirectory();
                rd.Read(pBR, pStartOffset);

                InnerObject = rd;
            }
        }

        public ResourceEntry GetEntryInfo(int id)
        {
            if (IsLeaf)
            {
                throw new Exception("Is a leaf");
            }

            ResourceDirectory rd = (ResourceDirectory)InnerObject;
            if (!rd.IDEntries.ContainsKey(id)) return null;
            
            ResourceDirectoryEntry rde = rd.IDEntries[id];
            if (rde.IsLeaf)
            {
                throw new Exception("Entry with ID is a leaf!");
            }

            ResourceDirectory rd2 = (ResourceDirectory)rde.InnerObject;
            ResourceDirectoryEntry rde2 = rd2.IDEntries.First().Value;
            if (!rde2.IsLeaf)
            {
                throw new Exception("Entry with ID is NOT a leaf!");
            }


            return rde2.InnerObject as ResourceEntry;
        }
    }

    class ResourceEntry
    {
        public int OffsetToData { get; private set; }
        public int Size { get; private set; }
        public int CodePage { get; private set; }
        public int Reserved { get; private set; }

        public void Read(BinaryReader pBR, int pStartOffset)
        {
            OffsetToData = pBR.ReadInt32();
            Size = pBR.ReadInt32();
            CodePage = pBR.ReadInt32();
            Reserved = pBR.ReadInt32();
            
        }
    }
}
