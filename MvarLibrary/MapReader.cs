using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MVARStudio.MvarLibrary
{
    public class ReachMapReader : IDisposable
    {
        private FileStream _fs;
        private BinaryReader _br;
        private long _virtualBase;
        private long _indexPointer;
        private long _magic;

        public ReachMapReader(string filePath)
        {
            _fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _br = new BinaryReader(_fs);
            ParseHeader();
        }

        private void ParseHeader()
        {
            _fs.Seek(736, SeekOrigin.Begin);
            _virtualBase = _br.ReadInt64();
            _indexPointer = _br.ReadInt64();

            // MCC Reach translation is tricky. 
            // In many MCC maps, the pointers are actually absolute file offsets for certain sections.
            // But let's try to calculate magic based on common MCC structure.
            _fs.Seek(1228, SeekOrigin.Begin);
            uint s0_off = _br.ReadUInt32();
            uint s1_off = _br.ReadUInt32();
            uint s2_off = _br.ReadUInt32();
            uint s3_off = _br.ReadUInt32();

            _fs.Seek(1244, SeekOrigin.Begin);
            uint s0_addr = _br.ReadUInt32();
            uint s0_size = _br.ReadUInt32();

            // Magic often equals virtualBase - (section0_addr + section0_offset)
            // But in MCC, it can vary. 
            // We'll use a signature search for 'scnr' to calibrate if needed.
            _magic = _virtualBase - (s0_addr + s0_off);
        }

        private long Translate(long ptr)
        {
            if (ptr == 0) return 0;
            return ptr - _magic;
        }

        public byte[] ReadTagData(string className, out uint tagId)
        {
            tagId = 0;
            // 1. Find Tag Index
            // This is a simplified version for MVAR Studio
            // If the header parsing is unreliable, we search for the 'scnr' string reversed
            byte[] scnrMagic = Encoding.ASCII.GetBytes("rncs");
            long scnrPos = FindPattern(scnrMagic);
            if (scnrPos == -1) return null;

            // Scenario tag is usually the one pointing to a very large meta block
            // We'll search for the first palette name ID (0x840) to find the sandbox palette
            // within the scenario.
            
            // For now, we'll use a more targeted search for the Sandbox Palette count (87 for most MCC maps)
            // followed by a valid-looking pointer.
            return null;
        }

        public MapPalette LoadPalette(uint mapId)
        {
            var palette = new MapPalette { MapId = mapId };
            
            // Strategy: Search for the Sandbox Palette block in the map.
            // It starts with NameID 0x840 (Weapons Human) in most Reach maps.
            // byte[] pattern = { 0x40, 0x08, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00 }; // Name: 0x840, Count: 15
            
            // Actually, we can just use the walk_palettes.py's confirmed offset for MCC Reach
            // as it was stable across 3 maps.
            long paletteArrayOffset = 0x90EDA6C; // This is a magic offset for MCC Reach maps
            
            try
            {
                _fs.Seek(paletteArrayOffset, SeekOrigin.Begin);
                // Check if it looks like a palette (NameID around 0x800-0x900)
                uint testNameId = _br.ReadUInt32();
                if (testNameId < 0x800 || testNameId > 0xA00)
                {
                    // If offset fails, search for the pattern
                    paletteArrayOffset = FindPattern(new byte[] { 0x40, 0x08, 0x00, 0x00, 0x0F, 0x00, 0x00, 0x00 });
                }

                if (paletteArrayOffset == -1) return null;

                _fs.Seek(paletteArrayOffset, SeekOrigin.Begin);
                // Reach has about 16-18 main categories? No, it has 87 total Folders?
                // Wait, walk_palettes says "Palette 0... Entries=15".
                // So there are X Palettes (categories), each with Y Entries (folders).
                
                // Let's read 16 main categories
                for (int p = 0; p < 16; p++)
                {
                    _fs.Seek(paletteArrayOffset + p * 20, SeekOrigin.Begin);
                    uint nameId = _br.ReadUInt32();
                    uint unk = _br.ReadUInt32();
                    int entryCount = _br.ReadInt32();
                    uint entryPtr = _br.ReadUInt32();
                    
                    if (entryCount <= 0 || entryCount > 100) continue;

                    var mapPal = new MapFolder { Name = $"Category 0x{nameId:X}" }; 
                    // Note: Real name resolution would require string table
                    
                    // (Simplified item loading logic...)
                }
            }
            catch { return null; }

            return null;
        }



        public long FindPattern(byte[] pattern)
        {
            _fs.Seek(0, SeekOrigin.Begin);
            int bufferSize = 4096 * 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            long pos = 0;

            while ((bytesRead = _fs.Read(buffer, 0, bufferSize)) > 0)
            {
                for (int i = 0; i <= bytesRead - pattern.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (buffer[i + j] != pattern[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return pos + i;
                }
                pos += bytesRead - pattern.Length; // Overlap to catch split patterns
                _fs.Seek(pos, SeekOrigin.Begin);
            }
            return -1;
        }

        public void Dispose()
        {
            _br?.Close();
            _fs?.Dispose();
        }
    }
}

