using System;
using System.IO;
using System.Collections.Generic;
using GovanifY.Utility;
using KH2FM_Toolkit;
using ISOTP = KH2FM_Toolkit.Program;

namespace KH2ISO_PatchMaker
{
    class PatchFile
    {
        
        public bool convertLinebreaks = true;
        public class FileEntry : IDisposable
        {
            /// <summary>Target file hash</summary>
            public uint Hash = 0;
            /// <summary>Parent IDX Hash</summary>
            public uint ParentHash = 0;
            /// <summary>Relink to this file</summary>
            public uint Relink = 0;
            public bool IsCompressed = false;
            /// <summary><para>Custom field</para><para>Specified whether the file should be ADDED to the IDX if it's missing</para></summary>
            public bool IsNewFile = false;
            /// <summary><para>File data, uncompressed</para><para>NULL if relinking</para></summary>
            public Stream Data = null;
            /// <summary>Filename, used in UI</summary>
            public string name = null;
            public void Dispose()
            {
                if (this.Data != null) { this.Data.Dispose(); this.Data = null; }
            }
        }
        public const uint Signature = 0x5032484B;
        public const uint Signaturec = 0x4332484B;
        public uint Version = 1;
        private byte[] _Author = new byte[] { 0 };
        public string Author
        {
            get { return System.Text.Encoding.ASCII.GetString(this._Author); }
            set { this._Author = System.Text.Encoding.ASCII.GetBytes(value + '\0'); }
        }
        private List<byte[]> Changelogs = new List<byte[]>();
        public void AddChange(string s)
        {
            if (this.convertLinebreaks) { s = s.Replace("\\n", "\r\n"); }
            this.Changelogs.Add(System.Text.Encoding.ASCII.GetBytes(s + '\0'));
        }
        public List<byte[]> Credits = new List<byte[]>();
        public void AddCredit(string s)
        {
            if (this.convertLinebreaks) { s = s.Replace("\\n", "\r\n"); }
            this.Credits.Add(System.Text.Encoding.ASCII.GetBytes(s + '\0'));
        }
        private byte[] _OtherInfo = new byte[] { 0 };
        public string OtherInfo
        {
            get { return System.Text.Encoding.ASCII.GetString(this._OtherInfo); }
            set
            {
                if (this.convertLinebreaks) { value = value.Replace("\\n", "\r\n"); }
                this._OtherInfo = System.Text.Encoding.ASCII.GetBytes(value + '\0');
            }
        }
        public List<FileEntry> Files = new List<FileEntry>();
        public void WriteDecrypted(Stream stream)
        {
            stream.Position = 0;
            uint changeLen = 0, creditLen = 0;
            foreach (byte[] b in this.Changelogs) { changeLen += 4 + (uint)b.Length; }
            foreach (byte[] b in this.Credits) { creditLen += 4 + (uint)b.Length; }
            using (BinaryStream bw = new BinaryStream(stream, leaveOpen:true))
            {
                uint i;
                bw.Write(Signature);
                bw.Write((uint)(16 + this._Author.Length));
                bw.Write((uint)(16 + this._Author.Length + 16 + changeLen + 4 + creditLen + this._OtherInfo.Length));
                bw.Write(this.Version);
                bw.Write(this._Author);
                bw.Write((uint)12);
                bw.Write((uint)(16 + changeLen));
                bw.Write((uint)(16 + changeLen + 4 + creditLen));
                bw.Write(i = (uint)this.Changelogs.Count); i *= 4;
                foreach (byte[] b in this.Changelogs) { bw.Write(i); i += (uint)b.Length; }
                foreach (byte[] b in this.Changelogs) { bw.Write(b); }
                bw.Write(i=(uint)this.Credits.Count); i *= 4;
                foreach (byte[] b in this.Credits) { bw.Write(i); i += (uint)b.Length; }
                foreach (byte[] b in this.Credits) { bw.Write(b); }
                bw.Write(this._OtherInfo);
                bw.Write((uint)this.Files.Count);

                //Check total size to add
                long fileTotal = 0;
                try { foreach (FileEntry file in this.Files) { if (file.Relink == 0) { fileTotal = checked(fileTotal + file.Data.Length); } } }
                catch (System.OverflowException) { ISOTP.WriteError("That's WAY too much file data... is there even that much in the gameo.O?\r\nTry to split up the patch..."); return; }
                Stream filedata = null;
                string filename = null;
                //Use a MemoryStream if we can, much cleaner\faster
                if (fileTotal <= int.MaxValue)
                {
                    try { filedata = new MemoryStream((int)fileTotal); }
                    catch (OutOfMemoryException) { filedata = null; ISOTP.WriteWarning("Failed to allocate enough memory, trying temporary file fallback..."); }
                }
                //If we can't use a MemStream (or that failed), try a FileStream as a temp file
                if (filedata == null)
                {
                    filename = Path.GetTempFileName();
                    Console.WriteLine("Wow there's a lot of file data! Using a temporary file now!\r\nUsing {0}", filename);
                    filedata = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                }
                using (filedata)
                {
                    i = (uint)(stream.Position + this.Files.Count * 92);
                    foreach (FileEntry file in this.Files)
                    {
                        bw.Write(file.Hash);
                        if (file.Relink != 0)
                        {
                            bw.Write((uint)0);
                            bw.Write((uint)0);
                            bw.Write((uint)0);
                            bw.Write(file.ParentHash);
                            bw.Write(file.Relink);
                            bw.Write((uint)0);
                        }
                        else
                        {
                            uint cSize;
                            file.Data.Position = 0;
                            if (file.IsCompressed)
                            {
                                try
                                {
                                    byte[] input = new byte[file.Data.Length];
                                    file.Data.Read(input, 0, (int)file.Data.Length);
                                    Console.Write("Compressing {0}: ", file.name != null ? file.name : file.Hash.ToString("X8"));
                                    byte[] output = KHCompress.KH2Compressor.compress(input);
                                    uint cSizeSectors = (uint)Math.Ceiling((double)output.Length / 2048) - 1;
                                    if (output.LongLength > int.MaxValue) { throw new NotSupportedException("Compressed data too big to store (Program limitation)"); }
                                    if (cSizeSectors > 0x2FFF) { throw new NotSupportedException("Compressed data too big to store (IDX limitation)"); }
                                    if ((cSizeSectors & 0x1000u) == 0x1000u) { throw new NotSupportedException("Compressed data size hit 0x1000 bit limitation (IDX limitation)"); }
                                    cSize = (uint)output.Length;
                                    filedata.Write(output, 0, output.Length);
                                }
                                catch (KHCompress.NotCompressableException e)
                                {
                                    string es = "ERROR: Failed to compress file: " + e.Message;
                                    ISOTP.WriteWarning(es);
                                    Console.Write("Add it without compressing? [Y/n] ");
                                    if (Program.GetYesNoInput())
                                    {
                                        file.IsCompressed = false;
                                        cSize = (uint)file.Data.Length;
                                        file.Data.Position = 0; //Ensure at beginning
                                        file.Data.CopyTo(filedata);
                                    }
                                    else { throw new KHCompress.NotCompressableException(es, e); }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Adding {0}", file.name != null ? file.name : file.Hash.ToString("X8"));
                                cSize = (uint)file.Data.Length;
                                file.Data.Position = 0; //Ensure at beginning
                                file.Data.CopyTo(filedata);
                            }
                            if (!file.IsCompressed && (((uint)Math.Ceiling((double)cSize / 2048) - 1) & 0x1000u) == 0x1000u)
                            {
                                ISOTP.WriteWarning("Data size hit 0x1000 bit limitation, but this file may be OK if it's streamed.");
                            }
                            bw.Write(i); i += cSize;
                            bw.Write(cSize);
                            bw.Write((uint)file.Data.Length);
                            bw.Write(file.ParentHash);
                            bw.Write((uint)0);
                            bw.Write((uint)(file.IsCompressed ? 1 : 0));
                        }
                        bw.Write((uint)(file.IsNewFile ? 1 : 0));   //Custom
                        //Padding
                        bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0); bw.Write((uint)0);
                    }
                    filedata.Position = 0; //Ensure at beginning
                    filedata.CopyTo(stream);
                }
                //If we used a temp file, delete it
                if (filename != null) { File.Delete(filename); filename = null; }
            }
        }
        public void Write(Stream stream)
        {
            if (Program.DoXeey == false)
            {
                
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    this.WriteDecrypted(ms);
                    data = ms.ToArray();
                }
                PatchManager.GYXor(data);
                    stream.Write(data, 0, data.Length);
            }
            else
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    this.WriteDecrypted(ms);
                    data = ms.ToArray();
                }
                PatchManager.XeeyXor(data);
                stream.Write(data, 0, data.Length);  
            }
            
           
        }
    }
    class Program
    {
        private static DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        //Define a bool who's define if the Xeeynamo's encryption is used. She's false until the command -xeey is used
        public static bool DoXeey = false;
        public static bool NewFormat = true;
        public static bool Compression = false;
        public static bool hvs = false;
        static uint GetFileAsInput(out string name, out bool blank)
        {
            string inp = Console.ReadLine().Replace("\"", "").Trim();
            uint hash; name = "";
            if (inp.Length == 0) { blank = true; return 0; }
            blank = false;
            if (inp.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (uint.TryParse(inp.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out hash))
                {
                    name = HashList.HashPairs.NameFromHash(hash);
                }
                else { ISOTP.WriteWarning("Error: Failed to parse as hex number."); return 0; }
            }
            else
            {
                hash = PatchManager.ToHash(inp);
                //Check hashpairs anyway, and warn if something unexpected returns
                if (!hvs)
                {
                    if (!HashList.HashPairs.pairs.TryGetValue(hash, out name))
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
        static uint GetFileHashAsInput(out string name)
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
                if (inp == "Y" || inp == "y") { return true; }
                if (inp == "N" || inp == "n") { return false; }
                Console.SetCursorPosition(cL, cT);
                Console.Beep();
            } while (true);
        }

        internal static void Mainp(string[] args)
        {
            Console.Title = KH2FM_Toolkit.Program.program.ProductName + " " + KH2FM_Toolkit.Program.program.FileVersion + " [" + KH2FM_Toolkit.Program.program.CompanyName + "]";
            PatchFile patch = new PatchFile();
            bool encrypt = true, batch = false, authorSet = false, verSet = false, changeSet = false, creditSet = false, otherSet = false;
            string output = "output.kh2patch";
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "-xeeynamo": DoXeey = true; ISOTP.WriteWarning("Using Xeeynamo's encryption!(DESTRUCTIVE METHOD)"); break;
                    case "-batch": batch = true; break;
#if DEBUG
                    case "-decrypted": if (encrypt) { encrypt = false; Console.WriteLine("Writing in decrypted mode!"); } break;
#endif
                    case "-hashverskip":
                        hvs = true;
                        break;
                    case "-version": if (!uint.TryParse(args[++i].Trim(), out patch.Version)) { patch.Version = 1; } else { verSet = true; } break;
                    case "-author": patch.Author = args[++i]; authorSet = true; break;
                    case "-other": patch.OtherInfo = args[++i]; otherSet = true; break;
                    case "-changelog": patch.AddChange(args[++i]); break;
                    case "-skipchangelog": changeSet = true; break;
                    case "-credits": patch.AddCredit(args[++i]); break;
                    case "-skipcredits": creditSet = true; break;
                    case "-output": output = args[++i]; if (!output.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase)) { output += ".kh2patch"; } break;
                }
            }
            //TODO MENU
            if (!batch)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                var builddate = RetrieveLinkerTimestamp();
                Console.Write("{0}\nBuild Date: {2}\nVersion {1}", KH2FM_Toolkit.Program.program.ProductName,
                    KH2FM_Toolkit.Program.program.FileVersion, builddate);
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
                    KH2FM_Toolkit.Program.program.CompanyName);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(
                    "\n\nThis tool is able to create patches for the software KH2FM_Toolkit.\nHe can add files using the internal compression of the game \nKingdom Hearts 2(Final Mix), relink files to their idx, recreate\nthe iso without size limits and without corruption.\nThis patch system is the best ever made for this game atm.\n");
                HashList.HashPairs.loadHashPairs(printInfo: true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nPress enter to run using the file:");
                Console.ResetColor();
                Console.Write(" {0}", output);

            if (!batch) { Console.ReadLine(); }
            }
            if (!authorSet) { Console.Write("Enter author's name: "); patch.Author = Console.ReadLine().Trim(); }
            if (!verSet) {
                Console.Write("Enter revision number: ");
                while (!uint.TryParse(Console.ReadLine().Trim(), out patch.Version)) { ISOTP.WriteWarning("\nInvalid number! "); }
            }
            if (!changeSet)
            {
                Console.WriteLine("Enter changelog lines here (leave blank to continue):");
                do
                {
                    string inp = Console.ReadLine().Trim();
                    if (inp.Length == 0) { break; }
                    patch.AddChange(inp);
                } while (true);
            }
            if (!creditSet)
            {
                Console.WriteLine("Enter credits here (leave blank to continue):");
                do
                {
                    string inp = Console.ReadLine().Trim();
                    if (inp.Length == 0) { break; }
                    patch.AddCredit(inp);
                } while (true);
            }
            if (!otherSet) { Console.Write("Other information (leave blank to continue): "); patch.OtherInfo = Console.ReadLine().Trim(); }
#if DEBUG
            Console.WriteLine("Filenames may be formatted as text (msg/jp/lk.bar) or hash (0x030b45da).");
#endif
            do
            {
                PatchFile.FileEntry file = new PatchFile.FileEntry();
                Console.Write("\nEnter filename: ");
                string name, rel;
                //Target file
                file.Hash = GetFileAsInput(out name, out otherSet);
                if (otherSet) { break; }
                Console.WriteLine("  Using \"{0}\" for {1:X8}", name, file.Hash);
                //Relink
                Console.Write("Relink to this filename(ex: 000al.idx) [Blank for none]: ");
                file.Relink = GetFileHashAsInput(out rel);
                if (file.Relink == 0)
                {
                    try { file.Data = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read); }
                    catch (Exception e) { ISOTP.WriteWarning("Failed opening the file: " + e.Message); continue; }
                    file.name = Path.GetFileName(name);
                    if (file.Data.Length > int.MaxValue || file.Data.Length < 10)
                    {
                        ISOTP.WriteWarning("Too {0} to compress. Press enter.", (file.Data.Length < 10 ? "small" : "big"));
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
                if (rel.Equals("KH2", StringComparison.InvariantCultureIgnoreCase)) { file.ParentHash = 0; }
                else if (rel.Equals("OVL", StringComparison.InvariantCultureIgnoreCase)) { file.ParentHash = 1; }
                else if (rel.Equals("ISO", StringComparison.InvariantCultureIgnoreCase)) { file.ParentHash = 2; }
                else
                {
                    switch (file.ParentHash)
                    {
                        case 0: rel = "KH2"; break;
                        case 1: rel = "OVL"; break;
                        case 2: rel = "ISO"; break;
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
                //TODO Compress(buffer>magic>Compress>Write to output)
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
                try { File.Delete("output.kh2patch"); }
                catch { }
            }
            if (!batch) { Console.Write("Press enter to exit..."); Console.ReadLine();
            Environment.Exit(0);
            }
        }
    }
}
