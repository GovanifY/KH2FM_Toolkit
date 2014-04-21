#include "Program.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections::Generic;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Globalization;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Reflection;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Text;
using namespace GovanifY::Utility;
using namespace HashList;
using namespace KH2FM_Toolkit;
using namespace KHCompress;
typedef KH2FM_Toolkit::Program ISOTP;

namespace KH2ISO_PatchMaker
{

	PatchFile::FileEntry::~FileEntry()
	{
		InitializeInstanceFields();
		if (Data != 0)
		{
			delete Data;
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete Data;
		}
	}

	void PatchFile::FileEntry::InitializeInstanceFields()
	{
		delete Data;
		Hash = 0;
		IsCompressed = false;
		IsNewFile = false;
		ParentHash = 0;
		Relink = 0;
		name = "";
	}

	const std::string &PatchFile::getAuthor() const
	{
		return Encoding::ASCII->GetString(_Author);
	}

	void PatchFile::setAuthor(const std::string &value)
	{
		_Author = Encoding::ASCII->GetBytes(value + '\0');
	}

	const std::string &PatchFile::getOtherInfo() const
	{
		return Encoding::ASCII->GetString(_OtherInfo);
	}

	void PatchFile::setOtherInfo(const std::string &value)
	{
		if (convertLinebreaks)
		{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Replace' method:
			value = value.Replace("\\n", "\r\n");
		}
		_OtherInfo = Encoding::ASCII->GetBytes(value + '\0');
	}

	void PatchFile::AddChange(const std::string &s)
	{
		if (convertLinebreaks)
		{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Replace' method:
			s = s.Replace("\\n", "\r\n");
		}
		Changelogs.push_back(Encoding::ASCII->GetBytes(s + '\0'));
	}

	void PatchFile::AddCredit(const std::string &s)
	{
		if (convertLinebreaks)
		{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Replace' method:
			s = s.Replace("\\n", "\r\n");
		}
		Credits.push_back(Encoding::ASCII->GetBytes(s + '\0'));
	}

	void PatchFile::WriteDecrypted(Stream *stream)
	{
		stream->Position = 0;
		unsigned int changeLen = 0, creditLen = 0;
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
		for (std::vector<unsigned char[]>::const_iterator b = Changelogs.begin(); b != Changelogs.end(); ++b)
		{
			changeLen += 4 + static_cast<unsigned int>((*b)->Length);
		}
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
		for (std::vector<unsigned char[]>::const_iterator b = Credits.begin(); b != Credits.end(); ++b)
		{
			creditLen += 4 + static_cast<unsigned int>((*b)->Length);
		}
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var bw = new BinaryStream(stream, leaveOpen: true))
		BinaryStream *bw = new BinaryStream(stream, leaveOpen: true);
		try
		{
			unsigned int i;
			bw->Write(Signature);
			bw->Write(static_cast<unsigned int>(16 + sizeof(_Author) / sizeof(_Author[0])));
			bw->Write(static_cast<unsigned int>(16 + sizeof(_Author) / sizeof(_Author[0]) + 16 + changeLen + 4 + creditLen + sizeof(_OtherInfo) / sizeof(_OtherInfo[0])));
			bw->Write(Version);
			bw->Write(_Author);
			bw->Write(static_cast<unsigned int>(12));
			bw->Write(16 + changeLen);
			bw->Write(16 + changeLen + 4 + creditLen);
			bw->Write(i = static_cast<unsigned int>(Changelogs.size()));
			i *= 4;
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
			for (std::vector<unsigned char[]>::const_iterator b = Changelogs.begin(); b != Changelogs.end(); ++b)
			{
				bw->Write(i);
				i += static_cast<unsigned int>((*b)->Length);
			}
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
			for (std::vector<unsigned char[]>::const_iterator b = Changelogs.begin(); b != Changelogs.end(); ++b)
			{
				bw->Write(*b);
			}
			bw->Write(i = static_cast<unsigned int>(Credits.size()));
			i *= 4;
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
			for (std::vector<unsigned char[]>::const_iterator b = Credits.begin(); b != Credits.end(); ++b)
			{
				bw->Write(i);
				i += static_cast<unsigned int>((*b)->Length);
			}
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
			for (std::vector<unsigned char[]>::const_iterator b = Credits.begin(); b != Credits.end(); ++b)
			{
				bw->Write(*b);
			}
			bw->Write(_OtherInfo);
			bw->Write(static_cast<unsigned int>(Files.size()));

			//Check total size to add
			long long fileTotal = 0;
			try
			{
				for (std::vector<FileEntry*>::const_iterator file = Files.begin(); file != Files.end(); ++file)
				{
					if ((*file)->Relink == 0)
					{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'checked' in this context:
//ORIGINAL LINE: fileTotal = checked(fileTotal + file.Data.Length);
						fileTotal = fileTotal + (*file)->Data->Length;
					}
				}
			}
			catch (OverflowException *e1)
			{
				ISOTP::WriteError("That's WAY too much file data... is there even that much in the gameo.O?\r\nTry to split up the patch...");
				return;
			}
			Stream *filedata = 0;
			std::string filename = "";
			//Use a MemoryStream if we can, much cleaner\faster
			if (fileTotal <= int::MaxValue)
			{
				try
				{
					filedata = new MemoryStream(static_cast<int>(fileTotal));
				}
				catch (OutOfMemoryException *e2)
				{
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
					delete filedata;
					ISOTP::WriteWarning("Failed to allocate enough memory, trying temporary file fallback...");
				}
			}
			//If we can't use a MemStream (or that failed), try a FileStream as a temp file
			if (filedata == 0)
			{
				filename = Path::GetTempFileName();
				std::cout << "Wow there's a lot of file data! Using a temporary file now!\r\nUsing " << filename << std::endl;
				filedata = File->Open(filename, FileMode::Create, FileAccess::ReadWrite, FileShare::None);
			}
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (filedata)
			try
			{
				i = static_cast<unsigned int>(stream->Position + Files.size()*92);
				for (std::vector<FileEntry*>::const_iterator file = Files.begin(); file != Files.end(); ++file)
				{
					bw->Write((*file)->Hash);
					if ((*file)->Relink != 0)
					{
						bw->Write(static_cast<unsigned int>(0));
						bw->Write(static_cast<unsigned int>(0));
						bw->Write(static_cast<unsigned int>(0));
						bw->Write((*file)->ParentHash);
						bw->Write((*file)->Relink);
						bw->Write(static_cast<unsigned int>(0));
					}
					else
					{
						unsigned int cSize;
						(*file)->Data->Position = 0;
						if ((*file)->IsCompressed)
						{
							try
							{
								unsigned char input[(*file)->Data->Length];
								(*file)->Data->Read(input, 0, static_cast<int>((*file)->Data->Length));
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to 'ToString':
								std::cout << "Compressing " << (*file)->name != "" ? (*file)->name : (*file)->Hash->ToString("X8");
//ORIGINAL LINE: byte[] output = KH2Compressor.compress(input);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
								unsigned char *output = KH2Compressor::compress(input);
								unsigned int cSizeSectors = static_cast<unsigned int>(ceil(static_cast<double>(sizeof(output) / sizeof(output[0])) / 2048)) - 1;
								if (output->LongLength > int::MaxValue)
								{
									throw new NotSupportedException("Compressed data too big to store (Program limitation)");
								}
								if (cSizeSectors > 0x2FFF)
								{
									throw new NotSupportedException("Compressed data too big to store (IDX limitation)");
								}
								if ((cSizeSectors & 0x1000u) == 0x1000u)
								{
									throw new NotSupportedException("Compressed data size hit 0x1000 bit limitation (IDX limitation)");
								}
								cSize = static_cast<unsigned int>(sizeof(output) / sizeof(output[0]));
								filedata->Write(output, 0, sizeof(output) / sizeof(output[0]));
							}
							catch (NotCompressableException *e)
							{
								std::string es = "ERROR: Failed to compress file: " + e->what();
								ISOTP::WriteWarning(es);
								std::cout << "Add it without compressing? [Y/n] ";
								if (Program::GetYesNoInput())
								{
									(*file)->IsCompressed = false;
									cSize = static_cast<unsigned int>((*file)->Data->Length);
									(*file)->Data->Position = 0; //Ensure at beginning
									(*file)->Data->CopyTo(filedata);
								}
								else
								{
									throw new NotCompressableException(es, e);
								}
							}
						}
						else
						{
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to 'ToString':
							std::cout << "Adding " << (*file)->name != "" ? (*file)->name : (*file)->Hash->ToString("X8") << std::endl;
							cSize = static_cast<unsigned int>((*file)->Data->Length);
							(*file)->Data->Position = 0; //Ensure at beginning
							(*file)->Data->CopyTo(filedata);
						}
						if (!(*file)->IsCompressed && ((static_cast<unsigned int>(ceil(static_cast<double>(cSize) / 2048)) - 1) & 0x1000u) == 0x1000u)
						{
							ISOTP::WriteWarning("Data size hit 0x1000 bit limitation, but this file may be OK if it's streamed.");
						}
						bw->Write(i);
						i += cSize;
						bw->Write(cSize);
						bw->Write(static_cast<unsigned int>((*file)->Data->Length));
						bw->Write((*file)->ParentHash);
						bw->Write(static_cast<unsigned int>(0));
						bw->Write(static_cast<unsigned int>((*file)->IsCompressed ? 1 : 0));
					}
					bw->Write(static_cast<unsigned int>((*file)->IsNewFile ? 1 : 0)); //Custom
					//Padding
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
					bw->Write(static_cast<unsigned int>(0));
				}
				filedata->Position = 0; //Ensure at beginning
				filedata->CopyTo(stream);
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (filedata != 0)
					filedata.Dispose();
			}
			//If we used a temp file, delete it
			if (filename != "")
			{
				File::Delete(filename);
				filename = "";
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (bw != 0)
				bw.Dispose();
		}
	}

	void PatchFile::Write(Stream *stream)
	{
		if (Program::DoXeey == false)
		{
//ORIGINAL LINE: byte[] data;
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
			unsigned char *data;
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (var ms = new MemoryStream())
			MemoryStream *ms = new MemoryStream();
			try
			{
				WriteDecrypted(ms);
				data = ms->ToArray();
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (ms != 0)
					ms.Dispose();
			}
			PatchManager::GYXor(data);
			stream->Write(data, 0, sizeof(data) / sizeof(data[0]));
		}
		else
		{
//ORIGINAL LINE: byte[] data;
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
			unsigned char *data;
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (var ms = new MemoryStream())
			MemoryStream *ms = new MemoryStream();
			try
			{
				WriteDecrypted(ms);
				data = ms->ToArray();
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (ms != 0)
					ms.Dispose();
			}
			PatchManager::XeeyXor(data);
			stream->Write(data, 0, sizeof(data) / sizeof(data[0]));
		}
	}

	void PatchFile::InitializeInstanceFields()
	{
		Credits = std::vector<unsigned char[]>();
		Files = std::vector<FileEntry*>();
		Version = 1;
		_Author = {0};
		_OtherInfo = {0};
		convertLinebreaks = true;
	}

bool Program::DoXeey = false;
bool Program::NewFormat = true;
bool Program::Compression = false;
bool Program::hvs = false;

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

	unsigned int Program::GetFileAsInput(std::string &name, bool &blank)
	{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Replace' method:
		std::cin::getline(std::string inp, sizeof(std::string inp))->Replace("\"", "")->Trim();
		unsigned int hash;
		name = "";
		if (inp.length() == 0)
		{
			blank = true;
			return 0;
		}
		blank = false;
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'StartsWith' method:
		if (inp.StartsWith("0x", StringComparison::InvariantCultureIgnoreCase))
		{
			if (unsigned int::TryParse(inp.substr(2), NumberStyles::HexNumber, CultureInfo::InvariantCulture, hash))
			{
				name = HashPairs::NameFromHash(hash);
			}
			else
			{
				ISOTP::WriteWarning("Error: Failed to parse as hex number.");
				return 0;
			}
		}
		else
		{
			hash = PatchManager::ToHash(inp);
			//Check hashpairs anyway, and warn if something unexpected returns
			if (!hvs)
			{
				if (!HashPairs::pairs.TryGetValue(hash, name))
				{
					std::cout << " Warning: Filename not found into the Hashlist." << std::endl;
				}
				else if (name != inp)
				{
					ISOTP::WriteWarning(" Warning: Hash conflict with {0}; both contain the same hash.", name);
				}
				name = inp;
			}
		}
		return hash;
	}

	unsigned int Program::GetFileHashAsInput(std::string &name)
	{
		bool blank;
		return GetFileAsInput(name, blank);
	}

	bool Program::GetYesNoInput()
	{
		int cL = Console::CursorLeft, cT = Console::CursorTop;
		do
		{
			std::cin::getline(std::string inp, sizeof(std::string inp));
			if (inp == "Y" || inp == "y")
			{
				return true;
			}
			if (inp == "N" || inp == "n")
			{
				return false;
			}
			Console::SetCursorPosition(cL, cT);
			Console::Beep();
		} while (true);
	}

	void Program::Mainp(std::string& args[])
	{
		bool log = false;

		Console->Title = ISOTP::program->ProductName + " " + ISOTP::program->FileVersion + " [" + ISOTP::program->CompanyName + "]";
		PatchFile *patch = new PatchFile();
		bool encrypt = true, batch = false, authorSet = false, verSet = false, changeSet = false, creditSet = false, otherSet = false;
		std::string output = "output.kh2patch";
		for (int i = 0; i < sizeof(args) / sizeof(args[0]); ++i)
		{
//C# TO C++ CONVERTER NOTE: The following 'switch' operated on a string variable and was converted to C++ 'if-else' logic:
//			switch (args[i].ToLowerInvariant())
//ORIGINAL LINE: case "-xeeynamo":
			if (args[i].ToLowerInvariant() == "-xeeynamo")
			{
					DoXeey = true;
					ISOTP::WriteWarning("Using Xeeynamo's encryption!(DESTRUCTIVE METHOD)");
			}
//ORIGINAL LINE: case "-batch":
			else if (args[i].ToLowerInvariant() == "-batch")
			{
					batch = true;
			}
//ORIGINAL LINE: case "-log":
			else if (args[i].ToLowerInvariant() == "-log")
			{
					log = true;
	#if defined(DEBUG)
			}
//ORIGINAL LINE: case "-decrypted":
			else if (args[i].ToLowerInvariant() == "-decrypted")
			{
					if (encrypt)
					{
						encrypt = false;
						std::cout << "Writing in decrypted mode!" << std::endl;
					}
	#endif
			}
//ORIGINAL LINE: case "-hashverskip":
			else if (args[i].ToLowerInvariant() == "-hashverskip")
			{
					hvs = true;
			}
//ORIGINAL LINE: case "-version":
			else if (args[i].ToLowerInvariant() == "-version")
			{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
					if (!unsigned int::TryParse(args[++i].Trim(), patch->Version))
					{
						patch->Version = 1;
					}
					else
					{
						verSet = true;
					}
			}
//ORIGINAL LINE: case "-author":
			else if (args[i].ToLowerInvariant() == "-author")
			{
					patch->setAuthor(args[++i]);
					authorSet = true;
			}
//ORIGINAL LINE: case "-other":
			else if (args[i].ToLowerInvariant() == "-other")
			{
					patch->setOtherInfo(args[++i]);
					otherSet = true;
			}
//ORIGINAL LINE: case "-changelog":
			else if (args[i].ToLowerInvariant() == "-changelog")
			{
					patch->AddChange(args[++i]);
			}
//ORIGINAL LINE: case "-skipchangelog":
			else if (args[i].ToLowerInvariant() == "-skipchangelog")
			{
					changeSet = true;
			}
//ORIGINAL LINE: case "-credits":
			else if (args[i].ToLowerInvariant() == "-credits")
			{
					patch->AddCredit(args[++i]);
			}
//ORIGINAL LINE: case "-skipcredits":
			else if (args[i].ToLowerInvariant() == "-skipcredits")
			{
					creditSet = true;
			}
//ORIGINAL LINE: case "-output":
			else if (args[i].ToLowerInvariant() == "-output")
			{
					output = args[++i];
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'EndsWith' method:
					if (!output.EndsWith(".kh2patch", StringComparison::InvariantCultureIgnoreCase))
					{
						output += ".kh2patch";
					}
			}
		}
		//TODO MENU
		if (log)
		{
			FileStream *filestream = new FileStream("log.log", FileMode::Create);
			StreamWriter *streamwriter = new StreamWriter(filestream);
			streamwriter->AutoFlush = true;
			Console::SetOut(streamwriter);
			Console::SetError(streamwriter);
		}
		if (!batch)
		{
			Console->ForegroundColor = ConsoleColor::Gray;
			DateTime builddate = RetrieveLinkerTimestamp();
			std::cout << ISOTP::program->ProductName << "\nBuild Date: " << builddate << "\nVersion " << ISOTP::program->FileVersion;
			Console::ResetColor();
	#if defined(DEBUG)
			Console->ForegroundColor = ConsoleColor::Red;
			std::cout << "\nPRIVATE RELEASE\n";
			Console::ResetColor();
	#else
			std::cout << "\nPUBLIC RELEASE\n";
	#endif
			Console->ForegroundColor = ConsoleColor::DarkMagenta;
			std::cout << "\nProgrammed by " << ISOTP::program->CompanyName;
			Console->ForegroundColor = ConsoleColor::Gray;
			std::cout << "\n\nThis tool is able to create patches for the software KH2FM_Toolkit.\nHe can add files using the internal compression of the game \nKingdom Hearts 2(Final Mix), relink files to their idx, recreate\nthe iso without size limits and without corruption.\nThis patch system is the best ever made for this game atm.\n";
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
			HashPairs::loadHashPairs(printInfo: true);
			Console->ForegroundColor = ConsoleColor::Green;
			std::cout << "\nPress enter to run using the file:";
			Console::ResetColor();
			std::cout << " " << output;

			if (!batch)
			{
				Console::ReadLine();
			}
		}
		if (!authorSet)
		{
			std::cout << "Enter author's name: ";
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
			std::cin::getline(patch->getAuthor(), sizeof(patch->getAuthor()))->Trim();
		}
		if (!verSet)
		{
			std::cout << "Enter revision number: ";
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
			while (!unsigned int::TryParse(Console::ReadLine()->Trim(), patch->Version))
			{
				ISOTP::WriteWarning("\nInvalid number! ");
			}
		}
		if (!changeSet)
		{
			std::cout << "Enter changelog lines here (leave blank to continue):" << std::endl;
			do
			{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
				std::cin::getline(std::string inp, sizeof(std::string inp))->Trim();
				if (inp.length() == 0)
				{
					break;
				}
				patch->AddChange(inp);
			} while (true);
		}
		if (!creditSet)
		{
			std::cout << "Enter credits here (leave blank to continue):" << std::endl;
			do
			{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
				std::cin::getline(std::string inp, sizeof(std::string inp))->Trim();
				if (inp.length() == 0)
				{
					break;
				}
				patch->AddCredit(inp);
			} while (true);
		}
		if (!otherSet)
		{
			std::cout << "Other information (leave blank to continue): ";
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
			std::cin::getline(patch->getOtherInfo(), sizeof(patch->getOtherInfo()))->Trim();
		}
	#if defined(DEBUG)
		std::cout << "Filenames may be formatted as text (msg/jp/lk.bar) or hash (0x030b45da)." << std::endl;
	#endif
		do
		{
			PatchFile::FileEntry *file = new PatchFile::FileEntry();
			std::cout << "\nEnter filename: ";
			std::string name, rel;
			//Target file
			file->Hash = GetFileAsInput(name, otherSet);
			if (otherSet)
			{
				break;
			}
			std::cout << "  Using \"" << name << "\" for " << std::hex << std::setw(1) << std::setprecision(8) << std::uppercase << file->Hash << std::dec << std::nouppercase << std::endl;
			//Relink
			std::cout << "Relink to this filename(ex: 000al.idx) [Blank for none]: ";
			file->Relink = GetFileHashAsInput(rel);
			if (file->Relink == 0)
			{
				try
				{
					file->Data = File->Open(name, FileMode::Open, FileAccess::Read, FileShare::Read);
				}
				catch (std::exception &e)
				{
					ISOTP::WriteWarning("Failed opening the file: " + e.what());
					continue;
				}
				file->name = Path::GetFileName(name);
				if (file->Data->Length > int::MaxValue || file->Data->Length < 10)
				{
					ISOTP::WriteWarning("Too {0} to compress. Press enter.", (file->Data->Length < 10 ? "small" : "big"));
					//Do this so the line count is the same whether we can compress or not.
					Console::ReadLine();
				}
				else
				{
					//Compress
					std::cout << "Compress this file? [Y/n] ";
					file->IsCompressed = GetYesNoInput();
				}
			}
			else
			{
				std::cout << "  Using \"" << rel << "\" for " << std::hex << std::setw(1) << std::setprecision(8) << std::uppercase << file->Relink << std::dec << std::nouppercase << std::endl;
			}
			//Parent
			std::cout << "Parent compressed file [Leave blank for KH2]: ";
			file->ParentHash = GetFileHashAsInput(rel);
			if (rel.Equals("KH2", StringComparison::InvariantCultureIgnoreCase))
			{
				file->ParentHash = 0;
			}
			else if (rel.Equals("OVL", StringComparison::InvariantCultureIgnoreCase))
			{
				file->ParentHash = 1;
			}
			else if (rel.Equals("ISO", StringComparison::InvariantCultureIgnoreCase))
			{
				file->ParentHash = 2;
			}
			else
			{
				switch (file->ParentHash)
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
			std::cout << "  Using \"" << rel << "\" for " << std::hex << std::setw(1) << std::setprecision(8) << std::uppercase << file->ParentHash << std::dec << std::nouppercase << std::endl;
			//IsNew
			std::cout << "Should this file be added if he's not in the game? [y/N] ";
			file->IsNewFile = GetYesNoInput();
			patch->Files.push_back(file);
		} while (true);
		try
		{
			//TODO Compress(buffer>magic>Compress>Write to output)
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (FileStream fs = File.Open(output, FileMode.Create, FileAccess.Write, FileShare.None))
			FileStream *fs = File->Open(output, FileMode::Create, FileAccess::Write, FileShare::None);
			try
			{
				if (encrypt)
				{
					patch->Write(fs);
				}
				else
				{
					patch->WriteDecrypted(fs);
				}
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (fs != 0)
					fs.Dispose();
			}
		}
		catch (std::exception &e)
		{
			ISOTP::WriteWarning("Failed to save file: " + e.what());
			ISOTP::WriteWarning(e.StackTrace);
			try
			{
				File::Delete("output.kh2patch");
			}
			catch (...)
			{
			}
		}
		if (!batch)
		{
			std::cout << "Press enter to exit...";
			Console::ReadLine();
			exit(0);
		}
	}
}
