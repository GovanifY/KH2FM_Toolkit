#include "Program.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections::Generic;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Reflection;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Text;
using namespace GovanifY::Utility;
using namespace HashList;
using namespace IDX_Tools;
using namespace ISO_Tools;
using namespace KH2FM_Toolkit::Properties;
using namespace Utility;

namespace KH2FM_Toolkit
{

FileVersionInfo *const Program::program = FileVersionInfo::GetVersionInfo(Assembly::GetEntryAssembly()->Location);
PatchManager *const Program::patches = new PatchManager();

	const DateTime &Program::getbuilddate() const
	{
		return privatebuilddate;
	}

	void Program::setbuilddate(const DateTime &value)
	{
		privatebuilddate = value;
	}

	DateTime Program::RetrieveLinkerTimestamp()
	{
		std::string filePath = Assembly::GetCallingAssembly()->Location;
		const int c_PeHeaderOffset = 60;
		const int c_LinkerTimestampOffset = 8;
		unsigned char b[2048];
		Stream *s = 0;

		try
		{
			s = new FileStream(filePath, FileMode::Open, FileAccess::Read);
			s->Read(b, 0, 2048);
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (s != 0)
			{
				s->Close();
			}
		}

		int i = BitConverter::ToInt32(b, c_PeHeaderOffset);
		int secondsSince1970 = BitConverter::ToInt32(b, i + c_LinkerTimestampOffset);
		DateTime dt = DateTime(1970, 1, 1, 0, 0, 0);
		dt = dt.AddSeconds(secondsSince1970);
		dt = dt.AddHours(TimeZone::CurrentTimeZone->GetUtcOffset(dt)->Hours);
		return dt;
	}

	void Program::WriteWarning(const std::string &format, ...)
	{
		Console->ForegroundColor = ConsoleColor::Red;
		std::cout << std::endl;
		Console::ResetColor();
	}

	void Program::WriteError(const std::string &format, ...)
	{
		WriteWarning(format, arg);
		//Let the user see the error
		std::cout << "Press enter to continue anyway... ";
		Console::ReadLine();
	}

	void Program::ExtractIDX(IDXFile *idx, Stream *img, bool recurse = false, const std::string &tfolder = "export/", const std::string &name = "")
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var imgf = new IMGFile(img, leaveOpen: true))
		IMGFile *imgf = new IMGFile(img, leaveOpen: true);
		try
		{
			std::vector<Tuple<IDXFile*, std::string>*> *idxs = std::vector<Tuple<IDXFile*, std::string>*>();
			unsigned int i = 0, total = idx->getCount();
			for (IDX_Tools::IDXFile::const_iterator entry = idx->begin(); entry != idx->end(); ++entry)
			{
				std::string filename = (*entry)->FileName();
				if (recurse)
				{
					switch ((*entry)->Hash)
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
							idxs->Add(new Tuple<IDXFile*, std::string>(new IDXFile(imgf->GetFileStream(*entry)), Path::GetFileNameWithoutExtension(filename)->substr(3)));
							Debug::WriteLine("  Added IDX to list");
							break;
					}
				}
				if (advanced)
				{
					if (name == "KH2")
					{
						std::cout << "-----------File " << std::setw(4) << ++i << "/" << total << ", using " << name << std::endl;
					}
					else
					{
						if (name == "OVL")
						{
							std::cout << "-----------File " << std::setw(4) << ++i << "/" << total << ", using " << name << std::endl;
						}
						else
						{
							std::cout << "-----------File " << std::setw(4) << ++i << "/" << total << ", using 000" << name << std::endl;
						}
					}
					std::cout << "Dual Hash flag: " << (*entry)->getIsDualHash() << std::endl; //Always false but anyways
					std::cout << "Hashed filename: " << (*entry)->Hash << "\nHashAlt: " << (*entry)->HashAlt << std::endl;
					std::cout << "Compression flags: " << (*entry)->getIsCompressed() << std::endl;
					std::cout << "Size (packed): " << (*entry)->getCompressedDataLength() << std::endl;
					std::cout << "Real name: " << filename << std::endl;
				}
				else
				{
					std::cout << "[" << name << ": " << std::setw(4) << ++i << "/" << total << "]\tExtracting " << filename << std::endl;
				}
				filename = Path::GetFullPath(tfolder + filename);
				Directory::CreateDirectory(Path::GetDirectoryName(filename));
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//				using (var output = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				FileStream *output = new FileStream(filename, FileMode::Create, FileAccess::ReadWrite, FileShare::None);
				try
				{
					bool adSize = advanced;
					imgf->ReadFile(*entry, output, adSize);
				}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
				finally
				{
					if (output != 0)
						output.Dispose();
				}
			}
			if (recurse && idxs->Count != 0)
			{
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
				for (std::vector::const_iterator sidx = idxs->begin(); sidx != idxs->end(); ++sidx)
				{
					ExtractIDX((*sidx)->Item1, img, false, tfolder, (*sidx)->Item2);
				}
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (imgf != 0)
				imgf.Dispose();
		}
	}

	void Program::ExtractISO(Stream *isofile, const std::string &tfolder = "export/")
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (var iso = new ISOFileReader(isofile))
		ISOFileReader *iso = new ISOFileReader(isofile);
		try
		{
			std::vector<IDXFile*> *idxs = std::vector<IDXFile*>();
			std::vector<std::string> *idxnames = std::vector<std::string>();
			int i = 0;
			for (ISO_Tools::ISOFileReader::const_iterator file = iso->begin(); file != iso->end(); ++file)
			{
				++i;
				std::string filename = (*file)->getFullName();
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
				if (filename.EndsWith(".IDX"))
				{
					idxs->Add(new IDXFile(iso->GetFileStream(*file)));
					idxnames->Add(Path::GetFileNameWithoutExtension(filename));
					//continue;
					//Write the IDX too
				}
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
				else if (filename.EndsWith(".IMG") && idxnames->Contains(Path::GetFileNameWithoutExtension(filename)))
				{
					continue;
				}
				std::cout << "[ISO: " << std::setw(3) << i << "]\tExtracting " << filename << std::endl;
				filename = Path::GetFullPath(tfolder + "ISO/" + filename);
				try
				{
					Directory::CreateDirectory(Path::GetDirectoryName(filename));
				}
				catch (IOException *e)
				{
					WriteError("Failed creating directory: {0}", e->Message);
					continue;
				}
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//				using (var output = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				FileStream *output = new FileStream(filename, FileMode::Create, FileAccess::ReadWrite, FileShare::None);
				try
				{
					iso->CopyFile(*file, output);
				}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
				finally
				{
					if (output != 0)
						output.Dispose();
				}
			}
			for (i = 0; i < idxs->Count; ++i)
			{
				try
				{
					FileDescriptor *file = iso->FindFile(idxnames[i] + ".IMG");
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//					using (Substream img = iso.GetFileStream(file))
					Substream *img = iso->GetFileStream(file);
					try
					{
						ExtractIDX(idxs[i], img, true, tfolder + "" + idxnames[i] + "/", idxnames[i]);
					}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
					finally
					{
						if (img != 0)
							img.Dispose();
					}
				}
				catch (FileNotFoundException *e1)
				{
					WriteError("ERROR: Failed to find matching IMG for IDX");
				}
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (iso != 0)
				iso.Dispose();
		}
	}

	MemoryStream *Program::PatchIDXInternal(Stream *sidx, Stream *simg, Stream *timg, long long imgOffset, unsigned int parenthash = 0)
	{
		//Generate Parent name
		std::string parentname;
		if (parenthash == 0)
		{
			parentname = "KH2";
		}
		else if (parenthash == 1)
		{
			parentname = "OVL";
		}
		else if (HashPairs::pairs.TryGetValue(parenthash, parentname))
		{
			parentname = parentname.substr(3, parentname.find('.') - 3);
		}
		else
		{
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to 'ToString':
			parentname = parenthash.ToString("X8");
		}
		//Need more using
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var idx = new IDXFile(sidx, leaveOpen: true))
		IDXFile *idx = new IDXFile(sidx, leaveOpen: true);
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var img = new IMGFile(simg, leaveOpen: true))
		IMGFile *img = new IMGFile(simg, leaveOpen: true);
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (var npair = new IDXIMGWriter(timg, imgOffset, true))
		IDXIMGWriter *npair = new IDXIMGWriter(timg, imgOffset, true);
		try
		{
			unsigned int i = 0, total = idx->getCount();
			for (IDX_Tools::IDXFile::const_iterator file = idx->begin(); file != idx->end(); ++file)
			{
				std::cout << "[" << parentname << ": " << std::setw(4) << ++i << "/" << total << "]\t" << (*file)->FileName();
				switch ((*file)->Hash)
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
						std::cout << "\tRe-Building..." << std::endl;
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//						using (Substream oidx = img.GetFileStream(file))
						Substream *oidx = img->GetFileStream(*file);
						try
						{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//						using (MemoryStream subidx = PatchIDXInternal(oidx, simg, timg, imgOffset, file.Hash))
						MemoryStream *subidx = PatchIDXInternal(oidx, simg, timg, imgOffset, (*file)->Hash);
						try
						{
							npair->AddFile(new IDXFile::IDXEntry {Hash = (*file)->Hash, HashAlt = (*file)->HashAlt, IsDualHash = (*file)->getIsDualHash(), DataLength = static_cast<unsigned int>(subidx->Length), IsCompressed = false, CompressedDataLength = static_cast<unsigned int>(subidx->Length)}, subidx);
						}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
						finally
						{
							if (subidx != 0)
								subidx.Dispose();
						}
						}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
						finally
						{
							if (oidx != 0)
								oidx.Dispose();
						}
						continue;
				}
				PatchManager::Patch *patch;
				// Could make sure the parents match perfectly, but there's only 1 of every name anyway.
				// So I'll settle for just making sure the file isn't made for the ISO.
				if (patches->patches.TryGetValue((*file)->Hash, patch) && !patch->getIsinISO()) //patch.Parent == parenthash
				{
					patch->IsNew = false;
					if (patch->getIsRelink())
					{
						try
						{
							npair->RelinkFile((*file)->Hash, patch->Relink);
							std::cout << "\tRelinking..." << std::endl;
						}
						catch (...)
						{
							std::cout << "\tDeferred Relinking..." << std::endl;
							// Add the patch to be processed later, in the new file block
							patch->Parent = parenthash;
							patches->AddToNewFiles(patch);
						}
						continue;
					}
					std::cout << "\tPatching..." << std::endl;
					try
					{
						npair->AddFile(new IDXFile::IDXEntry {Hash = (*file)->Hash, HashAlt = (*file)->HashAlt, IsDualHash = (*file)->getIsDualHash(), DataLength = patch->UncompressedSize, IsCompressed = patch->Compressed, CompressedDataLength = patch->CompressedSize}, patch->Stream);
						continue;
					}
					catch (std::exception &e)
					{
						WriteError(" ERROR Patching: " + e.what());
	#if defined(DEBUG)
						WriteError(e.StackTrace);
	#endif
					}
				}
				std::cout << "" << std::endl;
				npair->AddFile(*file, img->GetFileStream(*file));
			}
			//Check for new files to add
			std::vector<unsigned int> newfiles;
			if (patches->newfiles.TryGetValue(parenthash, newfiles))
			{
				for (std::vector<unsigned int>::const_iterator hash = newfiles.begin(); hash != newfiles.end(); ++hash)
				{
					PatchManager::Patch *patch;
					if (patches->patches.TryGetValue(*hash, patch) && patch->IsNew)
					{
						patch->IsNew = false;
						std::string fname;
						if (!HashPairs::pairs.TryGetValue(*hash, fname))
						{
							fname = std::string::Format("@noname/{0:X8}.bin", *hash);
						}
						std::cout << "[" << parentname << ": NEW]\t" << fname;
						try
						{
							if (patch->getIsRelink())
							{
								std::cout << "\tAdding link..." << std::endl;
								npair->RelinkFile(*hash, patch->Relink);
							}
							else
							{
								std::cout << "\tAdding file..." << std::endl;
								npair->AddFile(new IDXFile::IDXEntry {Hash = *hash, HashAlt = 0, IsDualHash = false, DataLength = patch->UncompressedSize, IsCompressed = patch->Compressed, CompressedDataLength = patch->CompressedSize}, patch->Stream);
							}
						}
						catch (FileNotFoundException *e1)
						{
							std::cout << " WARNING Failed to find the file to add!" << std::endl;
						}
						catch (std::exception &e)
						{
							WriteError(" ERROR adding file: {0}", e.what());
						}
					}
				}
			}
			return npair->GetCurrentIDX();
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (npair != 0)
				npair.Dispose();
		}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (img != 0)
				img.Dispose();
		}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (idx != 0)
				idx.Dispose();
		}
	}

	MemoryStream *Program::PatchIDX(Stream *idx, Stream *img, FileDescriptor *imgd, ISOCopyWriter *niso, bool IsOVL = false)
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (idx)
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (img)
		try
		{
			niso->SeekEnd();
			long long imgOffset = niso->getfile()->Position;
			MemoryStream *idxms = PatchIDXInternal(idx, img, niso->getfile(), imgOffset, IsOVL ? 1u : 0u);
			imgd->ExtentLBA = static_cast<unsigned int>(imgOffset) / 2048;
			imgd->ExtentLength = static_cast<unsigned int>(niso->getfile()->Position - imgOffset);
			imgd->RecordingDate = DateTime::UtcNow;
			niso->PatchFile(imgd);
			niso->SeekEnd();
			return idxms;
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (img != 0)
				img.Dispose();
		}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (idx != 0)
				idx.Dispose();
		}
	}

	void Program::PatchISO(Stream *isofile, Stream *nisofile)
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (var iso = new ISOFileReader(isofile))
		ISOFileReader *iso = new ISOFileReader(isofile);
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (var niso = new ISOCopyWriter(nisofile, iso))
		ISOCopyWriter *niso = new ISOCopyWriter(nisofile, iso);
		try
		{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'StartsWith' method:
			if (iso->getPrimaryVolumeDescriptor()->AbstractFileIdentifier.StartsWith("KH2NONSTANDARD", StringComparison::InvariantCultureIgnoreCase))
			{
				throw new NotSupportedException("This KH2 ISO was modified to use custom data formats which are incompatible with the normal game. This patcher cannot work with this ISO.");
			}
			unsigned int i = 0;
			Trivalent cKh2 = patches->getKH2Changed() ? ChangesPending : NoChanges, cOvl = patches->getOVLChanged() ? ChangesPending : NoChanges;
			bool cIso = patches->getISOChanged();
			for (ISO_Tools::ISOFileReader::const_iterator file = iso->begin(); file != iso->end(); ++file)
			{
				std::cout << "[ISO: " << std::setw(4) << ++i << "]\t" << (*file)->getFullName();
				std::string name = (*file)->getFileName();
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
				if (name.EndsWith("KH2.IDX") || name.EndsWith("KH2.IMG"))
				{
					if (cKh2::HasFlag(ChangesPending))
					{
						cKh2 = Changed;
						long long lpos = niso->getfile()->Position;
						std::cout << "\tRe-Building..." << std::endl;
						try
						{
							FileDescriptor *img = iso->FindFile("KH2.IMG"), *idx = iso->FindFile("KH2.IDX");
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//							using (MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso))
							MemoryStream *ms = PatchIDX(iso->GetFileStream(idx), iso->GetFileStream(img), img, niso);
							try
							{
								idx->RecordingDate = DateTime::UtcNow;
								niso->AddFile2(idx, ms, name);
							}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
							finally
							{
								if (ms != 0)
									ms.Dispose();
							}
							continue;
						}
						catch (std::exception &e)
						{
							WriteError(" Error creating IDX/IMG: {0}\n{1}", e.what(), e.StackTrace);
							niso->getfile()->Position = lpos;
						}
					}
					else if (cKh2::HasFlag(Changed))
					{
						std::cout << "\tRe-Built" << std::endl;
						continue;
					}
				}
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
				else if (name.EndsWith("OVL.IDX") || name.EndsWith("OVL.IMG"))
				{
					if (cOvl::HasFlag(ChangesPending))
					{
						cOvl = Changed;
						long long lpos = niso->getfile()->Position;
						std::cout << "\tRe-Building..." << std::endl;
						try
						{
							FileDescriptor *img = iso->FindFile("OVL.IMG"), *idx = iso->FindFile("OVL.IDX");
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//							using (MemoryStream ms = PatchIDX(iso.GetFileStream(idx), iso.GetFileStream(img), img, niso, true))
							MemoryStream *ms = PatchIDX(iso->GetFileStream(idx), iso->GetFileStream(img), img, niso, true);
							try
							{
								idx->RecordingDate = DateTime::UtcNow;
								niso->AddFile2(idx, ms, name);
							}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
							finally
							{
								if (ms != 0)
									ms.Dispose();
							}
							continue;
						}
						catch (std::exception &e)
						{
							WriteError(" Error creating IDX/IMG: " + e.what());
							niso->getfile()->Position = lpos;
						}
					}
					else if (cOvl::HasFlag(Changed))
					{
						std::cout << "\tRe-Built" << std::endl;
						continue;
					}
				}
				else if (cIso)
				{
					PatchManager::Patch *patch;
					if (patches->patches.TryGetValue(PatchManager::ToHash(name), patch) && patch->getIsinISO())
					{
						std::cout << "\tPatching..." << std::endl;
						(*file)->RecordingDate = DateTime::UtcNow;
						niso->AddFile2(*file, patch->Stream, name);
						continue;
					}
				}
				std::cout << "" << std::endl;
				niso->CopyFile(*file);
				if (niso->getSectorCount() >= 0x230540)
				{
					WriteWarning("Warning: This ISO has the size of a dual-layer ISO, but it isn't one. Some\nprograms may take a while to start while they search for the 2nd layer.");
				}
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (niso != 0)
				niso.Dispose();
		}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (iso != 0)
				iso.Dispose();
		}
	}

	void Program::Main(std::string& args[])
	{
		bool log = false;
		Console->Title = program->ProductName + " " + program->FileVersion + " [" + program->CompanyName + "]";
	#if defined(DEBUG)
		try
		{
			File::Delete("debug.log");
		}
		catch (...)
		{
		}
		Debug->AutoFlush = true;
		Debug::Listeners->Add(new TextWriterTraceListener("debug.log"));
	#endif
		//Arguments
		std::string isoname = "";
		bool batch = false, extract = false;


		for (std::string::const_iterator arg = args->begin(); arg != args->end(); ++arg)
		{
//C# TO C++ CONVERTER NOTE: The following 'switch' operated on a string variable and was converted to C++ 'if-else' logic:
//			switch (arg)
//ORIGINAL LINE: case "-exit":
			if (arg == "-exit")
			{
					return;
			}
//ORIGINAL LINE: case "-batch":
			else if (arg == "-batch")
			{
					batch = true;
			}
//ORIGINAL LINE: case "-extractor":
			else if (arg == "-extractor")
			{
					extract = true;
			}
//ORIGINAL LINE: case "-advancedinfo":
			else if (arg == "-advancedinfo")
			{
					advanced = true;
			}
//ORIGINAL LINE: case "-log":
			else if (arg == "-log")
			{
					log = true;
			}
//ORIGINAL LINE: case "-help":
			else if (arg == "-help")
			{
//ORIGINAL LINE: byte[] buffer = Encoding.ASCII.GetBytes(Resources.Readme);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
					unsigned char *buffer = Encoding::ASCII->GetBytes(Resources::getReadme());
					File::WriteAllBytes("Readme.txt", buffer);
					std::cout << "Help extracted as a Readme\nPress enter to leave the software...";
					const &std::getcin(); const
					return;
			}
//ORIGINAL LINE: case "-patchmaker":
			else if (arg == "-patchmaker")
			{
					KH2ISO_PatchMaker::Program::Mainp(args);
			}
			else
			{
					if (File::Exists(*arg))
					{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
						if (isoname == "" && (*arg).EndsWith(".iso", StringComparison::InvariantCultureIgnoreCase))
						{
							isoname = *arg;
						}
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
						else if ((*arg).EndsWith(".kh2patch", StringComparison::InvariantCultureIgnoreCase))
						{
							patches->AddPatch(*arg);
						}
					}

			}
		} //TODO patch after header



		if (log)
		{
			FileStream *filestream = new FileStream("log.log", FileMode::Create);
			StreamWriter *streamwriter = new StreamWriter(filestream);
			streamwriter->AutoFlush = true;
			Console::SetOut(streamwriter);
			Console::SetError(streamwriter);
				//TODO Redirect to a txt, but problem: make disappear the text on the console. Need to mirror the text
		}
		if (isoname == "")
		{
			isoname = "KH2FM.ISO";
			Console->ForegroundColor = ConsoleColor::Gray;
			setbuilddate(RetrieveLinkerTimestamp());
			std::cout << program->ProductName << "\nBuild Date: " << builddate << "\nVersion " << program->FileVersion;
			Console::ResetColor();
	#if defined(DEBUG)
			Console->ForegroundColor = ConsoleColor::Red;
			std::cout << "\nPRIVATE RELEASE\n";
			Console::ResetColor();
	#else
			std::cout << "\nPUBLIC RELEASE\n";
	#endif
	#if defined(extract)
			Console->ForegroundColor = ConsoleColor::Red;
			std::cout << "\nFUCKING EXTRACTOR EDITION!!!EXTRACTING & PATCHING THE GAMES WITH A KH2PATCH!!\n";
			Console::ResetColor();
	#endif

			Console->ForegroundColor = ConsoleColor::DarkMagenta;
			std::cout << "\nProgrammed by " << program->CompanyName;
			Console->ForegroundColor = ConsoleColor::Gray;
			if (extract)
			{
				std::cout << "\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nHe is using a list for extracting those files, which is not complete.\nBut this is the most complete one for now.\nHe can extract the files KH2.IMG and OVL.IMG\n\n";
			}
			else
			{
				std::cout << "\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nHe can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files for avoid\na corrupted game. He can add some files too.\n\n";
			}
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
			HashPairs::loadHashPairs(printInfo: true);
			Console->ForegroundColor = ConsoleColor::Green;
			std::cout << "\nPress enter to run using the file:";
			Console::ResetColor();
			std::cout << " " << isoname;
			if (!batch)
			{
				Console::ReadLine();
			}
		}
		else
		{
			Console->ForegroundColor = ConsoleColor::Gray;
			setbuilddate(RetrieveLinkerTimestamp());
			std::cout << program->ProductName << "\nBuild Date: " << builddate << "\nVersion " << program->FileVersion;
			Console::ResetColor();
	#if defined(DEBUG)
			Console->ForegroundColor = ConsoleColor::Red;
			std::cout << "\nPRIVATE RELEASE\n";
			Console::ResetColor();
	#else
			std::cout << "\nPUBLIC RELEASE\n";
	#endif
	#if defined(extract)
			Console->ForegroundColor = ConsoleColor::Red;
			std::cout << "\nFUCKING EXTRACTOR EDITION!!!EXTRACTING & PATCHING THE GAMES WITH A KH2PATCH!!\n";
			Console::ResetColor();
	#endif
			Console->ForegroundColor = ConsoleColor::DarkMagenta;
			std::cout << "\nProgrammed by " << program->CompanyName;
			Console->ForegroundColor = ConsoleColor::Gray;
			if (extract)
			{
				std::cout << "\n\nThis tool is able to extract the files of the game Kingdom Hearts 2(Final Mix).\nHe is using a list for extracting those files, which is not complete.\nBut this is the most complete one for now.\nHe can extract the files KH2.IMG and OVL.IMG\n\n";
			}
			else
			{
				std::cout << "\n\nThis tool is able to patch the game Kingdom Hearts 2(Final Mix).\nHe can modify iso files, like the elf and internal files,\nwich are stored inside KH2.IMG and OVL.IMG\nThis tool is recreating too new hashes into the idx files for avoid\na corrupted game. He can add some files too.\n\n";
			}
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
			HashPairs::loadHashPairs(printInfo: true);
			Console->ForegroundColor = ConsoleColor::Green;
			std::cout << "\nPress enter to run using the file:";
			Console::ResetColor();
			std::cout << " " << isoname;
			if (!batch)
			{
				Console::ReadLine();
			}

		}
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (FileStream iso = File.Open(isoname, FileMode.Open, FileAccess.Read, FileShare.Read))
			FileStream *iso = File->Open(isoname, FileMode::Open, FileAccess::Read, FileShare::Read);
			try
			{
				if (extract)
				{
					ExtractISO(iso);
				}
				else
				{
					if (patches->patches.empty())
					{
						WriteWarning("No patches specified, nothing to do!");
					}
					else
					{
						isoname = Path::ChangeExtension(isoname, ".new.iso");
						try
						{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//							using (FileStream niso = File.Open(isoname, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
							FileStream *niso = File->Open(isoname, FileMode::Create, FileAccess::ReadWrite, FileShare::None);
							try
							{
								PatchISO(iso, niso);
							}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
							finally
							{
								if (niso != 0)
									niso.Dispose();
							}
						}
						catch (std::exception &e1)
						{
							//Delete the new "incomplete" iso
							File::Delete(isoname);
							throw;
						}
					}
				}
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (iso != 0)
					iso.Dispose();
			}
		}
		catch (FileNotFoundException *e)
		{
			WriteWarning("Failed to open file: " + e->Message);
		}
		catch (std::exception &e)
		{
			WriteWarning("An error has occured! Please report this, including the following information:\n{1}: {0}\n{2}", e.what(), e.GetType()->FullName, e.StackTrace);
		}
		delete patches;
		if (!batch)
		{
			std::cout << "\nPress enter to exit...";
			Console::ReadLine();
		}
	}
}
