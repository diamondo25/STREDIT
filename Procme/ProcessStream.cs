using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Procme
{
    public class ProcessStream : Memory.MLProc
    {
        public long Position { get; set; }

        public ProcessStream(IntPtr handle) : base(handle) { Position = 0; }

        public bool ReadMemory(ref byte[] buffer, int size)
        {
            bool v = Memory.ReadProcessMemory(pHandle, (UIntPtr)Position, buffer, (UIntPtr)size, IntPtr.Zero);
            Position += buffer.Length;
            return v;
        }

        public bool ReadMemory(ref byte[] buffer)
        {
            bool v = Memory.ReadProcessMemory(pHandle, (UIntPtr)Position, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
            Position += buffer.Length;
            return v;
        }

        public byte[] ReadBytes(int length)
        {
            byte[] buffer = new byte[length];
            if (!ReadMemory(ref buffer))
            {
                return null;
            }
            return buffer;
        }

        public uint ReadUInt(bool reverse = false)
        {
            byte[] buffer = new byte[4];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            if (reverse) Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public int ReadInt(bool reverse = false)
        {
            byte[] buffer = new byte[4];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            if (reverse) Array.Reverse(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public ushort ReadUShort(bool reverse = false)
        {
            byte[] buffer = new byte[2];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            if (reverse) Array.Reverse(buffer);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public short ReadShort(bool reverse = false)
        {
            byte[] buffer = new byte[2];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            if (reverse) Array.Reverse(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }

        public byte ReadByte(bool reverse = false)
        {
            byte[] buffer = new byte[1];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            return buffer[0];
        }

        public sbyte ReadSByte(bool reverse = false)
        {
            byte[] buffer = new byte[1];
            if (!ReadMemory(ref buffer))
            {
                return 0;
            }
            return (sbyte)buffer[0];
        }

        public string ReadString(int length)
        {
            byte[] buffer = new byte[length];

            ReadMemory(ref buffer);

            string ret = Encoding.UTF8.GetString(buffer);

            if (ret.IndexOf("\0") != -1)
                ret.Remove(ret.IndexOf("\0"));

            return ret;
        }

        public string ReadString()
        {
            string buf = "";

            while (true)
            {
                byte b = ReadByte();
                if (b == 0x00) break;
                buf += (char)b;
            }

            return buf;
        }

        public bool WriteMemory(string value)
        {
            return WriteMemory(Encoding.Default.GetBytes(value));
        }

        public bool WriteMemory(string value, int padding)
        {
            if (!WriteMemory(value)) return false;
            if (!WriteMemory(new byte[padding - value.Length])) return false;
            return true;
        }

        public bool WriteMemory(double value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer);
        }

        public bool WriteMemory(float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer);
        }

        public bool WriteMemory(sbyte value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 1);
        }

        public bool WriteMemory(byte value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 1);
        }

        public bool WriteMemory(short value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 2);
        }

        public bool WriteMemory(ushort value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 2);
        }

        public bool WriteMemory(int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 4);
        }

        public bool WriteMemory(uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer, 4);
        }

        public bool WriteMemory(long value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            return WriteMemory(buffer);
        }

        public bool WriteMemory(byte[] buffer, int size)
        {
            bool v = Memory.WriteProcessMemory(pHandle, (UIntPtr)Position, buffer, (UIntPtr)size, IntPtr.Zero);
            if (v) Position += size;
            return v;
        }

        public bool WriteMemory(byte[] buffer)
        {
            bool v = Memory.WriteProcessMemory(pHandle, (UIntPtr)Position, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
            if (v) Position += buffer.Length;
            return v;
        }
    }
}
