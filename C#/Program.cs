using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using GovanifY.Utility;
using HashList;
using IDX_Tools;
using ISO_Tools;
using KH2FM_Toolkit.Properties;
using Utility;
using System.Security.Cryptography;
using KHCompress;

namespace KH2FM_Toolkit
{
    public static class Program
    {
        /// <summary>
        ///     <para>Sector size of the ISO</para>
        ///     <para>Almost always 2048 bytes</para>
        /// </summary>
        public const int sectorSize = 2048;

        public static readonly FileVersionInfo program =
            FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        private static readonly PatchManager patches = new PatchManager();
        private static bool advanced;

        private static DateTime builddate { get; set; }

        private static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
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

            int i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
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
                    if (advanced)
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
                        bool adSize = advanced;
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

        /// <param name="sidx">Left open.</param>
        /// <param name="simg">Left open.</param>
        /// <param name="timg">Left open.</param>
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
            else if (HashPairs.pairs.TryGetValue(parenthash, out parentname))
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
                                    DataLength = (uint)subidx.Length,
                                    IsCompressed = false,
                                    CompressedDataLength = (uint)subidx.Length
                                }, subidx);
                            }
                            continue;
                    }
                    PatchManager.Patch patch;
                    // Could make sure the parents match perfectly, but there's only 1 of every name anyway.
                    // So I'll settle for just making sure the file isn't made for the ISO.
                    if (patches.patches.TryGetValue(file.Hash, out patch) && /*patch.Parent == parenthash*/
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
                                patches.AddToNewFiles(patch);
                            }
                            continue;
                        }
                        Console.WriteLine("\tPatching...");
#if extract
Console.WriteLine("\nEXTRACTING THE FILE!");
Console.WriteLine("\nGetting the name...");
string fname2;
HashPairs.pairs.TryGetValue(file.Hash, out fname2);
Console.WriteLine("\nCreating directory...");
Directory.CreateDirectory(Path.GetDirectoryName(fname2));
Console.WriteLine("\nCreating the file...");
var fileStream = File.Create(fname2);
Console.WriteLine("\nConverting the stream to a byte[]...");
patch.Stream.Position = 0;
byte[] buffer = new byte[patch.Stream.Length];
for (int totalBytesCopied = 0; totalBytesCopied < patch.Stream.Length; )
totalBytesCopied += patch.Stream.Read(buffer, totalBytesCopied, Convert.ToInt32(patch.Stream.Length) - totalBytesCopied);
Console.WriteLine("\nConverting the int to uint...");
uint Size = Convert.ToUInt32(buffer.Length);
byte[] file2 = new byte[0];
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
Console.WriteLine("\nDefining the filestream as null...");
fileStream = null;
decompressed = null;
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
                if (patches.newfiles.TryGetValue(parenthash, out newfiles))
                {
                    foreach (uint hash in newfiles)
                    {
                        PatchManager.Patch patch;
                        if (patches.patches.TryGetValue(hash, out patch) && patch.IsNew)
                        {
                            patch.IsNew = false;
                            string fname;
                            if (!HashPairs.pairs.TryGetValue(hash, out fname))
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

        /// <param name="idx">Closed internally.</param>
        /// <param name="img">Closed internally.</param>
        private static MemoryStream PatchIDX(Stream idx, Stream img, FileDescriptor imgd, ISOCopyWriter niso,
            bool IsOVL = false)
        {
            using (idx)
            using (img)
            {
                niso.SeekEnd();
                long imgOffset = niso.file.Position;
                MemoryStream idxms = PatchIDXInternal(idx, img, niso.file, imgOffset, IsOVL ? 1u : 0u);
                imgd.ExtentLBA = (uint)imgOffset / 2048;
                imgd.ExtentLength = (uint)(niso.file.Position - imgOffset);
                imgd.RecordingDate = DateTime.UtcNow;
                niso.PatchFile(imgd);
                niso.SeekEnd();
                return idxms;
            }
        }

        private static void PatchISO(Stream isofile, Stream nisofile)
        {
            using (var iso = new ISOFileReader(isofile))
            using (var niso = new ISOCopyWriter(nisofile, iso))
            {
                if (iso.PrimaryVolumeDescriptor.AbstractFileIdentifier.StartsWith("KH2NONSTANDARD",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new NotSupportedException(
                        "This KH2 ISO was modified to use custom data formats which are incompatible with the normal game. This patcher cannot work with this ISO.");
                }
                uint i = 0;
                Trivalent cKh2 = patches.KH2Changed ? Trivalent.ChangesPending : Trivalent.NoChanges,
                    cOvl = patches.OVLChanged ? Trivalent.ChangesPending : Trivalent.NoChanges;
                bool cIso = patches.ISOChanged;
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
                        if (patches.patches.TryGetValue(PatchManager.ToHash(name), out patch) && patch.IsinISO)
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
        private static void Main(string[] args)
        {
            bool log = false;
            Console.Title = program.ProductName + " " + program.FileVersion + " [" + program.CompanyName + "]";
#if DEBUG
            try
            {
                File.Delete("debug.log");
            }
            catch
            {
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
                        advanced = true;
                        break;
                    case "-log":
                        log = true;
                        break;
                    case "-verifyiso": verify = true;
                        break;
                    case "-help":
                        byte[] buffer = Encoding.ASCII.GetBytes(Resources.Readme);
                        File.WriteAllBytes("Readme.txt", buffer);
                        Console.Write("Help extracted as a Readme\nPress enter to leave the software...");
                        Console.Read();
                        return;
                    case "-patchmaker":
                        KH2ISO_PatchMaker.Program.Mainp(args);
                        break;
                    default:
                        if (File.Exists(arg))
                        {
                            if (isoname == null && arg.EndsWith(".iso", StringComparison.InvariantCultureIgnoreCase))
                            {
                                isoname = arg;
                            }
                            else if (arg.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase))
                            {
                                patches.AddPatch(arg);
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
                var streamwriter = new StreamWriter(filestream);
                streamwriter.AutoFlush = true;
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
                //TODO Redirect to a txt, but problem: make disappear the text on the console. Need to mirror the text
            }
            #region SHA1
            if (verify == true)
            {
                if (isoname == null)
                {
                    isoname = "KH2FM.ISO";
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                builddate = RetrieveLinkerTimestamp();
                Console.Write("{0}\nBuild Date: {2}\nVersion {1}", program.ProductName, program.FileVersion, builddate);
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
                Console.Write("\nNo decompress edition: Extract files without decompressing them\n");
                Console.ResetColor();
#endif
#if extract
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nFUCKING EXTRACTOR EDITION!!!EXTRACTING & PATCHING THE GAMES WITH A KH2PATCH!!\n");
                Console.ResetColor();
#endif

                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("\nProgrammed by {0}\nhttp://www.govanify.blogspot.fr\nhttp://www.govanify.x10host.com",
                        program.CompanyName);
                    Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write(
                            "\n\nThis tool will calculate the hash of your iso for verify if it's a good dump of KH2(FM) or not.\n\n");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("\nPress enter to run using the file:");
                        Console.ResetColor();
                        Console.Write(" {0}", isoname);
                        if (!batch) { Console.ReadLine(); }
                Console.Write("Calculating the SHA1 hash of your iso. Please wait...\n");
                using (var sha1 = SHA1.Create())
                {
                    using (var stream = File.OpenRead(isoname))
                    {
                        string isouser = BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "").ToLower();
                        string KH2FMiso = "81c177a374e1bddf8453c8c96493d4e715a19236";
                        string KH2UKiso = "f541888cf953559bf4ef8c7e51a822f50e05265c";
                        string KH2FRiso = "70f69a59ba47edae5d41dec7396af0d747e92131";
                        string KH2GRiso = "1a03ffe1e3db3c5f920dd2f1a5e90f38783da43b";
                        string KH2ITiso = "36cfb0e3ee615b9228ec9a949d4d41cc0edf2e3a";
                        string KH2JAPiso = "678e1c6545e6d5ef7ed3e6c496b12f5dd2ecbe56";
                        string KH2ESiso = "d190089a0718a7c204ac256ddedc564d600731f3";
                        string KH2USiso = "7e57081735d82fd84be7f79ab05ad3e795bc7e5e";
                        string KH2BETAiso = "9867322a989a0a3e566e13cf962853a79df9c508";
                        Console.Write("The SHA1 hash of the your iso is: {0}", isouser);
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
                            goto EOF;
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
            if (isoname == null)
            {
                isoname = "KH2FM.ISO";
                Console.ForegroundColor = ConsoleColor.Gray;
                builddate = RetrieveLinkerTimestamp();
                Console.Write("{0}\nBuild Date: {2}\nVersion {1}", program.ProductName, program.FileVersion, builddate);
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
                Console.Write("\nNo decompress edition: Extract files without decompressing them\n");
                Console.ResetColor();
#endif
#if extract
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nFUCKING EXTRACTOR EDITION!!!EXTRACTING & PATCHING THE GAMES WITH A KH2PATCH!!\n");
                Console.ResetColor();
#endif

                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("\nProgrammed by {0}\nhttp://www.govanify.blogspot.fr\nhttp://www.govanify.x10host.com",
                        program.CompanyName);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    if (extract)
                    {
                        Console.Write(
                            "\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nHe is using a list for extracting those files, which is not complete.\nBut this is the most complete one for now.\nHe can extract the files KH2.IMG and OVL.IMG\n\n");
                    }
                    else
                    {
                        Console.Write(
                            "\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nHe can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files for avoid\na corrupted game. He can add some files too.\n\n");
                    }
                    HashPairs.loadHashPairs(printInfo: true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\nPress enter to run using the file:");
                    Console.ResetColor();
                    Console.Write(" {0}", isoname);
                    if (!batch)
                    {
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    builddate = RetrieveLinkerTimestamp();
                    Console.Write("{0}\nBuild Date: {2}\nVersion {1}", program.ProductName, program.FileVersion, builddate);
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
                Console.Write("\nNo decompress edition: Extract files without decompressing them\n");
                Console.ResetColor();
#endif
#if extract
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nFUCKING EXTRACTOR EDITION!!!EXTRACTING & PATCHING THE GAMES WITH A KH2PATCH!!\n");
                Console.ResetColor();
#endif
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.Write("\nProgrammed by {0}\nhttp://www.govanify.blogspot.fr\nhttp://www.govanify.x10host.com",
                        program.CompanyName);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    if (extract)
                    {
                        Console.Write(
                            "\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nHe is using a list for extracting those files, which is not complete.\nBut this is the most complete one for now.\nHe can extract the files KH2.IMG and OVL.IMG\n\n");
                    }
                    else
                    {
                        Console.Write(
                            "\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nHe can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files for avoid\na corrupted game. He can add some files too.\n\n");
                    }
                    HashPairs.loadHashPairs(printInfo: true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\nPress enter to run using the file:");
                    Console.ResetColor();
                    Console.Write(" {0}", isoname);
                    if (!batch)
                    {
                        Console.ReadLine();
                    }

            #endregion Description
                }
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
                            if (patches.patches.Count == 0)
                            {
                                WriteWarning("No patches specified, nothing to do!");
                            }
                            else
                            {
                                isoname = Path.ChangeExtension(isoname, ".new.iso");
                                try
                                {
                                    using (
                                        FileStream niso = File.Open(isoname, FileMode.Create, FileAccess.ReadWrite,
                                            FileShare.None))
                                    {
                                        PatchISO(iso, niso);
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
                catch (FileNotFoundException e)
                {
                    WriteWarning("Failed to open file: " + e.Message);
                }
                catch (Exception e)
                {
                    WriteWarning(
                        "An error has occured! Please report this, including the following information:\n{1}: {0}\n{2}",
                        e.Message, e.GetType().FullName, e.StackTrace);
                }
                patches.Dispose();
                if (!batch)
                {
                    Console.Write("\nPress enter to exit...");
                    Console.ReadLine();
                }
            }
        }
    }