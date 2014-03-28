using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GovanifY.Utility;

namespace IDX_Tools
{
    internal class IDXFile : IDisposable, IEnumerable<IDXFile.IDXEntry>
    {
        public class IDXEntry
        {
            /// <summary><para>Identifier of this file</para><para>Hashed filename</para></summary>
            public uint Hash;
            /// <summary><para>Secondary identifier of this file</para><para>Hashed filename</para></summary>
            public ushort HashAlt = 0;
            /// <summary><para>Compressed length of data, in sectors</para><para>Bit 0x4000 flags if compressed</para></summary>
            public ushort Compression = 0;
            /// <summary>Location of data in IMG (LBA)</summary>
            public uint DataLBA;
            /// <summary>Data length (size of file)</summary>
            public uint DataLength;
            /// <summary>Location of data in IMG (bytes)</summary>
            public long Offset { get { return this.DataLBA * 2048; } }
            /// <summary>Returns true if this file is compressed</summary>
            public bool IsCompressed
            {
                get { return (this.Compression & 0x4000u) == 0x4000u; }
                set { this.Compression = (ushort)((value ? 0x4000u : 0) | (this.Compression & 0xBFFFu)); }
            }
            /// <summary>Returns true if both hashes are checked</summary>
            public bool IsDualHash
            {
                get { return (this.Compression & 0x8000u) == 0x8000u; }
                set { this.Compression = (ushort)((value ? 0x8000u : 0) | (this.Compression & 0x7FFFu)); }
            }
            /// <summary>Calculated the compressed data size, in bytes</summary>
            public uint CompressedDataLength
            {
                get
                {
                    // KH2 does get this value by and-ing with 0x3FFF, so that's confirmed
                    uint size = ((uint)this.Compression & 0x3FFFu) + 1u;
                    //Fixes file 0x10303F6F:
                    //  Real compressed size = 12,093,440 bytes (Verified manually)
                    //  Compression = 0x4710
                    //  (0x4710 & 0x3FFF) + 1 = 0x711 * 2048 = 3,704,832 + 0x800000 = 12,093,440 bytes
                    //What is happening is the size is getting truncated (see the set function).
                    //0x10303F6F is the only compressed file that hits this limitation officially, and uncompressed files can bypass it (use DataLength).
                    //Also note this increases *all* files about the specified size
                    if (this.Hash == 0x10303F6F && this.IsCompressed && this.DataLength > 0xC00000) { size += 0x1000u; }
                    return size * 2048u;
                }
                set
                {
                    if (value == 0) { this.Compression &= (ushort)0xC000u; return; }
                    uint size = (uint)Math.Ceiling((double)value / 2048) - 1;
                    //Seems that the "size" component just truncated if it's too large, as seen by the larger files (videos, Title.vas)
                    /* Size component:
                     * * Includes at minimum 0x0FFF
                     * * Does NOT include 0x1000 (@noname/10303F6F.bin; vagstream/Title.vas; zmovie/fm/me3.pss; zmovie/fm/opn.pss)
                     * * Does include 0x2000 (zmovie/fm/me3.pss; zmovie/fm/opn.pss)
                     * * 0x4000 is the compression flag
                     * * 0x8000 is the alt hash flag
                     * Nothing has broken these rules, that I can find
                    */
                    this.Compression = (ushort)((this.Compression & 0xC000u) | (size & 0x2FFFu));
                }
            }
        }
        private BinaryReader file;
        private bool leaveOpen;
        public uint Count { get; private set; }
        public uint Position { get; private set; }
        protected IDXFile() { this.file = null; }
        public IDXFile(Stream input, bool newidx = false, bool leaveOpen = false)
        {
            this.file = new BinaryReader(input);
            this.leaveOpen = leaveOpen;
            input.Position = 0;
            if (newidx)
            {
                this.Count = 0;
                input.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
            }
            else
            {
                this.Count = this.file.ReadUInt32();
            }
            this.Position = 0;
        }
        public void Dispose(){
            if (this.file != null) { if (!leaveOpen) { this.file.Close(); } this.file = null; }
        }
        public IEnumerator<IDXEntry> GetEnumerator()
        {
            for (uint i = 0; i < this.Count; ++i)
            {
                yield return this.ReadEntry(i);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public IDXEntry ReadEntry(long index = -1)
        {
            if (index >= 0) { this.Position = (uint)index; }
            if (this.Position >= this.Count) { return new IDXEntry() { Hash = 0 }; }
            this.file.BaseStream.Position = 4 + 16 * this.Position++;
            IDXEntry entry = new IDXEntry()
            {
                Hash = this.file.ReadUInt32(),
                HashAlt = this.file.ReadUInt16(),
                Compression = this.file.ReadUInt16(),
                DataLBA = this.file.ReadUInt32(),
                DataLength = this.file.ReadUInt32()
            };
            return entry;
        }
        private void WriteEntry(IDXEntry entry, uint count = 0)
        {
            using (BinaryStream bw = new BinaryStream(this.file.BaseStream, leaveOpen:true))
            {
                bw.Write(entry.Hash);
                bw.Write(entry.HashAlt);
                bw.Write(entry.Compression);
                bw.Write(entry.DataLBA);
                bw.Write(entry.DataLength);
                if (count != 0)
                {
                    bw.BaseStream.Position = 0;
                    bw.Write(count);
                }
            }
        }
        private void FindEntryByHash(uint hash)
        {
            this.file.BaseStream.Position = 4;
            for (uint i = 0; i < this.Count; ++i)
            {
                if (this.file.ReadUInt32() == hash)
                {
                    this.file.BaseStream.Position -= 4;
                    return;
                }
                this.file.BaseStream.Position += 12;
            }
            throw new FileNotFoundException();
        }
        /// <summary>Relink hash to target</summary>
        /// <param name="hash"></param>
        /// <param name="target"></param>
        public void RelinkEntry(uint hash, uint target)
        {
            FindEntryByHash(target);
            this.file.BaseStream.Position+=4;
            IDXEntry entry = new IDXEntry()
            {
                Hash = hash,
                HashAlt = this.file.ReadUInt16(),
                Compression = this.file.ReadUInt16(),
                DataLBA = this.file.ReadUInt32(),
                DataLength = this.file.ReadUInt32()
            };
            FindEntryByHash(hash);
            WriteEntry(entry);
        }
        public void ModifyEntry(IDXEntry entry)
        {
            FindEntryByHash(entry.Hash);
            WriteEntry(entry);
        }
        public void AddEntry(IDXEntry entry)
        {
            this.file.BaseStream.Position = 4 + 16 * this.Count;
            WriteEntry(entry, ++this.Count);
        }
    }
    internal class IDXFileWriter
    {
        List<IDX_Tools.IDXFile.IDXEntry> entries = new List<IDX_Tools.IDXFile.IDXEntry>();
        public IDXFileWriter() { }
        public void AddEntry(IDX_Tools.IDXFile.IDXEntry entry)
        {
            this.entries.Add(entry);
        }
        public void RelinkEntry(uint hash, uint target)
        {
            IDX_Tools.IDXFile.IDXEntry t = this.entries.Find(e => e.Hash == target);
            if (t.Hash == 0) { throw new FileNotFoundException(); }
            this.entries.Add(new IDX_Tools.IDXFile.IDXEntry()
            {
                Hash = hash,
                HashAlt = 0,
                Compression = t.Compression,
                DataLBA = t.DataLBA,
                DataLength = t.DataLength
            });
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public MemoryStream GetStream()
        {
            this.entries.Sort((a, b) => a.Hash < b.Hash ? -1 : (a.Hash > b.Hash ? 1 : 0));
            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryStream bw = new BinaryStream(ms, leaveOpen:true))
                {
                    bw.Write(this.entries.Count);
                    foreach (var entry in this.entries)
                    {
                        bw.Write(entry.Hash);
                        bw.Write(entry.HashAlt);
                        bw.Write(entry.Compression);
                        bw.Write(entry.DataLBA);
                        bw.Write(entry.DataLength);
                    }
                }
            }
            catch (Exception)
            {
                ms.Close();
                throw;
            }
            ms.Position = 0;
            return ms;
        }
    }
    internal class IMGFile : IDisposable
    {
        internal Stream file;
        private long offset;
        private bool leaveOpen;
        public IMGFile(Stream file, long offset = 0, bool leaveOpen = false)
        {
            this.file = file;
            this.offset = offset;
            this.leaveOpen = leaveOpen;
        }
        public void Dispose() { Dispose(true); }
        public void Dispose(bool disposing)
        {
            if (this.file != null) { if (!leaveOpen) { this.file.Dispose(); } this.file = null; }
        }
        private static byte[] copyBuffer = new byte[2048 * 2];
        private void ReadFileBuffer(Stream destination, long origin, uint length)
        {
            //Not thread safe, but I'm not using threads
            this.file.Position = this.offset + origin;
            int num;
            while (length > 0 && (num = this.file.Read(copyBuffer, 0, (int)Math.Min(2048 * 2, length))) != 0)
            {
                destination.Write(copyBuffer, 0, num);
                length -= (uint)num;
            }
        }
        /// <summary>Ensure position is at a 2048 boundary</summary>
        private void EnsureBoundary()
        {
            if (((this.file.Position - this.offset) % 2048) != 0)
            {
                int rem = 2048 - (int)((this.file.Position - this.offset) % 2048);
                byte[] buf = new byte[] { 0, 0, 0, 0 };
                while (rem > 3) { this.file.Write(buf, 0, 4); rem -= 4; }
                while (--rem >= 0) { this.file.WriteByte(0); }
            }
            if (((this.file.Position - this.offset) % 2048) != 0) { throw new DataMisalignedException(); }
        }
        public Substream GetFileStream(IDXFile.IDXEntry entry)
        {
            return new Substream(this.file, this.offset + entry.DataLBA * 2048, entry.IsCompressed ? entry.CompressedDataLength : entry.DataLength);
        }
        public void Seek(uint sector)
        {
            this.file.Position = this.offset + sector * 2048;
        }
        public void ReadFile(IDXFile.IDXEntry entry, Stream target)
        {
            if (entry.IsCompressed)
            {
                if (entry.CompressedDataLength > int.MaxValue) { throw new NotSupportedException("File to big to decompress"); }
                byte[] input = new byte[entry.CompressedDataLength];
                this.Seek(entry.DataLBA);
                this.file.Read(input, 0, (int)entry.CompressedDataLength);
                try
                {
                    byte[] output = KHCompress.KH2Compressor.decompress(input, entry.DataLength);
                    target.Write(output, 0, output.Length);
                }
                catch (Exception e) { KH2FM_Toolkit.Program.WriteError(" ERROR: Failed to decompress: " + e.Message); }
            }
            else { this.ReadFileBuffer(target, entry.Offset, entry.DataLength); }
        }
        public void WriteFile(Stream data)
        {
            if (data.Length > 0xFFFFFFFF) { throw new NotSupportedException("data too big to store"); }
            this.EnsureBoundary();
            data.CopyTo(this.file);
            this.EnsureBoundary();
        }
        public void AppendFile(IDXFile.IDXEntry entry, Stream data)
        {
            if (data.Length > 0xFFFFFFFF) { throw new NotSupportedException("data too big to store"); }
            this.file.Seek(0, SeekOrigin.End);
            this.EnsureBoundary();
            entry.DataLBA = (uint)(this.file.Position - this.offset) / 2048;
            data.CopyTo(this.file);
            this.EnsureBoundary();
        }
    }
    internal class IDXIMGWriter : IDisposable
    {
        private IDXFileWriter idx = new IDXFileWriter();
        public IMGFile img { get; private set; }
        public IDXIMGWriter(Stream img, long imgoffset = 0, bool leaveOpen = false)
        {
            this.img = new IMGFile(img, imgoffset);
            this.img = new IMGFile(img, imgoffset, leaveOpen);
        }
        public void Dispose() { Dispose(true); }
        public void Dispose(bool disposing)
        {
            if (this.img != null) { this.img.Dispose(); this.img = null; }
            if (disposing && this.idx != null) { this.idx = null; }
        }
        public void AddFile(IDX_Tools.IDXFile.IDXEntry file, Stream data)
        {
            this.img.AppendFile(file, data);
            this.idx.AddEntry(file);
        }
        public void RelinkFile(uint hash, uint target) { this.idx.RelinkEntry(hash, target); }
        public MemoryStream GetCurrentIDX() { return this.idx.GetStream(); }
    }
}