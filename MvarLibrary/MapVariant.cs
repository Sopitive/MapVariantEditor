using System;
using System.Collections.Generic;
using System.Linq;

namespace MVARStudio.MvarLibrary
{
    public class MapVariant
    {
        public GameVariantHeader Header { get; set; } = new GameVariantHeader();
        public BoundingBox BBox { get; set; } = new BoundingBox();
        public List<ForgeObject> Objects { get; set; } = new List<ForgeObject>();
        public List<TypeMinMax> TypeMinMax { get; set; } = new List<TypeMinMax>();

        public byte Version { get; set; }
        public uint MapId { get; set; }
        public uint BudgetMax { get; set; }
        public uint BudgetSpent { get; set; }
        public ForgeLabels ForgeLabels { get; set; } = new ForgeLabels();
        public ushort CanvasFoldersCount { get; set; }

        public uint MapMccId { get; set; }
        public ushort UnkMcc04 { get; set; }
        public ushort UnkMcc06 { get; set; }
        public ulong UnkMcc08 { get; set; }

        public byte Unk02A0 { get; set; }
        public byte Unk02A1 { get; set; }
        public byte Unk02A2 { get; set; }
        public byte Unk02A3 { get; set; }
        public uint Unk02A4 { get; set; }
        public bool HardBoundary { get; set; }
        public bool IsCinematic { get; set; }
        public ulong UnkHeader64 { get; set; }

        public static MapVariant Parse(byte[] payload)
        {
            var mv = new MapVariant();
            var br = new BitstreamReader(payload);

            // Header
            mv.Header.Type = (byte)(br.ReadBits(4) - 1);
            mv.Header.FileLength = br.ReadUInt32LE();
            mv.Header.Unk08 = br.ReadUInt64LE();
            mv.Header.Unk10 = br.ReadUInt64LE();
            mv.Header.Unk18 = br.ReadUInt64LE();
            mv.Header.Unk20 = br.ReadUInt64LE();
            mv.Header.Activity = (byte)(br.ReadBits(3) - 1);
            mv.Header.GameMode = (byte)br.ReadBits(3);
            mv.Header.Engine = (byte)br.ReadBits(3);
            mv.Header.MapId = (uint)br.ReadBits(32);
            mv.Header.EngineCat = (byte)br.ReadBits(8);
            mv.Header.CreatedBy = ContentAuthor.Parse(br);
            mv.Header.ModifiedBy = ContentAuthor.Parse(br);
            mv.Header.Title = br.ReadWidecharStringBEStop(128);
            mv.Header.Description = br.ReadWidecharStringBEStop(128);

            if (mv.Header.Activity == 2) mv.Header.HopperId = (ushort)br.ReadUInt16BE();

            if (mv.Header.GameMode == 1)
            {
                mv.Unk02A0 = (byte)br.ReadBits(8);
                mv.Unk02A1 = (byte)br.ReadBits(2);
                mv.Unk02A2 = (byte)br.ReadBits(2);
                mv.Unk02A3 = (byte)br.ReadBits(8);
                mv.Unk02A4 = (uint)br.ReadBits(32);
            }
            else if (mv.Header.GameMode == 2)
            {
                mv.Unk02A0 = (byte)br.ReadBits(2);
                mv.Unk02A4 = (uint)br.ReadBits(32);
            }

            mv.Version = (byte)br.ReadBits(8);
            mv.UnkHeader64 = br.ReadBits(64);
            mv.CanvasFoldersCount = (ushort)br.ReadBits(9);
            mv.MapId = (uint)br.ReadBits(32);
            mv.HardBoundary = br.ReadFlag();
            mv.IsCinematic = br.ReadFlag();

            mv.BBox.XMin = br.ReadFloatBE(); mv.BBox.XMax = br.ReadFloatBE();
            mv.BBox.YMin = br.ReadFloatBE(); mv.BBox.YMax = br.ReadFloatBE();
            mv.BBox.ZMin = br.ReadFloatBE(); mv.BBox.ZMax = br.ReadFloatBE();

            mv.BudgetMax = (uint)br.ReadBits(32);
            mv.BudgetSpent = (uint)br.ReadBits(32);

            mv.ForgeLabels = ForgeLabels.Parse(br);

            if (mv.Version >= 32)
            {
                mv.MapMccId = (uint)br.ReadBits(32);
                mv.UnkMcc04 = (ushort)br.ReadBits(16);
                mv.UnkMcc06 = (ushort)br.ReadBits(16);
                mv.UnkMcc08 = br.ReadBits(64);
            }

            for (int i = 0; i < 651; i++)
            {
                var obj = ForgeObject.Parse(br, mv.BBox);
                obj.Slot = i;
                mv.Objects.Add(obj);
            }

            for (int i = 0; i < 256; i++)
            {
                if (i < mv.CanvasFoldersCount)
                {
                    mv.TypeMinMax.Add(new TypeMinMax { Min = (byte)br.ReadBits(8), Max = (byte)br.ReadBits(8), Placed = (byte)br.ReadBits(8) });
                }
                else mv.TypeMinMax.Add(new TypeMinMax());
            }

            return mv;
        }

        public byte[] Encode()
        {
            var bw = new BitstreamWriter();
            var hdr = Header;

            bw.WriteBits(4, (uint)hdr.Type + 1);
            bw.WriteUInt32LE(hdr.FileLength);
            bw.WriteUInt64LE(hdr.Unk08);
            bw.WriteUInt64LE(hdr.Unk10);
            bw.WriteUInt64LE(hdr.Unk18);
            bw.WriteUInt64LE(hdr.Unk20);
            bw.WriteBits(3, (uint)hdr.Activity + 1);
            bw.WriteBits(3, (uint)hdr.GameMode);
            bw.WriteBits(3, (uint)hdr.Engine);
            bw.WriteBits(32, hdr.MapId);
            bw.WriteBits(8, (uint)hdr.EngineCat);
            hdr.CreatedBy.Encode(bw);
            hdr.ModifiedBy.Encode(bw);
            bw.WriteWidecharStringBEStop(hdr.Title, 128);
            bw.WriteWidecharStringBEStop(hdr.Description, 128);

            if (hdr.Activity == 2) bw.WriteUInt16BE(hdr.HopperId);

            if (hdr.GameMode == 1)
            {
                bw.WriteBits(8, Unk02A0); bw.WriteBits(2, Unk02A1);
                bw.WriteBits(2, Unk02A2); bw.WriteBits(8, Unk02A3);
                bw.WriteBits(32, Unk02A4);
            }
            else if (hdr.GameMode == 2)
            {
                bw.WriteBits(2, Unk02A0); bw.WriteBits(32, Unk02A4);
            }

            bw.WriteBits(8, Version);
            bw.WriteBits(64, UnkHeader64); 
            bw.WriteBits(9, CanvasFoldersCount);
            bw.WriteBits(32, MapId);
            bw.WriteFlag(HardBoundary);
            bw.WriteFlag(IsCinematic);

            bw.WriteFloatBE(BBox.XMin); bw.WriteFloatBE(BBox.XMax);
            bw.WriteFloatBE(BBox.YMin); bw.WriteFloatBE(BBox.YMax);
            bw.WriteFloatBE(BBox.ZMin); bw.WriteFloatBE(BBox.ZMax);

            bw.WriteBits(32, BudgetMax);
            bw.WriteBits(32, BudgetSpent);

            ForgeLabels.Encode(bw);

            if (Version >= 32)
            {
                bw.WriteBits(32, MapMccId);
                bw.WriteBits(16, UnkMcc04);
                bw.WriteBits(16, UnkMcc06);
                bw.WriteBits(64, UnkMcc08);
            }

            foreach (var obj in Objects) obj.Encode(bw, BBox);

            for (int i = 0; i < 256; i++)
            {
                if (i < TypeMinMax.Count)
                {
                    bw.WriteBits(8, TypeMinMax[i].Min);
                    bw.WriteBits(8, TypeMinMax[i].Max);
                    bw.WriteBits(8, TypeMinMax[i].Placed);
                }
                else { bw.WriteBits(8, 0); bw.WriteBits(8, 0); bw.WriteBits(8, 0); }
            }

            return bw.GetBytes(229408);
        }
    }

    public class GameVariantHeader
    {
        public byte Type { get; set; }
        public uint FileLength { get; set; }
        public ulong Unk08 { get; set; }
        public ulong Unk10 { get; set; }
        public ulong Unk18 { get; set; }
        public ulong Unk20 { get; set; }
        public byte Activity { get; set; }
        public byte GameMode { get; set; }
        public byte Engine { get; set; }
        public uint MapId { get; set; }
        public byte EngineCat { get; set; }
        public ContentAuthor CreatedBy { get; set; } = new ContentAuthor();
        public ContentAuthor ModifiedBy { get; set; } = new ContentAuthor();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public ushort HopperId { get; set; }
    }

    public class ContentAuthor
    {
        public ulong Timestamp { get; set; }
        public ulong Xuid { get; set; }
        public string Name { get; set; } = "";
        public bool IsOnline { get; set; }

        public static ContentAuthor Parse(BitstreamReader br)
        {
            return new ContentAuthor
            {
                Timestamp = br.ReadUInt64LE(),
                Xuid = br.ReadUInt64LE(),
                Name = br.ReadStringStop(16),
                IsOnline = br.ReadFlag()
            };
        }

        public void Encode(BitstreamWriter bw)
        {
            bw.WriteUInt64LE(Timestamp);
            bw.WriteUInt64LE(Xuid);
            bw.WriteStringStop(Name, 16);
            bw.WriteFlag(IsOnline);
        }
    }

    public class BoundingBox
    {
        public float XMin { get; set; }
        public float XMax { get; set; }
        public float YMin { get; set; }
        public float YMax { get; set; }
        public float ZMin { get; set; }
        public float ZMax { get; set; }
    }

    public class ForgeLabels
    {
        public List<string> Strings { get; set; } = new List<string>();

        public static ForgeLabels Parse(BitstreamReader br)
        {
            var fl = new ForgeLabels();
            int count = (int)br.ReadBits(9);
            var offsets = new List<int?>();
            for (int i = 0; i < count; i++)
            {
                if (br.ReadFlag()) offsets.Add((int)br.ReadBits(12));
                else offsets.Add(null);
            }
            if (count > 0)
            {
                int dataLen = (int)br.ReadBits(13);
                bool compressed = br.ReadFlag();
                byte[] data = br.ReadBytesRaw(dataLen);
                foreach (var off in offsets)
                {
                    if (off == null) fl.Strings.Add(null);
                    else if (data.Length == 0 || off.Value >= data.Length) fl.Strings.Add("");
                    else
                    {
                        int end = Array.IndexOf(data, (byte)0, off.Value);
                        if (end == -1) end = data.Length;
                        fl.Strings.Add(System.Text.Encoding.ASCII.GetString(data, off.Value, end - off.Value));
                    }
                }
            }
            return fl;
        }

        public void Encode(BitstreamWriter bw)
        {
            bw.WriteBits(9, (uint)Strings.Count);
            var raw = new List<byte>();
            var offsets = new List<int?>();
            foreach (var s in Strings)
            {
                if (s == null) offsets.Add(null);
                else
                {
                    offsets.Add(raw.Count);
                    raw.AddRange(System.Text.Encoding.ASCII.GetBytes(s));
                    raw.Add(0);
                }
            }
            foreach (var off in offsets)
            {
                if (off == null) bw.WriteFlag(false);
                else { bw.WriteFlag(true); bw.WriteBits(12, (uint)off.Value); }
            }
            if (Strings.Count > 0)
            {
                bw.WriteBits(13, (uint)raw.Count);
                bw.WriteFlag(false);
                foreach (var b in raw) bw.WriteByte(b);
            }
        }
    }

    public class ForgeObject
    {
        public int Slot { get; set; }
        public bool Present { get; set; }
        public byte Flags { get; set; }
        public ushort ForgeFolder { get; set; }
        public byte ForgeFolderItem { get; set; }
        public Vector3 Position { get; set; } = new Vector3();
        public bool UpIsGlobal { get; set; }
        public uint UpQuant { get; set; }
        public ushort Angle { get; set; }
        public int Relative { get; set; }
        public ObjectExtra Extra { get; set; } = new ObjectExtra();

        public static ForgeObject Parse(BitstreamReader br, BoundingBox bb)
        {
            var obj = new ForgeObject();
            obj.Present = br.ReadFlag();
            if (!obj.Present) return obj;

            obj.Flags = (byte)br.ReadBits(2);
            if (br.ReadFlag()) obj.ForgeFolder = 0xFFFF; else obj.ForgeFolder = (ushort)br.ReadBits(8);
            if (br.ReadFlag()) obj.ForgeFolderItem = 0xFF; else obj.ForgeFolderItem = (byte)br.ReadBits(5);

            obj.Position = Vector3.Parse(br, bb);

            if (br.ReadFlag()) obj.UpIsGlobal = true;
            else { obj.UpIsGlobal = false; obj.UpQuant = (uint)br.ReadBits(20); }
            obj.Angle = (ushort)br.ReadBits(14);
            obj.Relative = (int)br.ReadBits(10) - 1;

            obj.Extra = ObjectExtra.Parse(br);
            return obj;
        }

        public void Encode(BitstreamWriter bw, BoundingBox bb)
        {
            bw.WriteFlag(Present);
            if (!Present) return;

            bw.WriteBits(2, Flags);
            if (ForgeFolder == 0xFFFF) bw.WriteFlag(true); else { bw.WriteFlag(false); bw.WriteBits(8, ForgeFolder); }
            if (ForgeFolderItem == 0xFF) bw.WriteFlag(true); else { bw.WriteFlag(false); bw.WriteBits(5, ForgeFolderItem); }

            Position.Encode(bw, bb);

            if (UpIsGlobal) bw.WriteFlag(true);
            else { bw.WriteFlag(false); bw.WriteBits(20, UpQuant); }
            bw.WriteBits(14, Angle);
            bw.WriteBits(10, (uint)(Relative + 1));

            Extra.Encode(bw);
        }
    }

    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool InBounds { get; set; }
        public bool HasBsp { get; set; }
        public byte BspIndex { get; set; }

        public static Vector3 Parse(BitstreamReader br, BoundingBox bb)
        {
            int[] bits = ComputeAxisBits(21, bb);
            var v = new Vector3();
            v.InBounds = br.ReadFlag();
            if (!v.InBounds) 
            { 
                v.HasBsp = br.ReadFlag();
                if (!v.HasBsp) v.BspIndex = (byte)br.ReadBits(2); 
            }

            v.X = (0.5f + (uint)br.ReadBits(bits[0])) * ((bb.XMax - bb.XMin) / (1 << bits[0])) + bb.XMin;
            v.Y = (0.5f + (uint)br.ReadBits(bits[1])) * ((bb.YMax - bb.YMin) / (1 << bits[1])) + bb.YMin;
            v.Z = (0.5f + (uint)br.ReadBits(bits[2])) * ((bb.ZMax - bb.ZMin) / (1 << bits[2])) + bb.ZMin;
            return v;
        }

        public void Encode(BitstreamWriter bw, BoundingBox bb)
        {
            int[] bits = ComputeAxisBits(21, bb);
            bw.WriteFlag(InBounds);
            if (!InBounds)
            {
                bw.WriteFlag(HasBsp);
                if (!HasBsp) bw.WriteBits(2, BspIndex);
            }

            float[] coords = { X, Y, Z };
            float[] bb_mins = { bb.XMin, bb.YMin, bb.ZMin };
            float[] bb_maxs = { bb.XMax, bb.YMax, bb.ZMax };

            for (int i = 0; i < 3; i++)
            {
                int n_bits = bits[i];
                if (n_bits == 0) continue;
                float rng = bb_maxs[i] - bb_mins[i];
                uint raw = (uint)((coords[i] - bb_mins[i]) / rng * (1 << n_bits));
                uint maxVal = (uint)((1 << n_bits) - 1);
                if (raw > maxVal) raw = maxVal;
                bw.WriteBits(n_bits, raw);
            }
        }

        private static int[] ComputeAxisBits(int bitcount, BoundingBox bb)
        {
            float MINIMUM_UNIT_16BIT = 0.00833333333f;
            float min_step = (bitcount > 0x10) ? MINIMUM_UNIT_16BIT / (1 << (bitcount - 0x10)) : (1 << (0x10 - bitcount)) * MINIMUM_UNIT_16BIT;
            if (min_step < 0.0001f) return new int[] { 26, 26, 26 };

            float[] axes = { bb.XMax - bb.XMin, bb.YMax - bb.YMin, bb.ZMax - bb.ZMin };
            min_step *= 2;
            int[] outBits = { bitcount, bitcount, bitcount };
            for (int i = 0; i < 3; i++)
            {
                int edx = (int)Math.Min(0x800000, axes[i] / min_step + 0.9999f);
                if (edx == 0) outBits[i] = 0;
                else
                {
                    int ecx = HighestBitSet(edx);
                    int eax = (1 << ecx) - 1;
                    int r8 = ecx + ((edx & eax) != 0 ? 1 : 0);
                    outBits[i] = Math.Min(26, r8);
                }
            }
            return outBits;
        }

        private static int HighestBitSet(int value)
        {
            int r = 0;
            while ((value >>= 1) != 0) r++;
            return r;
        }
    }

    public class ObjectExtra
    {
        public byte ShapeType { get; set; }
        public float ShapeRadius { get; set; }
        public float ShapeLength { get; set; }
        public float ShapeTop { get; set; }
        public float ShapeBottom { get; set; }

        public sbyte SpawnSeq { get; set; }
        public byte RespawnTime { get; set; }
        public byte MpType { get; set; }
        public int ForgeLabelIdx { get; set; }
        public byte PlacementFlags { get; set; }
        public byte TeamRaw { get; set; }
        public int Color { get; set; }

        public byte SpareClips { get; set; }
        public byte TeleChannel { get; set; }
        public byte TelePassability { get; set; }
        public byte LocationNameIdx { get; set; }

        public static ObjectExtra Parse(BitstreamReader br)
        {
            var ex = new ObjectExtra();
            ex.ShapeType = (byte)br.ReadBits(2);
            if (ex.ShapeType == 1) ex.ShapeRadius = DecodeDim((uint)br.ReadBits(11));
            else if (ex.ShapeType == 2) { ex.ShapeRadius = DecodeDim((uint)br.ReadBits(11)); ex.ShapeTop = DecodeDim((uint)br.ReadBits(11)); ex.ShapeBottom = DecodeDim((uint)br.ReadBits(11)); }
            else if (ex.ShapeType == 3) { ex.ShapeRadius = DecodeDim((uint)br.ReadBits(11)); ex.ShapeLength = DecodeDim((uint)br.ReadBits(11)); ex.ShapeTop = DecodeDim((uint)br.ReadBits(11)); ex.ShapeBottom = DecodeDim((uint)br.ReadBits(11)); }

            ex.SpawnSeq = (sbyte)br.ReadBitsSigned(8);
            ex.RespawnTime = (byte)br.ReadBits(8);
            ex.MpType = (byte)br.ReadBits(5);

            if (br.ReadFlag()) ex.ForgeLabelIdx = -1; else ex.ForgeLabelIdx = (int)br.ReadBits(8);
            ex.PlacementFlags = (byte)br.ReadBits(8);
            ex.TeamRaw = (byte)br.ReadBits(4);
            if (br.ReadFlag()) ex.Color = -1; else ex.Color = (int)br.ReadBits(3);

            if (ex.MpType == 1) ex.SpareClips = (byte)br.ReadBits(8);
            else if (ex.MpType >= 12 && ex.MpType <= 14) { ex.TeleChannel = (byte)br.ReadBits(5); ex.TelePassability = (byte)br.ReadBits(5); }
            else if (ex.MpType == 19) ex.LocationNameIdx = (byte)(br.ReadBits(8) - 1);

            return ex;
        }

        public void Encode(BitstreamWriter bw)
        {
            bw.WriteBits(2, ShapeType);
            if (ShapeType == 1) bw.WriteBits(11, EncodeDim(ShapeRadius));
            else if (ShapeType == 2) { bw.WriteBits(11, EncodeDim(ShapeRadius)); bw.WriteBits(11, EncodeDim(ShapeTop)); bw.WriteBits(11, EncodeDim(ShapeBottom)); }
            else if (ShapeType == 3) { bw.WriteBits(11, EncodeDim(ShapeRadius)); bw.WriteBits(11, EncodeDim(ShapeLength)); bw.WriteBits(11, EncodeDim(ShapeTop)); bw.WriteBits(11, EncodeDim(ShapeBottom)); }

            bw.WriteBitsSigned(8, SpawnSeq);
            bw.WriteBits(8, RespawnTime);
            bw.WriteBits(5, MpType);

            if (ForgeLabelIdx == -1) bw.WriteFlag(true); else { bw.WriteFlag(false); bw.WriteBits(8, (uint)ForgeLabelIdx); }
            bw.WriteBits(8, PlacementFlags);
            bw.WriteBits(4, TeamRaw);
            if (Color == -1) bw.WriteFlag(true); else { bw.WriteFlag(false); bw.WriteBits(3, (uint)Color); }

            if (MpType == 1) bw.WriteBits(8, SpareClips);
            else if (MpType >= 12 && MpType <= 14) { bw.WriteBits(5, TeleChannel); bw.WriteBits(5, TelePassability); }
            else if (MpType == 19) bw.WriteBits(8, (uint)(LocationNameIdx + 1));
        }

        private static float DecodeDim(uint r) => r == 0 ? 0 : r == 0x7FF ? 200 : (r - 1) * 0.0977517142892f + 0.0488758571446f;
        private static uint EncodeDim(float m) => m <= 0 ? 0 : m >= 200 ? 0x7FFU : (uint)((m - 0.0488758571446f) / 0.0977517142892f + 1.5f);
    }

    public class TypeMinMax { public byte Min; public byte Max; public byte Placed; }
}
