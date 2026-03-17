using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MVARStudio.MvarLibrary
{
    public class BlfChunk
    {
        public string Magic { get; set; }
        public uint Size { get; set; }
        public ushort Version { get; set; }
        public ushort Flags { get; set; }
        public byte[] Sha1 { get; set; }
        public uint ContentLength { get; set; }
        public byte[] Payload { get; set; }

        public byte[] ToBytes()
        {
            byte[] data = new byte[Size];
            using (var ms = new MemoryStream(data))
            using (var bw = new BinaryWriter(ms))
            {
                // Sig (4 bytes)
                byte[] sigBytes = Encoding.ASCII.GetBytes(Magic.PadRight(4).Substring(0, 4));
                bw.Write(sigBytes);

                // Size (4 bytes BE)
                bw.Write(Flip(Size));

                if (Size >= 36)
                {
                    // Version (2 bytes BE)
                    bw.Write(Flip(Version));

                    // Flags (2 bytes BE)
                    bw.Write(Flip(Flags));

                    // SHA-1 (20 bytes)
                    if (Sha1 == null || Sha1.Length != 20) Sha1 = new byte[20];
                    bw.Write(Sha1);

                    // Content Length (4 bytes BE)
                    bw.Write(Flip(ContentLength));

                    // Payload
                    if (Payload != null)
                    {
                        bw.Write(Payload, 0, Math.Min(Payload.Length, (int)Size - 36));
                    }
                }
                else
                {
                    // Small chunk payload
                    if (Payload != null)
                    {
                        bw.Write(Payload, 0, Math.Min(Payload.Length, (int)Size - 8));
                    }
                }
            }
            return data;
        }

        private static uint Flip(uint v) => BitConverter.IsLittleEndian ? ((v & 0xFF) << 24) | ((v & 0xFF00) << 8) | ((v >> 8) & 0xFF00) | (v >> 24) : v;
        private static ushort Flip(ushort v) => BitConverter.IsLittleEndian ? (ushort)(((v & 0xFF) << 8) | (v >> 8)) : v;
    }

    public class BlfFile
    {
        public List<BlfChunk> Chunks { get; set; } = new List<BlfChunk>();

        public static BlfFile Read(string path)
        {
            var blf = new BlfFile();
            byte[] data = File.ReadAllBytes(path);
            int pos = 0;

            while (pos + 8 <= data.Length)
            {
                string magic = Encoding.ASCII.GetString(data, pos, 4);
                uint size = ReadUInt32BE(data, pos + 4);

                // BLF chunks must have at least 8 bytes (magic + size)
                // If we encounter a tiny or zero size, something is wrong with the file structure.
                if (size < 8 || pos + size > data.Length) 
                {
                    // If this is the very first chunk and it's not _blf, it's not a valid BLF file.
                    if (pos == 0 && magic != "_blf") break;
                    
                    // Otherwise, we might have hit trailing data or corruption.
                    break; 
                }

                var chunk = new BlfChunk
                {
                    Magic = magic,
                    Size = size,
                    Sha1 = new byte[20]
                };

                if (size >= 36)
                {
                    chunk.Version = ReadUInt16BE(data, pos + 8);
                    chunk.Flags = ReadUInt16BE(data, pos + 10);
                    Array.Copy(data, pos + 12, chunk.Sha1, 0, 20);
                    chunk.ContentLength = ReadUInt32BE(data, pos + 32);
                    
                    int payloadSize = (int)size - 36;
                    if (payloadSize > 0)
                    {
                        chunk.Payload = new byte[payloadSize];
                        Array.Copy(data, pos + 36, chunk.Payload, 0, payloadSize);
                    }
                }
                else
                {
                    // Small chunk (like _eof) - just capture payload after size
                    int payloadSize = (int)size - 8;
                    if (payloadSize > 0)
                    {
                        chunk.Payload = new byte[payloadSize];
                        Array.Copy(data, pos + 8, chunk.Payload, 0, payloadSize);
                    }
                }

                blf.Chunks.Add(chunk);
                pos += (int)size;
            }

            return blf;
        }

        public void Save(string path)
        {
            using (var fs = File.Create(path))
            {
                foreach (var chunk in Chunks)
                {
                    if (chunk.Magic == "mvar")
                    {
                        // Recalculate SHA-1
                        using (var sha1 = SHA1.Create())
                        {
                            chunk.Sha1 = sha1.ComputeHash(chunk.Payload);
                        }
                    }
                    byte[] bytes = chunk.ToBytes();
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        private static uint ReadUInt32BE(byte[] data, int off)
        {
            uint v = BitConverter.ToUInt32(data, off);
            return BitConverter.IsLittleEndian ? ((v & 0xFF) << 24) | ((v & 0xFF00) << 8) | ((v >> 8) & 0xFF00) | (v >> 24) : v;
        }

        private static ushort ReadUInt16BE(byte[] data, int off)
        {
            ushort v = BitConverter.ToUInt16(data, off);
            return BitConverter.IsLittleEndian ? (ushort)(((v & 0xFF) << 8) | (v >> 8)) : v;
        }
    }
}
