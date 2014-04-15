using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using GovanifY.Utility;
using KH2FM_Toolkit;
using KHCompress;

namespace IDX_Tools
{
    internal class IDXFile : IDisposable, IEnumerable<IDXFile.IDXEntry>
    {
        private readonly bool leaveOpen;
        private BinaryReader file;

        protected IDXFile()
        {
            file = null;
        }

        public IDXFile(Stream input, bool newidx = false, bool leaveOpen = false)
        {
            file = new BinaryReader(input);
            this.leaveOpen = leaveOpen;
            input.Position = 0;
            if (newidx)
            {
                Count = 0;
                input.Write(new byte[] {0, 0, 0, 0}, 0, 4);
            }
            else
            {
                Count = file.ReadUInt32();
            }
            Position = 0;
        }

        public uint Count { get; private set; }
        public uint Position { get; private set; }

        public void Dispose()
        {
            if (file != null)
            {
                if (!leaveOpen)
                {
                    file.Close();
                }
                file = null;
            }
        }

        public IEnumerator<IDXEntry> GetEnumerator()
        {
            for (uint i = 0; i < Count; ++i)
            {
                yield return ReadEntry(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDXEntry ReadEntry(long index = -1)
        {
            if (index >= 0)
            {
                Position = (uint) index;
            }
            if (Position >= Count)
            {
                return new IDXEntry {Hash = 0};
            }
            file.BaseStream.Position = 4 + 16*Position++;
            var entry = new IDXEntry
            {
                Hash = file.ReadUInt32(),
                HashAlt = file.ReadUInt16(),
                Compression = file.ReadUInt16(),
                DataLBA = file.ReadUInt32(),
                DataLength = file.ReadUInt32()
            };
            return entry;
        }

        private void WriteEntry(IDXEntry entry, uint count = 0)
        {
            using (var bw = new BinaryStream(file.BaseStream, leaveOpen: true))
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
            file.BaseStream.Position = 4;
            for (uint i = 0; i < Count; ++i)
            {
                if (file.ReadUInt32() == hash)
                {
                    file.BaseStream.Position -= 4;
                    return;
                }
                file.BaseStream.Position += 12;
            }
            throw new FileNotFoundException();
        }

        /// <summary>Relink hash to target</summary>
        /// <param name="hash"></param>
        /// <param name="target"></param>
        public void RelinkEntry(uint hash, uint target)
        {
            FindEntryByHash(target);
            file.BaseStream.Position += 4;
            var entry = new IDXEntry
            {
                Hash = hash,
                HashAlt = file.ReadUInt16(),
                Compression = file.ReadUInt16(),
                DataLBA = file.ReadUInt32(),
                DataLength = file.ReadUInt32()
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
            file.BaseStream.Position = 4 + 16*Count;
            WriteEntry(entry, ++Count);
        }

        public class IDXEntry
        {
            /// <summary>
            ///     <para>Compressed length of data, in sectors</para>
            ///     <para>Bit 0x4000 flags if compressed</para>
            /// </summary>
            public ushort Compression = 0;

            /// <summary>Location of data in IMG (LBA)</summary>
            public uint DataLBA;

            /// <summary>Data length (size of file)</summary>
            public uint DataLength;

            /// <summary>
            ///     <para>Identifier of this file</para>
            ///     <para>Hashed filename</para>
            /// </summary>
            public uint Hash;

            /// <summary>
            ///     <para>Secondary identifier of this file</para>
            ///     <para>Hashed filename</para>
            /// </summary>
            public ushort HashAlt = 0;

            /// <summary>Location of data in IMG (bytes)</summary>
            public long Offset
            {
                get { return DataLBA*2048; }
            }

            /// <summary>Returns true if this file is compressed</summary>
            public bool IsCompressed
            {
                get { return (Compression & 0x4000u) == 0x4000u; }
                set { Compression = (ushort) ((value ? 0x4000u : 0) | (Compression & 0xBFFFu)); }
            }

            /// <summary>Returns true if both hashes are checked</summary>
            public bool IsDualHash
            {
                get { return (Compression & 0x8000u) == 0x8000u; }
                set { Compression = (ushort) ((value ? 0x8000u : 0) | (Compression & 0x7FFFu)); }
            }

            /// <summary>Calculated the compressed data size, in bytes</summary>
            public uint CompressedDataLength
            {
                get
                {
                    // KH2 does get this value by and-ing with 0x3FFF, so that's confirmed
                    uint size = (Compression & 0x3FFFu) + 1u;
                    //Fixes file 0x10303F6F:
                    //  Real compressed size = 12,093,440 bytes (Verified manually)
                    //  Compression = 0x4710
                    //  (0x4710 & 0x3FFF) + 1 = 0x711 * 2048 = 3,704,832 + 0x800000 = 12,093,440 bytes
                    //What is happening is the size is getting truncated (see the set function).
                    //0x10303F6F is the only compressed file that hits this limitation officially, and uncompressed files can bypass it (use DataLength).
                    //Also note this increases *all* files about the specified size
                    if (Hash == 0x10303F6F && IsCompressed && DataLength > 0xC00000)
                    {
                        size += 0x1000u;
                    }
                    return size*2048u;
                }
                set
                {
                    if (value == 0)
                    {
                        Compression &= (ushort) 0xC000u;
                        return;
                    }
                    uint size = (uint) Math.Ceiling((double) value/2048) - 1;
                    //Seems that the "size" component just truncated if it's too large, as seen by the larger files (videos, Title.vas)
                    /* Size component:
                     * * Includes at minimum 0x0FFF
                     * * Does NOT include 0x1000 (@noname/10303F6F.bin; vagstream/Title.vas; zmovie/fm/me3.pss; zmovie/fm/opn.pss)
                     * * Does include 0x2000 (zmovie/fm/me3.pss; zmovie/fm/opn.pss)
                     * * 0x4000 is the compression flag
                     * * 0x8000 is the alt hash flag
                     * Nothing has broken these rules, that I can find
                    */
                    Compression = (ushort) ((Compression & 0xC000u) | (size & 0x2FFFu));
                }
            }
        }
    }

    internal class IDXFileWriter
    {
        private readonly List<IDXFile.IDXEntry> entries = new List<IDXFile.IDXEntry>();

        public void AddEntry(IDXFile.IDXEntry entry)
        {
            entries.Add(entry);
        }

        public void RelinkEntry(uint hash, uint target)
        {
            IDXFile.IDXEntry t = entries.Find(e => e.Hash == target);
            if (t.Hash == 0)
            {
                throw new FileNotFoundException();
            }
            entries.Add(new IDXFile.IDXEntry
            {
                Hash = hash,
                HashAlt = 0,
                Compression = t.Compression,
                DataLBA = t.DataLBA,
                DataLength = t.DataLength
            });
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public MemoryStream GetStream()
        {
            entries.Sort((a, b) => a.Hash < b.Hash ? -1 : (a.Hash > b.Hash ? 1 : 0));
            var ms = new MemoryStream();
            try
            {
                using (var bw = new BinaryStream(ms, leaveOpen: true))
                {
                    bw.Write(entries.Count);
                    foreach (IDXFile.IDXEntry entry in entries)
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
        private static readonly byte[] copyBuffer = new byte[2048*2];
        private readonly bool leaveOpen;
        private readonly long offset;
        internal Stream file;

        public IMGFile(Stream file, long offset = 0, bool leaveOpen = false)
        {
            this.file = file;
            this.offset = offset;
            this.leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (file != null)
            {
                if (!leaveOpen)
                {
                    file.Dispose();
                }
                file = null;
            }
        }

        private void ReadFileBuffer(Stream destination, long origin, uint length)
        {
            //Not thread safe, but I'm not using threads
            file.Position = offset + origin;
            int num;
            while (length > 0 && (num = file.Read(copyBuffer, 0, (int) Math.Min(2048*2, length))) != 0)
            {
                destination.Write(copyBuffer, 0, num);
                length -= (uint) num;
            }
        }

        /// <summary>Ensure position is at a 2048 boundary</summary>
        private void EnsureBoundary()
        {
            if (((file.Position - offset)%2048) != 0)
            {
                int rem = 2048 - (int) ((file.Position - offset)%2048);
                byte[] buf = {0, 0, 0, 0};
                while (rem > 3)
                {
                    file.Write(buf, 0, 4);
                    rem -= 4;
                }
                while (--rem >= 0)
                {
                    file.WriteByte(0);
                }
            }
            if (((file.Position - offset)%2048) != 0)
            {
                throw new DataMisalignedException();
            }
        }

        public Substream GetFileStream(IDXFile.IDXEntry entry)
        {
            return new Substream(file, offset + entry.DataLBA*2048,
                entry.IsCompressed ? entry.CompressedDataLength : entry.DataLength);
        }

        public void Seek(uint sector)
        {
            file.Position = offset + sector*2048;
        }
        public void ReadFile(IDXFile.IDXEntry entry, Stream target, bool AdSize)
        {
            if (entry.IsCompressed)
            {
                if (entry.CompressedDataLength > int.MaxValue)
                {
                    throw new NotSupportedException("File to big to decompress");
                }
                var input = new byte[entry.CompressedDataLength];
                Seek(entry.DataLBA);
                file.Read(input, 0, (int) entry.CompressedDataLength);
                try
                {
                    byte[] output = KH2Compressor.decompress(input, entry.DataLength);
                    target.Write(output, 0, output.Length);
                    if (AdSize)
                    {
                        Console.WriteLine("Size (unpacked): {0}\r\n", output.Length);
                    }
                }
                catch (Exception e)
                {
                    Program.WriteError(" ERROR: Failed to decompress: " + e.Message);
                }
            }
            else
            {
                ReadFileBuffer(target, entry.Offset, entry.DataLength);
            }
        }

        public void WriteFile(Stream data)
        {
            if (data.Length > 0xFFFFFFFF)
            {
                throw new NotSupportedException("data too big to store");
            }
            EnsureBoundary();
            data.CopyTo(file);
            EnsureBoundary();
        }

        public void AppendFile(IDXFile.IDXEntry entry, Stream data)
        {
            if (data.Length > 0xFFFFFFFF)
            {
                throw new NotSupportedException("data too big to store");
            }
            file.Seek(0, SeekOrigin.End);
            EnsureBoundary();
            entry.DataLBA = (uint) (file.Position - offset)/2048;
            data.CopyTo(file);
            EnsureBoundary();
        }
    }

    internal class IDXIMGWriter : IDisposable
    {
        private IDXFileWriter idx = new IDXFileWriter();

        public IDXIMGWriter(Stream img, long imgoffset = 0, bool leaveOpen = false)
        {
            this.img = new IMGFile(img, imgoffset);
            this.img = new IMGFile(img, imgoffset, leaveOpen);
        }

        public IMGFile img { get; private set; }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (img != null)
            {
                img.Dispose();
                img = null;
            }
            if (disposing && idx != null)
            {
                idx = null;
            }
        }

        public void AddFile(IDXFile.IDXEntry file, Stream data)
        {
            img.AppendFile(file, data);
            idx.AddEntry(file);
        }

        public void RelinkFile(uint hash, uint target)
        {
            idx.RelinkEntry(hash, target);
        }

        public MemoryStream GetCurrentIDX()
        {
            return idx.GetStream();
        }
    }
}