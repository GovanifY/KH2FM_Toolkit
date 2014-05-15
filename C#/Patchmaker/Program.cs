using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GovanifY.Utility;
using HashList;
using KH2FM_Toolkit;
using KHCompress;
using ISOTP = KH2FM_Toolkit.Program;

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
namespace KH2ISO_PatchMaker
{
    internal class PatchFile
    {
        public const uint Signature = 0x5032484B;
        public const uint Signaturec = 0x4332484B;
        private readonly List<byte[]> Changelogs = new List<byte[]>();
        public List<byte[]> Credits = new List<byte[]>();
        public List<FileEntry> Files = new List<FileEntry>();
        public uint Version = 1;
        private byte[] _Author = {0};
        private byte[] _OtherInfo = {0};
        public bool convertLinebreaks = true;

        public string Author
        {
            get { return Encoding.ASCII.GetString(_Author); }
            set { _Author = Encoding.ASCII.GetBytes(value + '\0'); }
        }

        public string OtherInfo
        {
            get { return Encoding.ASCII.GetString(_OtherInfo); }
            set
            {
                if (convertLinebreaks)
                {
                    value = value.Replace("\\n", "\r\n");
                }
                _OtherInfo = Encoding.ASCII.GetBytes(value + '\0');
            }
        }

        public void AddChange(string s)
        {
            if (convertLinebreaks)
            {
                s = s.Replace("\\n", "\r\n");
            }
            Changelogs.Add(Encoding.ASCII.GetBytes(s + '\0'));
        }

        public void AddCredit(string s)
        {
            if (convertLinebreaks)
            {
                s = s.Replace("\\n", "\r\n");
            }
            Credits.Add(Encoding.ASCII.GetBytes(s + '\0'));
        }

        public void WriteDecrypted(Stream stream)
        {
            stream.Position = 0;
            uint changeLen = 0, creditLen = 0;
            changeLen = Changelogs.Aggregate(changeLen, (current, b) => current + (4 + (uint) b.Length));
            creditLen = Credits.Aggregate(creditLen, (current, b) => current + (4 + (uint) b.Length));
            using (var bw = new BinaryStream(stream, leaveOpen: true))
            {
                uint i;
                bw.Write(Signature);
                bw.Write((uint) (16 + _Author.Length));
                bw.Write((uint) (16 + _Author.Length + 16 + changeLen + 4 + creditLen + _OtherInfo.Length));
                bw.Write(Version);
                bw.Write(_Author);
                bw.Write((uint) 12);
                bw.Write(16 + changeLen);
                bw.Write(16 + changeLen + 4 + creditLen);
                bw.Write(i = (uint) Changelogs.Count);
                i *= 4;
                foreach (var b in Changelogs)
                {
                    bw.Write(i);
                    i += (uint) b.Length;
                }
                foreach (var b in Changelogs)
                {
                    bw.Write(b);
                }
                bw.Write(i = (uint) Credits.Count);
                i *= 4;
                foreach (var b in Credits)
                {
                    bw.Write(i);
                    i += (uint) b.Length;
                }
                foreach (var b in Credits)
                {
                    bw.Write(b);
                }
                bw.Write(_OtherInfo);
                bw.Write((uint) Files.Count);

                //Check total size to add
                long fileTotal = 0;
                try
                {
                    fileTotal = Files.Where(file => file.Relink == 0)
                        .Aggregate(fileTotal, (current, file) => checked(current + file.Data.Length));
                }
                catch (OverflowException)
                {
                    ISOTP.WriteError(
                        "That's WAY too much file data... is there even that much in the gameo.O?\r\nTry to split up the patch...");
                    return;
                }
                Stream filedata = null;
                string filename = null;
                //Use a MemoryStream if we can, much cleaner\faster
                if (fileTotal <= int.MaxValue)
                {
                    try
                    {
                        filedata = new MemoryStream((int) fileTotal);
                    }
                    catch (OutOfMemoryException)
                    {
                        filedata = null;
                        ISOTP.WriteWarning("Failed to allocate enough memory, trying temporary file fallback...");
                    }
                }
                //If we can't use a MemStream (or that failed), try a FileStream as a temp file
                if (filedata == null)
                {
                    filename = Path.GetTempFileName();
                    Console.WriteLine("Wow there's a lot of file data! Using a temporary file now!\r\nUsing {0}",
                        filename);
                    filedata = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                using (filedata)
                {
                    i = (uint) (stream.Position + Files.Count*92);
                    foreach (FileEntry file in Files)
                    {
                        bw.Write(file.Hash);
                        if (file.Relink != 0)
                        {
                            bw.Write((uint) 0);
                            bw.Write((uint) 0);
                            bw.Write((uint) 0);
                            bw.Write(file.ParentHash);
                            bw.Write(file.Relink);
                            bw.Write((uint) 0);
                        }
                        else
                        {
                            uint cSize;
                            file.Data.Position = 0;
                            if (file.IsCompressed)
                            {
                                try
                                {
                                    var input = new byte[file.Data.Length];
                                    file.Data.Read(input, 0, (int) file.Data.Length);
                                    Console.Write("Compressing {0}: ",
                                        file.name ?? file.Hash.ToString("X8"));
                                    byte[] output = KH2Compressor.compress(input);
                                    uint cSizeSectors = (uint) Math.Ceiling((double) output.Length/2048) - 1;
                                    if (output.LongLength > int.MaxValue)
                                    {
                                        throw new NotSupportedException(
                                            "Compressed data too big to store (Program limitation)");
                                    }
                                    if (cSizeSectors > 0x2FFF)
                                    {
                                        throw new NotSupportedException(
                                            "Compressed data too big to store (IDX limitation)");
                                    }
                                    if ((cSizeSectors & 0x1000u) == 0x1000u)
                                    {
                                        throw new NotSupportedException(
                                            "Compressed data size hit 0x1000 bit limitation (IDX limitation)");
                                    }
                                    cSize = (uint) output.Length;
                                    filedata.Write(output, 0, output.Length);
                                }
                                catch (NotCompressableException e)
                                {
                                    string es = "ERROR: Failed to compress file: " + e.Message;
                                    ISOTP.WriteWarning(es);
                                    Console.Write("Add it without compressing? [Y/n] ");
                                    if (Program.GetYesNoInput())
                                    {
                                        file.IsCompressed = false;
                                        cSize = (uint) file.Data.Length;
                                        file.Data.Position = 0; //Ensure at beginning
                                        file.Data.CopyTo(filedata);
                                    }
                                    else
                                    {
                                        throw new NotCompressableException(es, e);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Adding {0}", file.name ?? file.Hash.ToString("X8"));
                                cSize = (uint) file.Data.Length;
                                file.Data.Position = 0; //Ensure at beginning
                                file.Data.CopyTo(filedata);
                            }
                            if (!file.IsCompressed &&
                                (((uint) Math.Ceiling((double) cSize/2048) - 1) & 0x1000u) == 0x1000u)
                            {
                                ISOTP.WriteWarning(
                                    "Data size hit 0x1000 bit limitation, but this file may be OK if it's streamed.");
                            }
                            bw.Write(i);
                            i += cSize;
                            bw.Write(cSize);
                            bw.Write((uint) file.Data.Length);
                            bw.Write(file.ParentHash);
                            bw.Write((uint) 0);
                            bw.Write((uint) (file.IsCompressed ? 1 : 0));
                        }
                        bw.Write((uint) (file.IsNewFile ? 1 : 0)); //Custom
                        //Padding
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                        bw.Write((uint) 0);
                    }
                    filedata.Position = 0; //Ensure at beginning
                    filedata.CopyTo(stream);
                }
                //If we used a temp file, delete it
                if (filename != null)
                {
                    File.Delete(filename);
                }
            }
        }

        public void Write(Stream stream)
        {
            if (Program.DoXeey == false)
            {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    WriteDecrypted(ms);
                    data = ms.ToArray();
                }
                PatchManager.NGYXor(data);
                stream.Write(data, 0, data.Length);
            }
            else
            {
                byte[] data;
                using (var ms = new MemoryStream())
                {
                    WriteDecrypted(ms);
                    data = ms.ToArray();
                }
                PatchManager.XeeyXor(data);
                stream.Write(data, 0, data.Length);
            }
        }

        public class FileEntry : IDisposable
        {
            /// <summary>
            ///     <para>File data, uncompressed</para>
            ///     <para>NULL if relinking</para>
            /// </summary>
            public Stream Data = null;

            /// <summary>Target file hash</summary>
            public uint Hash = 0;

            public bool IsCompressed = false;

            /// <summary>
            ///     <para>Custom field</para>
            ///     <para>Specified whether the file should be ADDED to the IDX if it's missing</para>
            /// </summary>
            public bool IsNewFile = false;

            /// <summary>Parent IDX Hash</summary>
            public uint ParentHash = 0;

            /// <summary>Relink to this file</summary>
            public uint Relink = 0;

            /// <summary>Filename, used in UI</summary>
            public string name = null;

            public void Dispose()
            {
                if (Data != null)
                {
                    Data.Dispose();
                    Data = null;
                }
            }
        }
    }

    internal class Program
    {
        //Define a bool who's define if the Xeeynamo's encryption is used. She's false until the command -xeey is used
        public static bool DoXeey = false;
        public static bool NewFormat = true;
        public static bool Compression = false;
        public static bool hvs = false;

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

        private static uint GetFileAsInput(out string name, out bool blank)
        {
            string inp = Console.ReadLine().Replace("\"", "").Trim();
            uint hash;
            name = "";
            if (inp.Length == 0)
            {
                blank = true;
                return 0;
            }
            blank = false;
            if (inp.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (uint.TryParse(inp.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hash))
                {
                    name = HashList.HashList.NameFromHash(hash);
                }
                else
                {
                    ISOTP.WriteWarning("Error: Failed to parse as hex number.");
                    return 0;
                }
            }
            else
            {
                hash = PatchManager.ToHash(inp);
                //Check hashpairs anyway, and warn if something unexpected returns
                if (!hvs)
                {
                    if (!HashList.HashList.pairs.TryGetValue(hash, out name))
                    {
                        Console.WriteLine(" Warning: Filename not found into the Hashlist.");
                    }
                    else if (name != inp)
                    {
                        ISOTP.WriteWarning(" Warning: Hash conflict with {0}; both contain the same hash.", name);
                    }
                    name = inp;
                }
            }
            return hash;
        }

        private static uint GetFileHashAsInput(out string name)
        {
            bool blank;
            return GetFileAsInput(out name, out blank);
        }

        public static bool GetYesNoInput()
        {
            int cL = Console.CursorLeft, cT = Console.CursorTop;
            do
            {
                string inp = Console.ReadLine();
                if (inp == "Y" || inp == "y")
                {
                    return true;
                }
                if (inp == "N" || inp == "n")
                {
                    return false;
                }
                Console.SetCursorPosition(cL, cT);
                Console.Beep();
            } while (true);
        }

        internal static void Mainp(string[] args)
        {
            bool log = false;

            Console.Title = ISOTP.program.ProductName + " " + ISOTP.program.FileVersion + " [" +
                            ISOTP.program.CompanyName + "]";
            var patch = new PatchFile();
            bool encrypt = true,
                batch = false,
                authorSet = false,
                verSet = false,
                changeSet = false,
                creditSet = false,
                otherSet = false;
            string output = "output.kh2patch";
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "-xeeynamo":
                        DoXeey = true;
                        ISOTP.WriteWarning("Using Xeeynamo's encryption!(DESTRUCTIVE METHOD)");
                        break;
                    case "-batch":
                        batch = true;
                        break;
                    case "-log":
                        log = true;
                        break;
#if DEBUG
                    case "-decrypted":
                        if (encrypt)
                        {
                            encrypt = false;
                            Console.WriteLine("Writing in decrypted mode!");
                        }
                        break;
#endif
                    case "-hashverskip":
                        hvs = true;
                        break;
                    case "-version":
                        if (!uint.TryParse(args[++i].Trim(), out patch.Version))
                        {
                            patch.Version = 1;
                        }
                        else
                        {
                            verSet = true;
                        }
                        break;
                    case "-author":
                        patch.Author = args[++i];
                        authorSet = true;
                        break;
                    case "-other":
                        patch.OtherInfo = args[++i];
                        otherSet = true;
                        break;
                    case "-changelog":
                        patch.AddChange(args[++i]);
                        break;
                    case "-skipchangelog":
                        changeSet = true;
                        break;
                    case "-credits":
                        patch.AddCredit(args[++i]);
                        break;
                    case "-skipcredits":
                        creditSet = true;
                        break;
                    case "-output":
                        output = args[++i];
                        if (!output.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase))
                        {
                            output += ".kh2patch";
                        }
                        break;
                }
            }
            //TODO MENU
            if (log)
            {
                var filestream = new FileStream("log.log", FileMode.Create);
                var streamwriter = new StreamWriter(filestream);
                streamwriter.AutoFlush = true;
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
            }
            if (!batch)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                DateTime builddate = RetrieveLinkerTimestamp();
                Console.Write("{0}\nBuild Date: {2}\nVersion {1}", ISOTP.program.ProductName,
                    ISOTP.program.FileVersion, builddate);
                Console.ResetColor();
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nPRIVATE RELEASE\n");
                Console.ResetColor();
#else
                Console.Write("\nPUBLIC RELEASE\n");
#endif
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write("\nProgrammed by {0}\nhttp://www.govanify.blogspot.fr\nhttp://www.govanify.x10host.com",
                    ISOTP.program.CompanyName);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(
                    "\n\nThis tool is able to create patches for the software KH2FM_Toolkit.\nHe can add files using the internal compression of the game \nKingdom Hearts 2(Final Mix), relink files to their idx, recreate\nthe iso without size limits and without corruption.\nThis patch system is the best ever made for this game atm.\n");
                HashList.HashList.loadHashPairs(printInfo: true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nPress enter to run using the file:");
                Console.ResetColor();
                Console.Write(" {0}", output);

                if (!batch)
                {
                    Console.ReadLine();
                }
            }
            if (!authorSet)
            {
                Console.Write("Enter author's name: ");
                patch.Author = Console.ReadLine().Trim();
            }
            if (!verSet)
            {
                Console.Write("Enter revision number: ");
                while (!uint.TryParse(Console.ReadLine().Trim(), out patch.Version))
                {
                    ISOTP.WriteWarning("\nInvalid number! ");
                }
            }
            if (!changeSet)
            {
                Console.WriteLine("Enter changelog lines here (leave blank to continue):");
                do
                {
                    string inp = Console.ReadLine().Trim();
                    if (inp.Length == 0)
                    {
                        break;
                    }
                    patch.AddChange(inp);
                } while (true);
            }
            if (!creditSet)
            {
                Console.WriteLine("Enter credits here (leave blank to continue):");
                do
                {
                    string inp = Console.ReadLine().Trim();
                    if (inp.Length == 0)
                    {
                        break;
                    }
                    patch.AddCredit(inp);
                } while (true);
            }
            if (!otherSet)
            {
                Console.Write("Other information (leave blank to continue): ");
                patch.OtherInfo = Console.ReadLine().Trim();
            }
#if DEBUG
            Console.WriteLine("Filenames may be formatted as text (msg/jp/lk.bar) or hash (0x030b45da).");
#endif
            do
            {
                var file = new PatchFile.FileEntry();
                Console.Write("\nEnter filename: ");
                string name, rel;
                //Target file
                file.Hash = GetFileAsInput(out name, out otherSet);
                if (otherSet)
                {
                    break;
                }
                Console.WriteLine("  Using \"{0}\" for {1:X8}", name, file.Hash);
                //Relink
                Console.Write("Relink to this filename(ex: 000al.idx) [Blank for none]: ");
                file.Relink = GetFileHashAsInput(out rel);
                if (file.Relink == 0)
                {
                    try
                    {
                        file.Data = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    catch (Exception e)
                    {
                        ISOTP.WriteWarning("Failed opening the file: " + e.Message);
                        continue;
                    }
                    file.name = Path.GetFileName(name);
                    if (file.Data.Length > int.MaxValue || file.Data.Length < 10)
                    {
                        ISOTP.WriteWarning("Too {0} to compress. Press enter.",
                            (file.Data.Length < 10 ? "small" : "big"));
                        //Do this so the line count is the same whether we can compress or not.
                        Console.ReadLine();
                    }
                    else
                    {
                        //Compress
                        Console.Write("Compress this file? [Y/n] ");
                        file.IsCompressed = GetYesNoInput();
                    }
                }
                else
                {
                    Console.WriteLine("  Using \"{0}\" for {1:X8}", rel, file.Relink);
                }
                //Parent
                Console.Write("Parent compressed file [Leave blank for KH2]: ");
                file.ParentHash = GetFileHashAsInput(out rel);
                if (rel.Equals("KH2", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.ParentHash = 0;
                }
                else if (rel.Equals("OVL", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.ParentHash = 1;
                }
                else if (rel.Equals("ISO", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.ParentHash = 2;
                }
                else
                {
                    switch (file.ParentHash)
                    {
                        case 0:
                            rel = "KH2";
                            break;
                        case 1:
                            rel = "OVL";
                            break;
                        case 2:
                            rel = "ISO";
                            break;
                    }
                }
                Console.WriteLine("  Using \"{0}\" for {1:X8}", rel, file.ParentHash);
                //IsNew
                Console.Write("Should this file be added if he's not in the game? [y/N] ");
                file.IsNewFile = GetYesNoInput();
                patch.Files.Add(file);
            } while (true);
            try
            {
                //TODO Compress(buffer>magic>Compress>Write to output). Files are already compressed, I need to look at this later
                using (FileStream fs = File.Open(output, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (encrypt)
                    {
                        patch.Write(fs);
                    }
                    else
                    {
                        patch.WriteDecrypted(fs);
                    }
                }
            }
            catch (Exception e)
            {
                ISOTP.WriteWarning("Failed to save file: " + e.Message);
                ISOTP.WriteWarning(e.StackTrace);
                try
                {
                    File.Delete("output.kh2patch");
                }
                catch (Exception z)
                {
                    ISOTP.WriteWarning("Failed to delete file: " + z.Message);
                }
            }
            if (!batch)
            {
                Console.Write("Press enter to exit...");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
    }
}