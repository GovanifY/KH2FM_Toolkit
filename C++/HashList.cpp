#include "HashList.h"

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
using namespace IDX_Tools;
using namespace KH2FM_Toolkit;
using namespace KH2FM_Toolkit::Properties;

namespace HashList
{

	const std::string &HashPairs::getversion() const
	{
		return privateversion;
	}

	void HashPairs::setversion(const std::string &value)
	{
		privateversion = value;
	}

	const std::string &HashPairs::getauthor() const
	{
		return privateauthor;
	}

	void HashPairs::setauthor(const std::string &value)
	{
		privateauthor = value;
	}

	void HashPairs::loadHashPairs(const std::string &filename = "HashList.bin", bool forceReload = false, bool printInfo = false)

	{
		if (File::Exists(filename))
		{
			if (printInfo)
			{
				Console->ForegroundColor = ConsoleColor::Red;
				std::cout << "HASHLIST file found! Loading this file instead of the basic one!" << std::endl;
				Console::ResetColor();
			}
			else
			{
				Console->ForegroundColor = ConsoleColor::Red;
				Debug::WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
				Console::ResetColor();
			}
		}


//ORIGINAL LINE: byte[] Hashlist = Resources.HashList;
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
		unsigned char *Hashlist = Resources::getHashList();
		PatchManager::GYXor(Hashlist);
		std::string Hashtemp = Path::GetTempFileName();
		File::WriteAllBytes(Hashtemp, Hashlist);
		if (pairs.empty())
		{
			pairs = std::map<unsigned int, std::string>();
		}
		else if (forceReload)
		{
			pairs.clear();
		}
		else
		{
			return;
		}
		setauthor("");
	setversion(getauthor());
		if (!File::Exists(filename)) // If it's not in the current directory, try the EXE's location
		{
			filename = Path::GetDirectoryName(Assembly::GetExecutingAssembly()->Location) + "\\" + filename;
			if (File::Exists(filename))
			{
				if (printInfo)
				{
					Console->ForegroundColor = ConsoleColor::Red;
					std::cout << "HASHLIST file found! Loading this file instead of the basic one!" << std::endl;
					Console::ResetColor();
				}
				else
				{
					Console->ForegroundColor = ConsoleColor::Red;
					Debug::WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
					Console::ResetColor();
				}
			}
		}
		if (!File::Exists(filename))
		{


//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (var rd = new StreamReader(File.Open(Hashtemp, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8, false, 1024))
			StreamReader *rd = new StreamReader(File->Open(Hashtemp, FileMode::Open, FileAccess::Read, FileShare::Read), Encoding::UTF8, false, 1024);
			try
			{
				std::string line;
				int index;
				while ((line = rd->ReadLine()) != 0)
				{
					if (line.length() != 0)
					{
						if (line[0] == '#')
						{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'StartsWith' method:
							if (line.StartsWith("#Version ") && line.length() > 9)
							{
								//^#Version ([\.0-9]+)(?: (.+))?$
								index = line.find(' ', 9);
								if (index > 9 && index < line.length() - 1)
								{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
									setauthor(line.substr(index + 1)->Trim());
								}
								else
								{
									index = line.length();
								}
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
								setversion(line.substr(9, index - 9)->Trim());
							}
						}
						else
						{
							index = line.find('=');
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
							if (index > 0 && index < line.length() - 1 && (filename = line.substr(index + 1)->Trim())->Length > 0)
							{
								try
								{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
									pairs.insert(make_pair(Convert::ToUInt32(line.substr(0, index)->Trim(), 16), filename));
								}
								catch (std::exception &e)
								{
									Debug::WriteLine("HASHLIST: Failed to parse line \"{0}\"\n{1} {2}", line, e.GetType(), e.what());
								}
							}
						}
					}
				}
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (rd != 0)
					rd.Dispose();
			}
		}

		else
		{
//ORIGINAL LINE: byte[] Hashlistencrypted = File.ReadAllBytes(filename);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
			unsigned char *Hashlistencrypted = File::ReadAllBytes(filename);
			PatchManager::GYXor(Hashlistencrypted);
			std::string Hashtemp2;
			Hashtemp2 = Path::GetTempFileName();
			File::WriteAllBytes(Hashtemp2, Hashlistencrypted);
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//			using (var rd = new StreamReader(File.Open(Hashtemp2, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.UTF8, false, 1024))
			StreamReader *rd = new StreamReader(File->Open(Hashtemp2, FileMode::Open, FileAccess::Read, FileShare::Read), Encoding::UTF8, false, 1024);
			try
			{
				std::string line;
				int index;
				while ((line = rd->ReadLine()) != 0)
				{
					if (line.length() != 0)
					{
						if (line[0] == '#')
						{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'StartsWith' method:
							if (line.StartsWith("#Version ") && line.length() > 9)
							{
								//^#Version ([\.0-9]+)(?: (.+))?$
								index = line.find(' ', 9);
								if (index > 9 && index < line.length() - 1)
								{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
									setauthor(line.substr(index + 1)->Trim());
								}
								else
								{
									index = line.length();
								}
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
								setversion(line.substr(9, index - 9)->Trim());
							}
						}
						else
						{
							index = line.find('=');
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
							if (index > 0 && index < line.length() - 1 && (filename = line.substr(index + 1)->Trim())->Length > 0)
							{
								try
								{
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Trim' method:
									pairs.insert(make_pair(Convert::ToUInt32(line.substr(0, index)->Trim(), 16), filename));
								}
								catch (std::exception &e)
								{
									Debug::WriteLine("HASHLIST: Failed to parse line \"{0}\"\n{1} {2}", line, e.GetType(), e.what());
								}
							}
						}
					}
				}
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (rd != 0)
					rd.Dispose();
			}



			if (printInfo)
				if (File::Exists(Hashtemp2))
				{

				{
						Console->ForegroundColor = ConsoleColor::Red;
						std::cout << "Loaded HASHLIST file version " << (getversion().length() > 0 ? getversion() : "?") << ", created by " << (getauthor().length() > 0 ? "" + getauthor() + "" : "") << " with " << pairs.size() << std::endl;
						Console::ResetColor();
					}
				}

		}
	}

	std::string HashPairs::NameFromHash(unsigned int hash)
	{
		if (pairs.empty())
		{
			loadHashPairs();
		}
		std::string ret;
		if (!pairs.TryGetValue(hash, ret) || ret.length() < 3)
		{
			ret = std::string::Format("@noname/{0:X8}.bin", hash);
		}
		return ret;
	}

	std::string Extensions::FileName(IDXFile::IDXEntry *entry)
	{
		return HashPairs::NameFromHash(entry->Hash);
	}
}
