using System;
using System.Collections.Generic;
using System.Text;

namespace MVARStudio.MvarLibrary
{
    public class BitstreamReader
    {
        private readonly byte[] _buffer;
        public int BitPosition { get; private set; }

        public BitstreamReader(byte[] buffer, int byteOffset = 0)
        {
            _buffer = buffer;
            BitPosition = byteOffset * 8;
        }

        public ulong ReadBits(int n)
        {
            ulong result = 0;
            for (int i = 0; i < n; i++)
            {
                int byteIdx = BitPosition >> 3;
                int bitOffset = 7 - (BitPosition & 7);
                ulong bit = (ulong)((_buffer[byteIdx] >> bitOffset) & 1);
                result = (result << 1) | bit;
                BitPosition++;
            }
            return result;
        }

        public long ReadBitsSigned(int n)
        {
            ulong r = ReadBits(n);
            if (n > 0 && (r & (1UL << (n - 1))) != 0)
            {
                return (long)(r - (1UL << n));
            }
            return (long)r;
        }

        public bool ReadFlag() => ReadBits(1) != 0;
        public byte ReadByte() => (byte)ReadBits(8);

        public uint ReadUInt16BE() => (uint)((ReadBits(8) << 8) | ReadBits(8));
        public uint ReadUInt32BE() => (uint)((ReadBits(8) << 24) | (ReadBits(8) << 16) | (ReadBits(8) << 8) | ReadBits(8));
        public uint ReadUInt32LE() => (uint)(ReadBits(8) | (ReadBits(8) << 8) | (ReadBits(8) << 16) | (ReadBits(8) << 24));
        public ulong ReadUInt64LE()
        {
            ulong r = 0;
            for (int i = 0; i < 8; i++) r |= (ReadBits(8) << (i * 8));
            return r;
        }

        public float ReadFloatBE()
        {
            byte[] bytes = new byte[4];
            bytes[0] = ReadByte();
            bytes[1] = ReadByte();
            bytes[2] = ReadByte();
            bytes[3] = ReadByte();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public string ReadStringStop(int maxN)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < maxN; i++)
            {
                byte b = ReadByte();
                if (b == 0) break;
                sb.Append((char)b);
            }
            return sb.ToString();
        }

        public string ReadWidecharStringBEStop(int maxWC)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < maxWC; i++)
            {
                ushort wc = (ushort)ReadUInt16BE();
                if (wc == 0) break;
                sb.Append((char)wc);
            }
            return sb.ToString();
        }

        public byte[] ReadBytesRaw(int n)
        {
            byte[] res = new byte[n];
            for (int i = 0; i < n; i++) res[i] = ReadByte();
            return res;
        }
    }

    public class BitstreamWriter
    {
        private byte[] _buffer;
        public int BitPosition { get; private set; }

        public BitstreamWriter(int capacity = 65536)
        {
            _buffer = new byte[capacity];
            BitPosition = 0;
        }

        public void WriteBits(int n, ulong value)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                int byteIdx = BitPosition >> 3;
                if (byteIdx >= _buffer.Length)
                {
                    Array.Resize(ref _buffer, _buffer.Length + 1024);
                }
                int bitOffset = 7 - (BitPosition & 7);
                ulong bit = (value >> i) & 1;
                if (bit != 0)
                    _buffer[byteIdx] |= (byte)(1 << bitOffset);
                else
                    _buffer[byteIdx] &= (byte)~(1 << bitOffset);
                BitPosition++;
            }
        }

        public void WriteBitsSigned(int n, long value)
        {
            ulong uValue = (ulong)value;
            if (value < 0)
            {
                uValue = (ulong)(value & (long)((1UL << n) - 1));
            }
            WriteBits(n, uValue);
        }

        public void WriteFlag(bool v) => WriteBits(1, v ? 1UL : 0UL);
        public void WriteByte(byte v) => WriteBits(8, v);

        public void WriteUInt16BE(ushort v)
        {
            WriteByte((byte)(v >> 8));
            WriteByte((byte)v);
        }

        public void WriteUInt32BE(uint v)
        {
            WriteByte((byte)(v >> 24));
            WriteByte((byte)(v >> 16));
            WriteByte((byte)(v >> 8));
            WriteByte((byte)v);
        }

        public void WriteUInt32LE(uint v)
        {
            WriteByte((byte)v);
            WriteByte((byte)(v >> 8));
            WriteByte((byte)(v >> 16));
            WriteByte((byte)(v >> 24));
        }

        public void WriteUInt64LE(ulong v)
        {
            for (int i = 0; i < 8; i++) WriteByte((byte)(v >> (i * 8)));
        }

        public void WriteFloatBE(float v)
        {
            byte[] bytes = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            foreach (byte b in bytes) WriteByte(b);
        }

        public void WriteStringStop(string s, int maxN)
        {
            int count = 0;
            foreach (char c in s)
            {
                if (count >= maxN - 1) break;
                WriteByte((byte)c);
                count++;
            }
            WriteByte(0);
        }

        public void WriteWidecharStringBEStop(string s, int maxWC)
        {
            int count = 0;
            foreach (char c in s)
            {
                if (count >= maxWC - 1) break;
                WriteUInt16BE((ushort)c);
                count++;
            }
            WriteUInt16BE(0);
        }

        public byte[] GetBytes(int? totalBits = null)
        {
            int byteCount = (BitPosition + 7) >> 3;
            byte[] result = new byte[byteCount];
            Array.Copy(_buffer, result, byteCount);

            if (totalBits.HasValue)
            {
                int targetBytes = (totalBits.Value + 7) >> 3;
                if (result.Length < targetBytes)
                {
                    Array.Resize(ref result, targetBytes);
                }
                else if (result.Length > targetBytes)
                {
                    byte[] clipped = new byte[targetBytes];
                    Array.Copy(result, clipped, targetBytes);
                    return clipped;
                }
            }
            return result;
        }
    }
}
