using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GovanifY.Utility;
using HashList;
using IDX_Tools;
using ISO_Tools;
using KH2FM_Toolkit.Properties;
using KHCompress;

namespace KH2FM_Toolkit
{
    public static class Program
    {
        /// <summary>
        ///     <para>Sector size of the ISO</para>
        ///     <para>Almost always 2048 bytes</para>
        /// </summary>
        public const int SectorSize = 2048;

        public static readonly FileVersionInfo program =
            FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        private static readonly PatchManager Patches = new PatchManager();
        private static bool _advanced;
        private static bool k2e;

        private static DateTime Builddate { get; set; }

        private static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = Assembly.GetCallingAssembly().Location;
            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;
            var b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = BitConverter.ToInt32(b, cPeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        public static void WriteWarning(string format, params object[] arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }

        public static void WriteError(string format, params object[] arg)
        {
            WriteWarning(format, arg);
            //Let the user see the error
            Console.Write(@"Press enter to continue anyway... ");
            Console.ReadLine();
        }

        /// <param name="idx">Left open.</param>
        /// <param name="img">Left open.</param>
        /// <param name="recurse">recursive</param>
        /// <param name="tfolder">Complete name</param>
        /// <param name="name">Complete name</param>
        private static void ExtractIDX(IDXFile idx, Stream img, bool recurse = false, string tfolder = "export/",
            string name = "")
        {
            using (var imgf = new IMGFile(img, leaveOpen: true))
            {
                var idxs = new List<Tuple<IDXFile, string>>();
                uint i = 0, total = idx.Count;
                foreach (IDXFile.IDXEntry entry in idx)
                {
                    string filename = entry.FileName();
                    if (recurse)
                    {
                        switch (entry.Hash)
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
                                idxs.Add(new Tuple<IDXFile, string>(new IDXFile(imgf.GetFileStream(entry)),
                                    Path.GetFileNameWithoutExtension(filename).Substring(3)));
                                Debug.WriteLine("  Added IDX to list");
                                break;
                        }
                    }
                    if (_advanced)
                    {
                        if (name == "KH2")
                        {
                            Console.WriteLine("-----------File {0,4}/{1}, using {2}.IDX\n", ++i, total, name);
                        }
                        else
                        {
                            if (name == "OVL")
                            {
                                Console.WriteLine("-----------File {0,4}/{1}, using {2}.IDX\n", ++i, total, name);
                            }
                            else
                            {
                                Console.WriteLine("-----------File {0,4}/{1}, using 000{2}.idx\n", ++i, total, name);
                            }
                        }
                        Console.WriteLine("Dual Hash flag: {0}", entry.IsDualHash); //Always false but anyways
                        Console.WriteLine("Hashed filename: {0}\nHashAlt: {1}", entry.Hash, entry.HashAlt);
                        Console.WriteLine("Compression flags: {0}", entry.IsCompressed);
                        Console.WriteLine("Size (packed): {0}", entry.CompressedDataLength);
                        Console.WriteLine("Real name: {0}", filename);
                    }
                    else
                    {
                        Console.WriteLine("[{2}: {0,4}/{1}]\tExtracting {3}", ++i, total, name, filename);
                    }
                    filename = Path.GetFullPath(tfolder + filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    using (var output = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        bool adSize = _advanced;
                        imgf.ReadFile(entry, output, adSize);
                    }
                }
                if (recurse && idxs.Count != 0)
                {
                    foreach (var sidx in idxs)
                    {
                        ExtractIDX(sidx.Item1, img, false, tfolder, sidx.Item2);
                    }
                }
            }
        }

        private static void KH2PATCHInternal(Substream KH2PFileStream, string fname2, bool Compressed, UInt32 UncompressedSize)
        {
            try {Directory.CreateDirectory(Path.GetDirectoryName(fname2)); }catch{}//Creating folder
            FileStream fileStream = File.Create(fname2);
            byte[] buffer = new byte[KH2PFileStream.Length];
            byte[] buffer2 = new byte[UncompressedSize];
            var file3 = new MemoryStream();
           if (Compressed)
            {
                KH2PFileStream.CopyTo(file3);
                buffer = file3.ToArray();
                buffer2 = KH2Compressor.decompress(buffer, UncompressedSize); // Will crash if the byte array is equal to void.
                file3 = new MemoryStream (buffer2);
            }
            else
           {
               KH2PFileStream.CopyTo(file3);
           }
            file3.CopyTo(fileStream);
            Console.WriteLine("Done!");
        }

        private static void KH2PatchExtractor(Stream patch)
        {
            using (var br = new BinaryStream(patch, Encoding.ASCII, leaveOpen: true))
            {
                if (br.ReadUInt32() != 0x5032484b)
                {
                    br.Close();
                    br.Close();
                    throw new InvalidDataException("Invalid KH2Patch file!");
                }
                uint oaAuther = br.ReadUInt32(),
                    obFileCount = br.ReadUInt32(),
                    num = br.ReadUInt32();
                string patchname = "";
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
                    uint Hash = br.ReadUInt32();
                    oaAuther = br.ReadUInt32();
                    uint CompressedSize = br.ReadUInt32();
                    uint UncompressedSize = br.ReadUInt32();
                    uint Parent = br.ReadUInt32();
                    uint Relink = br.ReadUInt32();
                    bool Compressed = br.ReadUInt32() != 0;
                    bool IsNew = br.ReadUInt32() == 1; //Custom
                    if (Relink == 0)
                    {
                        if (CompressedSize != 0)
                        {

                            var KH2PFileStream = new Substream(patch, oaAuther, CompressedSize);
                            string fname2;
                            HashList.HashList.pairs.TryGetValue(Hash, out fname2);
                            Console.Write("Extracting {0}...", fname2);
                            var brpos = br.Tell();
                            KH2PATCHInternal(KH2PFileStream, fname2, Compressed, UncompressedSize);
                            br.ChangePosition((int)brpos); //Changing the original position of the BinaryReader for what's next
                        }
                        else
                        {
                            throw new InvalidDataException("File length is 0, but not relinking.");
                        }
                    }
                    else
                    {
                        string fname3;
                        if (!HashList.HashList.pairs.TryGetValue(Relink, out fname3))
                        {
                            fname3 = String.Format("@noname/{0:X8}.bin", Relink);
                        }
                        Console.WriteLine("File relinked to {0}, no need to extract", fname3);
                    }
                    br.Seek(60, SeekOrigin.Current);
                }
            }//End of br
        }

        private static void ExtractISO(Stream isofile, string tfolder = "export/")
        {
            using (var iso = new ISOFileReader(isofile))
            {
                var idxs = new List<IDXFile>();
                var idxnames = new List<string>();
                int i = 0;
                foreach (FileDescriptor file in iso)
                {
                    ++i;
                    string filename = file.FullName;
                    if (filename.EndsWith(".IDX"))
                    {
                        idxs.Add(new IDXFile(iso.GetFileStream(file)));
                        idxnames.Add(Path.GetFileNameWithoutExtension(filename));
                        //continue;
                        //Write the IDX too
                    }
                    else if (filename.EndsWith(".IMG") && idxnames.Contains(Path.GetFileNameWithoutExtension(filename)))
                    {
                        continue;
                    }
                    Console.WriteLine("[ISO: {0,3}]\tExtracting {1}", i, filename);
                    filename = Path.GetFullPath(tfolder + "ISO/" + filename);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    }
                    catch (IOException e)
                    {
                        WriteError("Failed creating directory: {0}", e.Message);
                        continue;
                    }
                    using (var output = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        iso.CopyFile(file, output);
                    }
                }
                for (i = 0; i < idxs.Count; ++i)
                {
                    try
                    {
                        FileDescriptor file = iso.FindFile(idxnames[i] + ".IMG");
                        using (Substream img = iso.GetFileStream(file))
                        {
                            ExtractIDX(idxs[i], img, true, tfolder + "" + idxnames[i] + "/", idxnames[i]);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        WriteError("ERROR: Failed to find matching IMG for IDX");
                    }
                }
            }
        }

        /// <param name="sidx">Stream of the original idx.</param>
        /// <param name="simg">Stream of the original img.</param>
        /// <param name="timg">img of the new iso.</param>
        /// <param name="imgOffset">Offset of the new img in the new iso.</param>
        /// <param name="parenthash">Parent Hash(KH2 or OVL or 000's)</param>
        private static MemoryStream PatchIDXInternal(Stream sidx, Stream simg, Stream timg, long imgOffset,
            uint parenthash = 0)
        {
            //Generate Parent name
            string parentname;
            if (parenthash == 0)
            {
                parentname = "KH2";
            }
            else if (parenthash == 1)
            {
                parentname = "OVL";
            }
            else if (HashList.HashList.pairs.TryGetValue(parenthash, out parentname))
            {
                parentname = parentname.Substring(3, parentname.IndexOf('.') - 3);
            }
            else
            {
                parentname = parenthash.ToString("X8");
            }
            //Need more using
            using (var idx = new IDXFile(sidx, leaveOpen: true))
            using (var img = new IMGFile(simg, leaveOpen: true))
            using (var npair = new IDXIMGWriter(timg, imgOffset, true))
            {
                uint i = 0, total = idx.Count;
                foreach (IDXFile.IDXEntry file in idx)
                {
                    Console.Write("[{0}: {1,4}/{2}]\t{3}", parentname, ++i, total, file.FileName());
                    switch (file.Hash)
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
                            Console.WriteLine("\tRe-Building...");
                            using (Substream oidx = img.GetFileStream(file))
                            using (MemoryStream subidx = PatchIDXInternal(oidx, simg, timg, imgOffset, file.Hash))
                            {
                                npair.AddFile(new IDXFile.IDXEntry
                                {
                                    Hash = file.Hash,
                                    HashAlt = file.HashAlt,
                                    IsDualHash = file.IsDualHash,
                                    DataLength = (uint) subidx.Length,
                                    IsCompressed = false,
                                    CompressedDataLength = (uint) subidx.Length
                                }, subidx);
                            }
                            continue;
                    }
                    PatchManager.Patch patch;
                    // Could make sure the parents match perfectly, but there's only 1 of every name anyway.
                    // So I'll settle for just making sure the file isn't made for the ISO.
                    if (Patches.patches.TryGetValue(file.Hash, out patch) && /*patch.Parent == parenthash*/
                        !patch.IsinISO)
                    {
                        patch.IsNew = false;
                        if (patch.IsRelink)
                        {
                            try
                            {
                                npair.RelinkFile(file.Hash, patch.Relink);
                                Console.WriteLine("\tRelinking...");
                            }
                            catch
                            {
                                Console.WriteLine("\tDeferred Relinking...");
                                // Add the patch to be processed later, in the new file block
                                patch.Parent = parenthash;
                                Patches.AddToNewFiles(patch);
                            }
                            continue;
                        }
                        Console.WriteLine("\tPatching...");
#if extract
                        Console.WriteLine("\nEXTRACTING THE FILE!");
                        Console.WriteLine("\nGetting the name...");
                        string fname2;
                        HashList.HashList.pairs.TryGetValue(file.Hash, out fname2);
                        Console.WriteLine("\nCreating directory...");
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fname2));
                        }
                        catch
                        {
                            Console.WriteLine("\nDirectory is surely null. Trying to create the file anyways...");
                        }
                        Console.WriteLine("\nCreating the file...");
                        FileStream fileStream = File.Create(fname2);
                        Console.WriteLine("\nConverting the stream to a byte[]...");
                        patch.Stream.Position = 0;
                        var buffer = new byte[patch.Stream.Length];
                        for (int totalBytesCopied = 0; totalBytesCopied < patch.Stream.Length;)
                            totalBytesCopied += patch.Stream.Read(buffer, totalBytesCopied,
                                Convert.ToInt32(patch.Stream.Length) - totalBytesCopied);
                        Console.WriteLine("\nConverting the int to uint...");
                        byte[] file2;
                        if (patch.Compressed)
                        {
                            Console.WriteLine("\nThe file is compressed!");
                            Console.WriteLine("\nDecompressing the file...");
                            file2 = KH2Compressor.decompress(buffer, patch.UncompressedSize);
                        }
                        else
                        {
                            file2 = buffer;
                        }
                        Console.WriteLine("\nOpening the stream for the file..");
                        Stream decompressed = new MemoryStream(file2);
                        Console.WriteLine("\nCopying the Stream...");
                        decompressed.CopyTo(fileStream);
                        Console.WriteLine("\nDone!...");
#endif
                        try
                        {
                            npair.AddFile(new IDXFile.IDXEntry
                            {
                                Hash = file.Hash,
                                HashAlt = file.HashAlt,
                                IsDualHash = file.IsDualHash,
                                DataLength = patch.UncompressedSize,
                                IsCompressed = patch.Compressed,
                                CompressedDataLength = patch.CompressedSize
                            }, patch.Stream);
                            continue;
                        }
                        catch (Exception e)
                        {
                            WriteError(" ERROR Patching: " + e.Message);
#if DEBUG
                            WriteError(e.StackTrace);
#endif
                        }
                    }
                    Console.WriteLine("");
                    npair.AddFile(file, img.GetFileStream(file));
                }
                //Check for new files to add
                List<uint> newfiles;
                if (Patches.newfiles.TryGetValue(parenthash, out newfiles))
                {
                    foreach (uint hash in newfiles)
                    {
                        PatchManager.Patch patch;
                        if (Patches.patches.TryGetValue(hash, out patch) && patch.IsNew)
                        {
                            patch.IsNew = false;
                            string fname;
                            if (!HashList.HashList.pairs.TryGetValue(hash, out fname))
                            {
                                fname = String.Format("@noname/{0:X8}.bin", hash);
                            }
                            Console.Write("[{0}: NEW]\t{1}", parentname, fname);
                            try
                            {
                                if (patch.IsRelink)
                                {
                                    Console.WriteLine("\tAdding link...");
                                    npair.RelinkFile(hash, patch.Relink);
                                }
                                else
                                {
                                    Console.WriteLine("\tAdding file...");
                                    npair.AddFile(new IDXFile.IDXEntry
                                    {
                                        Hash = hash,
                                        HashAlt = 0,
                                        IsDualHash = false,
                                        DataLength = patch.UncompressedSize,
                                        IsCompressed = patch.Compressed,
                                        CompressedDataLength = patch.CompressedSize
                                    }, patch.Stream);
                                }
                            }
                            catch (FileNotFoundException)
                            {
                                Console.WriteLine(" WARNING Failed to find the file to add!");
                            }
                            catch (Exception e)
                            {
                                WriteError(" ERROR adding file: {0}", e.Message);
                            }
                        }
                    }
                }
                return npair.GetCurrentIDX();
            }
        }

        /// <param name="idx">Stream of the idx inside the iso.</param>
        /// <param name="img">Stream of the img inside the iso.</param>
        /// <param name="imgd">File descriptor of the img file.</param>
        /// <param name="niso">New ISO.</param>
        /// <param name="IsOVL">Bool which define if the OVL should be patched</param>
        private static MemoryStream PatchIDX(Stream idx, Stream img, FileDescriptor imgd, ISOCopyWriter niso,
            bool IsOVL = false)
        {
            using (idx)
            using (img)
            {
                niso.SeekEnd();
                long imgOffset = niso.file.Position;
                MemoryStream idxms = PatchIDXInternal(idx, img, niso.file, imgOffset, IsOVL ? 1u : 0u);
                imgd.ExtentLBA = (uint) imgOffset/2048;
                imgd.ExtentLength = (uint) (niso.file.Position - imgOffset);
                imgd.RecordingDate = DateTime.UtcNow;
                niso.PatchFile(imgd);
                niso.SeekEnd();
                return idxms;
            }
        }

        /// <param name="isofile">Original ISO</param>
        /// <param name="nisofile">New ISO file</param>
        private static void PatchISO(Stream isofile, Stream nisofile)
        {
            using (var iso = new ISOFileReader(isofile))
            using (var niso = new ISOCopyWriter(nisofile, iso))
            {
                uint i = 0;
                Trivalent cKh2 = Patches.KH2Changed ? Trivalent.ChangesPending : Trivalent.NoChanges,
                    cOvl = Patches.OVLChanged ? Trivalent.ChangesPending : Trivalent.NoChanges;
                bool cIso = Patches.ISOChanged;
                foreach (FileDescriptor file in iso)
                {
                    Console.Write("[ISO: {0,4}]\t{1}", ++i, file.FullName);
                    string name = file.FileName;
                    if (name.EndsWith("KH2.IDX") || name.EndsWith("KH2.IMG"))
                    {
                        if (cKh2.HasFlag(Trivalent.ChangesPending))
                        {
                            cKh2 = Trivalent.Changed;
                            long lpos = niso.file.Position;
                            Console.WriteLine("\tRe-Building...");
                            try
                            {
                                FileDescriptor img = iso.FindFile("KH2.IMG"),
                                    idx = iso.FindFile("KH2.IDX");
                                using (
                                    MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso)
                                    )
                                {
                                    idx.RecordingDate = DateTime.UtcNow;
                                    niso.AddFile2(idx, ms, name);
                                }
                                continue;
                            }
                            catch (Exception e)
                            {
                                WriteError(" Error creating IDX/IMG: {0}\n{1}", e.Message, e.StackTrace);
                                niso.file.Position = lpos;
                            }
                        }
                        else if (cKh2.HasFlag(Trivalent.Changed))
                        {
                            Console.WriteLine("\tRe-Built");
                            continue;
                        }
                    }
                    else if (name.EndsWith("OVL.IDX") || name.EndsWith("OVL.IMG"))
                    {
                        if (cOvl.HasFlag(Trivalent.ChangesPending))
                        {
                            cOvl = Trivalent.Changed;
                            long lpos = niso.file.Position;
                            Console.WriteLine("\tRe-Building...");
                            try
                            {
                                FileDescriptor img = iso.FindFile("OVL.IMG"),
                                    idx = iso.FindFile("OVL.IDX");
                                using (
                                    MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso,
                                        true))
                                {
                                    idx.RecordingDate = DateTime.UtcNow;
                                    niso.AddFile2(idx, ms, name);
                                }
                                continue;
                            }
                            catch (Exception e)
                            {
                                WriteError(" Error creating IDX/IMG: " + e.Message);
                                niso.file.Position = lpos;
                            }
                        }
                        else if (cOvl.HasFlag(Trivalent.Changed))
                        {
                            Console.WriteLine("\tRe-Built");
                            continue;
                        }
                    }
                    else if (cIso)
                    {
                        PatchManager.Patch patch;
                        if (Patches.patches.TryGetValue(PatchManager.ToHash(name), out patch) && patch.IsinISO)
                        {
                            Console.WriteLine("\tPatching...");
                            file.RecordingDate = DateTime.UtcNow;
                            niso.AddFile2(file, patch.Stream, name);
                            continue;
                        }
                    }
                    Console.WriteLine("");
                    niso.CopyFile(file);
                    if (niso.SectorCount >= 0x230540)
                    {
                        WriteWarning(
                            "Warning: This ISO has the size of a dual-layer ISO, but it isn't one. Some\nprograms may take a while to start while they search for the 2nd layer.");
                    }
                }
            }
        }

        /// <summary>The main entry point for the application.</summary>
        /// <exception cref="Exception">Cannot delete debug.log</exception>
        private static void Main(string[] args)
        {
            bool log = false;
            Console.Title = program.ProductName + " " + program.FileVersion + " [" + program.CompanyName + "]";
#if DEBUG
            try
            {
                File.Delete("debug.log");
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot delete debug.log!: {0}", e);
                throw new Exception();
            }
            Debug.AutoFlush = true;
            Debug.Listeners.Add(new TextWriterTraceListener("debug.log"));
#endif
            //Arguments
            string isoname = null;
            bool batch = false, extract = false;
            bool verify = false;

            #region Arguments

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-exit":
                        return;
                    case "-batch":
                        batch = true;
                        break;
                    case "-extractor":
                        extract = true;
                        break;
                    case "-advancedinfo":
                        _advanced = true;
                        break;
                    case "-log":
                        log = true;
                        break;
                    case "-kh2patchextractor":
                    case "-k2e":
                        k2e = true;
                        break;
                    case "-verifyiso":
                        verify = true;
                        break;
                    case "-help":
                        byte[] buffer = Encoding.ASCII.GetBytes(Resources.Readme);
                        File.WriteAllBytes("Readme.txt", buffer);
                        Console.Write("Help extracted as a Readme\nPress enter to leave the software...");
                        Console.Read();
                        return;
                    case "-license":
                        byte[] buffer2 = Encoding.ASCII.GetBytes(Resources.LICENSE);
                        File.WriteAllBytes("LICENSE.TXT", buffer2);
                        Console.Write("License extracted as the file LICENSE.TXT\nPress enter to leave the software...");
                        Console.Read();
                        return;
                    case "-patchmaker":
                        KH2ISO_PatchMaker.Program.Mainp(args);
                        break;
                    default:
                        if (File.Exists(arg))
                        {
                            if (k2e)
                            {
                                if (isoname == null &&
                                    arg.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    isoname = arg;
                                }
                                else if (isoname == null &&
                                         arg.EndsWith(".iso", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    isoname = arg;
                                }
                                else if (arg.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    Patches.AddPatch(arg);
                                }
                            }
                        }

                        break;
                }
            } //TODO patch after header

            #endregion Arguments

            #region Description

            if (log)
            {
                var filestream = new FileStream("log.log", FileMode.Create);
                var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
                //TODO Redirect to a txt, but problem: make disappear the text on the console. Need to mirror the text
            }
            if (isoname == null)
            {
                isoname = "KH2FM.ISO";
            }
            Console.ForegroundColor = ConsoleColor.Gray;
            Builddate = RetrieveLinkerTimestamp();
            Console.Write("{0}\nBuild Date: {2}\nVersion {1}", program.ProductName, program.FileVersion, Builddate);
            string Platform;
            if (IntPtr.Size == 8)
            {
                Platform = "x64";
            }
            else
            {
                Platform = "x86";
            }
            Console.Write("\n{0} build", Platform);
            Console.ResetColor();
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nPRIVATE RELEASE\n");
            Console.ResetColor();
#else
                Console.Write("\nPUBLIC RELEASE\n");
#endif
#if NODECOMPRESS
                                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nNODECOMPRESS edition: Decompress algo is returning the input.\n");
                Console.ResetColor();
#endif
#if extract
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nKH2PATCH EXTRACTOR edition: Extract the kh2patch when during process.\n");
            Console.ResetColor();
#endif

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.Write(
                "\nProgrammed by {0}\nhttp://www.govanify.blogspot.fr\nhttp://www.govanify.x10host.com\nSoftware under GPL 2 license, for more info, use the command -license",
                program.CompanyName);
            Console.ForegroundColor = ConsoleColor.Gray;
            if (extract)
            {
                Console.Write(
                    "\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nHe is using a list for extracting those files, which is not complete.\nBut this is the most complete one for now.\nHe can extract the files KH2.IMG and OVL.IMG\n\n");
            }
            else
            {
                if (k2e)
                {
                    Console.Write(
                        "\n\nThis tool is able to extract kh2patch files, it will require an authorization\nto access to the tool though\n");
                }
                if (!k2e)
                {
                    Console.Write(verify
                        ? "\n\nThis tool will calculate the hash of your iso for verify if it's a good dump of KH2(FM) or not.\n\n"
                        : "\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nHe can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files for avoid\na corrupted game. He can add some files too.\n\n");
                }
            }
            HashList.HashList.loadHashPairs(printInfo: true);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nPress enter to run using the file:");
            Console.ResetColor();
            Console.Write(" {0}", isoname);
            if (!batch)
            {
                Console.ReadLine();
            }

            #endregion Description

            #region SHA1

            if (verify)
            {
                Console.Write("Calculating the SHA1 hash of your iso. Please wait...\n");
                using (SHA1 sha1 = SHA1.Create())
                {
                    using (FileStream stream = File.OpenRead(isoname))
                    {
                        //List of all SHA1 hashes of KH2 games
                        string isouser = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
                        const string KH2FMiso = "81c177a374e1bddf8453c8c96493d4e715a19236";
                        const string KH2UKiso = "f541888cf953559bf4ef8c7e51a822f50e05265c";
                        const string KH2FRiso = "70f69a59ba47edae5d41dec7396af0d747e92131";
                        const string KH2GRiso = "1a03ffe1e3db3c5f920dd2f1a5e90f38783da43b";
                        const string KH2ITiso = "36cfb0e3ee615b9228ec9a949d4d41cc0edf2e3a";
                        const string KH2JAPiso = "678e1c6545e6d5ef7ed3e6c496b12f5dd2ecbe56";
                        const string KH2ESiso = "d190089a0718a7c204ac256ddedc564d600731f3";
                        const string KH2USiso = "7e57081735d82fd84be7f79ab05ad3e795bc7e5e";
                        const string KH2BETAiso = "9867322a989a0a3e566e13cf962853a79df9c508";
                        Console.Write("The SHA1 hash of the your iso is: {0}", isouser);
                        //I'm sure I can make those checks liter but too lazy to do it
                        if (isouser == KH2FMiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2FM!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2UKiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 UK!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2FRiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 FR!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2GRiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 GR!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2ITiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 IT!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2JAPiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 JAP!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2ESiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 ES!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2USiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 US!");
                            Console.ResetColor();
                            goto EOF;
                        }
                        if (isouser == KH2BETAiso)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nYou have a correct dump of the game KH2 PROTOTYPE!\n");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("Wait you SERIOUSLY HAVE ONE? o.O");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("\nYou don't have a correct dump! Please make a new one!");
                            Console.ResetColor();
                        }
                        EOF:
                        Console.ReadLine();
                        return;
                    }
                }
            }

            #endregion

            try
            {
                using (FileStream iso = File.Open(isoname, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (extract)
                    {
                        ExtractISO(iso);
                    }
                    else
                    {
                        if (k2e)
                        {
                            try
                            {
                                FileStream fs = iso;
                                if (fs.ReadByte() == 0x4B && fs.ReadByte() == 0x48 && fs.ReadByte() == 0x32 &&
                                    fs.ReadByte() == 0x50)
                                {
                                    fs.Position = 0;
                                    KH2PatchExtractor(fs);
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
                                    fs.Read(buffer, 0, (int) fs.Length);
                                    PatchManager.NGYXor(buffer);
                                    KH2PatchExtractor(new MemoryStream(buffer));
                                }

                                catch (Exception)
                                {
                                    try
                                    {
                                        fs.Position = 0;
                                        var buffer = new byte[fs.Length];
                                        fs.Read(buffer, 0, (int) fs.Length);
                                        PatchManager.GYXor(buffer);
                                        KH2PatchExtractor(new MemoryStream(buffer));
#if DEBUG
                                        WriteWarning("Old format is used, Please use the new one!");
#endif
                                    }
                                    catch (Exception)
                                    {
                                        fs.Position = 0;
                                        var buffer = new byte[fs.Length];
                                        fs.Read(buffer, 0, (int) fs.Length);
                                        PatchManager.XeeyXor(buffer);
                                        KH2PatchExtractor(new MemoryStream(buffer));
                                        WriteWarning("Old format is used, Please use the new one!");
                                    }
                                }
                                finally
                                {
                                    fs.Dispose();
                                    fs = null;
                                }
                            }
                            catch
                            {
                            }
                        }
                        else
                        {
                            if (Patches.patches.Count == 0)
                            {
                                WriteWarning("No patches loaded!");
                            }
                            else
                            {
                                isoname = Path.ChangeExtension(isoname, ".NEW.ISO");
                                try
                                {
                                    using (
                                        FileStream NewISO = File.Open(isoname, FileMode.Create, FileAccess.ReadWrite,
                                            FileShare.None))
                                    {
                                        PatchISO(iso, NewISO);
                                    }
                                }
                                catch (Exception)
                                {
                                    //Delete the new "incomplete" iso
                                    File.Delete(isoname);
                                    throw;
                                }
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                WriteWarning("Failed to open file: " + e.Message);
            }
            catch (Exception e)
            {
                WriteWarning(
                    "An error has occured when trying to open your iso:\n{1}: {0}\n{2}",
                    e.Message, e.GetType().FullName, e.StackTrace);
            }
            Patches.Dispose();
            if (!batch)
            {
                Console.Write("\nPress enter to exit...");
                Console.ReadLine();

            }
        }
    }
}