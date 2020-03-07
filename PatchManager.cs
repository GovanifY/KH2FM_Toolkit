using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using GovanifY.Utility;
using HashList;
using System.Security.Cryptography;

namespace KH2FM_Toolkit
{
    /* KH2 Patch File Format
     * 0    UInt32  Magic 0x5032484B "KH2P"
     * 4    UInt32  0x10000000 + Author Length
     * 8    UInt32  0x10000000 + Author Length + 0x10000000 + Changelog Length + 40000000 + Credits Length + Other Info Length
     * 12   UInt32  Version number of the patch
     * 13   string  Author
     * ?    UInt32  0x0C000000
     * ?    UInt32  0x10000000 + Changelog Length
     * ?    UInt32  0x10000000 + Changelog Length + 40000000 + Credits Length
     * ?    UInt32  Number of lines of the changelog
     *
     *      for each changelog lines:
     *          UInt32  "i" 0x04000000
     *          Increase i by the length of the next line
     *          string Changelog line
     *
     * ?    UInt32  Number of lines of the credits
     *
     *      for each changelog lines:
     *          UInt32  "i" 0x04000000
     *          Increase i by the length of the next line
     *          string Changelog line
     *
     * ?    string  Other infos
     * ?    UInt32  Number of files
     *      i = (Position of the stream of the patch + Number of files) *92
     *       for each non-relinking file:
     *          UInt32  hashed filename
     *          UInt32  i + Compressed size of the file
     *          UInt32  Compressed size of the file
     *          UInt32  Length of the uncompressed file
     *          UInt32  Parent Hash (KH2, OVL, etc...)
     *          UInt32  0x00000000
     *          UInt32  If file is compressed 0x01000000, otherwise 0x00000000
     *          UInt32  If file should be added if he's not in the game 0x01000000, otherwise 0x00000000
     *          UInt32(x15) 0x00000000(padding)
     *          byte*?  Raw file data
     *
     *      for each relinked file:
     *          UInt32  0x00000000
     *          UInt32  0x00000000
     *          UInt32  0x00000000
     *          UInt32 Hash of the file
     *          UInt32 Hash of the filename to relink to
     *          UInt32  0x00000000
     *
     *
     * Notes:
     * All files which needs to be compressed are already compressed into the patch file
     * Relinking a file will copy the content of the original file to the new file
     */
    public sealed class PatchManager : IDisposable
    {
        private readonly List<Stream> patchms = new List<Stream>();

        /// <summary>Mapping of Parent IDX -> new children hashes</summary>
        internal Dictionary<uint, List<uint>> newfiles = new Dictionary<uint, List<uint>>();

        /// <summary>Mapping of hash->Patch</summary>
        internal Dictionary<uint, Patch> patches = new Dictionary<uint, Patch>();

        internal PatchManager()
        {
            ISOChanged = OVLChanged = KH2Changed = false;
        }

        public bool fast_patch=false;
        public bool ISOChanged { get; private set; }
        public bool OVLChanged { get; private set; }
        public bool KH2Changed { get; private set; }

        public void Dispose()
        {
            foreach (var patch in patches)
            {
                patch.Value.Dispose();
            }
            patches.Clear();
            foreach (Stream ms in patchms)
            {
                ms.Dispose();
            }
            patchms.Clear();
        }
        public static bool XorSig(byte[] buffer, byte[] xorSource)
        {
            if (xorSource == null)
            {
                throw new ArgumentNullException("xorSource");
            }
            int num = buffer.Length;
            int num2 = xorSource.Length - 1;
            if (num2 < 0)
            {
                throw new ArgumentException("xorSource", "XOR array is empty.");
            }
            uint num3 = 0u;
            byte b = 0;
            while (b < 4 && num > 0)
            {
                num3 |= (uint)((uint)(buffer[(int)b] ^ xorSource[--num & num2]) << (int)(b * 8));
                b += 1;
            }
            if (num3 == 1345472587u || num3 == 1362249803u)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        byte[] ODXY = { 0x58, 0x0c, 0xdd, 0x59, 0xf7, 0x24, 0x7f, 0x4f };
        byte[] OGYX = { 164, 28, 107, 129, 48, 13, 35, 91, 92, 58, 167, 222, 219, 244, 115, 90, 160, 194, 112, 209, 40, 72, 170, 114, 98, 181, 154, 124, 124, 32, 224, 199, 34, 32, 114, 204, 38, 198, 188, 128, 45, 120, 181, 149, 219, 55, 33, 116, 6, 17, 181, 125, 239, 137, 72, 215, 1, 167, 110, 208, 110, 238, 124, 204 };
        public static void XeeyXor(byte[] buffer)
        {
            byte[] v84 = {0x58, 0x0c, 0xdd, 0x59, 0xf7, 0x24, 0x7f, 0x4f};
            int i = -1, l = buffer.Length;
            while (l > 0)
            {
                buffer[++i] ^= v84[(--l & 7)];
            }
        }
        public static void GYXor(byte[] buffer)
        {
            byte[] v84 = { 164, 28, 107, 129, 48, 13, 35, 91, 92, 58, 167, 222, 219, 244, 115, 90, 160, 194, 112, 209, 40, 72, 170, 114, 98, 181, 154, 124, 124, 32, 224, 199, 34, 32, 114, 204, 38, 198, 188, 128, 45, 120, 181, 149, 219, 55, 33, 116, 6, 17, 181, 125, 239, 137, 72, 215, 1, 167, 110, 208, 110, 238, 124, 204 };
            int i = -1, l = buffer.Length;
            while (l > 0)
            {
                buffer[++i] ^= v84[(--l & 63)];
            }
        }

        public static uint ToHash(string name)
        {
            uint v0 = uint.MaxValue;
            foreach (char c in name)
            {
                v0 ^= ((uint) c << 24);
                for (int i = 9; --i > 0;)
                {
                    if ((v0 & 0x80000000u) != 0)
                    {
                        v0 = (v0 << 1) ^ 0x04C11DB7u;
                    }
                    else
                    {
                        v0 <<= 1;
                    }
                }
            }
            return ~v0;
        }

        public static ushort ToHashAlt(string name)
        {
            ushort s1 = ushort.MaxValue;
            for (int j = name.Length; --j >= 0;)
            {
                s1 ^= (ushort) (name[j] << 8);
                for (int i = 9; --i > 0;)
                {
                    if ((s1 & 0x8000u) != 0)
                    {
                        s1 = (ushort) ((s1 << 1) ^ 0x1021u);
                    }
                    else
                    {
                        s1 <<= 1;
                    }
                }
            }
            return (ushort) ~s1;
        }

        internal void AddToNewFiles(Patch nPatch)
        {
            nPatch.IsNew = true;
            if (!newfiles.ContainsKey(nPatch.Parent))
            {
                newfiles.Add(nPatch.Parent, new List<uint>(1));
            }
            if (!newfiles[nPatch.Parent].Contains(nPatch.Hash))
            {
                newfiles[nPatch.Parent].Add(nPatch.Hash);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void AddPatch(Stream ms, string patchname = "")
        {
            using (var br = new BinaryStream(ms, Encoding.ASCII, leaveOpen: true))
            {
                if (br.ReadUInt32() != 0x5032484b)
                {
                    br.Seek(0, SeekOrigin.Begin);
                    if (br.ReadUInt32() != 0x5132484b)
                    {
                        br.Seek(0, SeekOrigin.Begin);
                        if (br.ReadUInt32() != 0x4632484b)
                        {
                            br.Close();
                            ms.Close();
                            throw new InvalidDataException("Invalid KH2Patch file!");
                        }
                        else
                        {
                            fast_patch=true;
                            Console.WriteLine("Fast patch and dev flags detected! You might get some issues with those!");
                        }
                    }
                }
                patchms.Add(ms);
                uint oaAuther = br.ReadUInt32(),
                     obFileCount = br.ReadUInt32(),
                     num = br.ReadUInt32();
                patchname = Path.GetFileName(patchname);
                try
                {
                    string author = br.ReadCString();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Loading patch {0} version {1} by {2}", patchname, num, author);
                    Console.ResetColor();
                    br.Seek(oaAuther, SeekOrigin.Begin);
                    uint os1 = br.ReadUInt32(),
                         os2 = br.ReadUInt32(),
                         os3 = br.ReadUInt32();
                    br.Seek(oaAuther + os1, SeekOrigin.Begin);
                    num = br.ReadUInt32();
                    if (num > 0)
                    {
                        br.Seek(num*4, SeekOrigin.Current);
                        Console.WriteLine("Changelog:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            Console.WriteLine(" * {0}", br.ReadCString());
                        }
                    }
                    br.Seek(oaAuther + os2, SeekOrigin.Begin);
                    num = br.ReadUInt32();
                    if (num > 0)
                    {
                        br.Seek(num*4, SeekOrigin.Current);
                        Console.ResetColor();
                        Console.WriteLine("Credits:");
                        Console.ForegroundColor = ConsoleColor.Green;
                        while (num > 0)
                        {
                            --num;
                            Console.WriteLine(" * {0}", br.ReadCString());
                        }
                        Console.ResetColor();
                    }
                    br.Seek(oaAuther + os3, SeekOrigin.Begin);
                    author = br.ReadCString();
                    if (author.Length != 0)
                    {
                        Console.WriteLine("Other information:\r\n");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("{0}", author);
                    }
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error reading kh2patch header: {0}: {1}\r\nAttempting to continue files...",
                            e.GetType(), e.Message);
                    Console.ResetColor();
                }
                Console.WriteLine("");
                br.Seek(obFileCount, SeekOrigin.Begin);
                num = br.ReadUInt32();
                while (num > 0)
                {
                    --num;
                    var nPatch = new Patch();
                    nPatch.Hash = br.ReadUInt32();
                    oaAuther = br.ReadUInt32();
                    nPatch.CompressedSize = br.ReadUInt32();
                    nPatch.UncompressedSize = br.ReadUInt32();
                    nPatch.Parent = br.ReadUInt32();
                    nPatch.Relink = br.ReadUInt32();
                    nPatch.Compressed = br.ReadUInt32() != 0;
                    nPatch.IsNew = br.ReadUInt32() == 1; //Custom
                    if (!nPatch.IsRelink)
                    {
                        if (nPatch.CompressedSize != 0)
                        {
                            nPatch.Stream = new Substream(ms, oaAuther, nPatch.CompressedSize);
                        }
                        else
                        {
                            throw new InvalidDataException("File length is 0, but not relinking.");
                        }
                    }
                    // Use the last file patch
                    if (patches.ContainsKey(nPatch.Hash))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
#if DEBUG
                        Console.WriteLine("The file {0} has been included multiple times. Using the one from {1}.",
                                HashList.HashList.NameFromHash(nPatch.Hash), patchname);
#endif
                        patches[nPatch.Hash].Dispose();
                        patches.Remove(nPatch.Hash);
                        Console.ResetColor();
                    }
                    patches.Add(nPatch.Hash, nPatch);
                    //Global checks
                    if (!KH2Changed && nPatch.IsInKH2 || nPatch.IsInKH2Sub)
                    {
                        KH2Changed = true;
                    }
                    else if (!OVLChanged && nPatch.IsInOVL)
                    {
                        OVLChanged = true;
                    }
                    else if (!ISOChanged && nPatch.IsinISO)
                    {
                        ISOChanged = true;
                    }
                    if (nPatch.IsNew)
                    {
                        AddToNewFiles(nPatch);
                    }
                    br.Seek(60, SeekOrigin.Current);
                }
            }
        }

        public void AddPatch(string patchname)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(patchname, FileMode.Open, FileAccess.Read, FileShare.Read);
                int num;
                if (fs.ReadByte() == 0x4B && fs.ReadByte() == 0x48 && fs.ReadByte() == 0x32 && ((num = fs.ReadByte()) == 0x50 || num == 0x51 || num == 0x46))
                {
                    fs.Position = 0;
                    AddPatch(fs, patchname);
                    return;
                }
                if (fs.Length > int.MaxValue)
                {
                    throw new OutOfMemoryException("File too large");
                }

                try
                {
                    fs.Position = 0;
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
                    if (XorSig(buffer, OGYX))
                    {
                        GYXor(buffer);
                        AddPatch(new MemoryStream(buffer), patchname);
                    }
                    else
                    {
                        throw new Exception();
                    }
#if DEBUG
                    Program.WriteWarning("Old format is used, Please use the new one!");
#endif
                }
                catch (Exception)
                {
                    fs.Position = 0;
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, (int)fs.Length);
                    if (XorSig(buffer, ODXY))
                    {
                        XeeyXor(buffer);
                        AddPatch(new MemoryStream(buffer), patchname);
                    }
                    else
                    {
                        throw;
                    }
#if DBEUG
                    Program.WriteWarning("Old format is used, Please use the new one!");
#endif
                }
                finally
                {
                    fs.Dispose();
                    fs = null;
                }
            }
            catch (Exception e)
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
                Console.WriteLine("Failed to parse patch: {0}", e.Message);
            }
        }

        internal class Patch : IDisposable
        {
            public bool Compressed;
            public uint CompressedSize;
            public uint Hash;
            public bool IsNew;
            public uint Parent;
            public uint Relink;
            public Substream Stream;
            public uint UncompressedSize;

            public bool IsInKH2
            {
                get { return Parent == 0; }
            }

            public bool IsInOVL
            {
                get { return Parent == 1; }
            }

            public bool IsinISO
            {
                get { return Parent == 2; }
            }

            public bool IsInKH2Sub
            {
                get
                {
                    switch (Parent)
                    {
                        case 0x0499386d: //000hb.idx
                        case 0x0b2025ed: //000mu.idx
                        case 0x2b87c9dc: //000di.idx
                        case 0x2bb6ecb2: //000ca.idx
                        case 0x35f6154a: //000al.idx
                        case 0x3604eeef: //000tt.idx
                        case 0x43887a92: //000po.idx
                        case 0x4edb9e9e: //000gumi.idx
                        case 0x608e02b4: //000es.idx
                        case 0x60dd6d06: //000lk.idx
                        case 0x79a2a329: //000eh.idx
                        case 0x84eaa276: //000tr.idx
                        case 0x904a97e0: //000wm.idx
                        case 0xb0be1463: //000wi.idx
                        case 0xd233219f: //000lm.idx
                        case 0xe4633b6f: //000nm.idx
                        case 0xeb89495d: //000bb.idx
                        case 0xf87401c0: //000dc.idx
                        case 0xff7a1379: //000he.idx
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public bool IsRelink
            {
                get { return Relink != 0; }
            }

            public void Dispose()
            {
                if (Stream != null)
                {
                    Stream.Dispose();
                    Stream = null;
                }
            }
        }
    }
}
