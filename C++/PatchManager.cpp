#include "PatchManager.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections::Generic;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics::CodeAnalysis;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Text;
using namespace GovanifY::Utility;
using namespace HashList;

namespace KH2FM_Toolkit
{

	const bool &PatchManager::Patch::getIsInKH2() const
	{
		return Parent == 0;
	}

	const bool &PatchManager::Patch::getIsInOVL() const
	{
		return Parent == 1;
	}

	const bool &PatchManager::Patch::getIsinISO() const
	{
		return Parent == 2;
	}

	const bool &PatchManager::Patch::getIsInKH2Sub() const
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

	const bool &PatchManager::Patch::getIsRelink() const
	{
		return Relink != 0;
	}

	PatchManager::Patch::~Patch()
	{
		if (Stream != 0)
		{
			delete Stream;
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete Stream;
		}
	}

	PatchManager::PatchManager() : patchms(new List<Stream>())
	{
		InitializeInstanceFields();
		setKH2Changed(false);
	setOVLChanged(getKH2Changed());
		setISOChanged(getOVLChanged());
	}

	const bool &PatchManager::getISOChanged() const
	{
		return privateISOChanged;
	}

	void PatchManager::setISOChanged(const bool &value)
	{
		privateISOChanged = value;
	}

	const bool &PatchManager::getOVLChanged() const
	{
		return privateOVLChanged;
	}

	void PatchManager::setOVLChanged(const bool &value)
	{
		privateOVLChanged = value;
	}

	const bool &PatchManager::getKH2Changed() const
	{
		return privateKH2Changed;
	}

	void PatchManager::setKH2Changed(const bool &value)
	{
		privateKH2Changed = value;
	}

	PatchManager::~PatchManager() : patchms(new List<Stream>())
	{
		InitializeInstanceFields();
//C# TO C++ CONVERTER TODO TASK: There is no equivalent to implicit typing in C++ unless the C++0x inferred typing option is selected:
		for (std::map<unsigned int, Patch*>::const_iterator patch = patches.begin(); patch != patches.end(); ++patch)
		{
			delete (*patch)->Value;
		}
		patches.clear();
		for (std::vector<Stream*>::const_iterator ms = patchms.begin(); ms != patchms.end(); ++ms)
		{
			delete ms;
		}
		patchms.clear();
	}

	void PatchManager::XeeyXor(unsigned char buffer[])
	{
		unsigned char v84[8] = {0x58, 0x0c, 0xdd, 0x59, 0xf7, 0x24, 0x7f, 0x4f};
		int i = -1, l = sizeof(buffer) / sizeof(buffer[0]);
		while (l > 0)
		{
			buffer[++i] ^= v84[(--l & 7)];
		}
	}

	void PatchManager::GYXor(unsigned char buffer[])
	{
		unsigned char v84[8] = {0x47, 0x59, 0x4b, 0x35, 0x9a, 0x7f, 0x0e, 0x2a};
		int i = -1, l = sizeof(buffer) / sizeof(buffer[0]);
		while (l > 0)
		{
			buffer[++i] ^= v84[(--l & 7)];
		}
	}

	unsigned int PatchManager::ToHash(const std::string &name)
	{
		unsigned int v0 = unsigned int::MaxValue;
		for (std::string::const_iterator c = name.begin(); c != name.end(); ++c)
		{
			v0 ^= (static_cast<unsigned int>(*c) << 24);
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

	unsigned short PatchManager::ToHashAlt(const std::string &name)
	{
		unsigned short s1 = unsigned short::MaxValue;
		for (int j = name.length(); --j >= 0;)
		{
			s1 ^= static_cast<unsigned short>(name[j] << 8);
			for (int i = 9; --i > 0;)
			{
				if ((s1 & 0x8000u) != 0)
				{
					s1 = static_cast<unsigned short>((s1 << 1) ^ 0x1021u);
				}
				else
				{
					s1 <<= 1;
				}
			}
		}
		return static_cast<unsigned short>(~s1);
	}

	void PatchManager::AddToNewFiles(Patch *nPatch)
	{
		nPatch->IsNew = true;
		if (!newfiles.find(nPatch->Parent) != newfiles.end())
		{
			newfiles.insert(make_pair(nPatch->Parent, std::vector<unsigned int>(1)));
		}
		if (!std::find(newfiles[nPatch->Parent].begin(), newfiles[nPatch->Parent].end(), nPatch->Hash) != newfiles[nPatch->Parent].end())
		{
			newfiles[nPatch->Parent].push_back(nPatch->Hash);
		}
	}

	void PatchManager::AddPatch(Stream *ms, const std::string &patchname = "")
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var br = new BinaryStream(ms, Encoding.ASCII, leaveOpen: true))
		BinaryStream *br = new BinaryStream(ms, Encoding::ASCII, leaveOpen: true);
		try
		{
			if (br->ReadUInt32() != 0x5032484b)
			{
				br->Close();
				ms->Close();
				throw new InvalidDataException("Invalid KH2Patch file!");
			}
			patchms.push_back(ms);
			unsigned int oaAuther = br->ReadUInt32(), obFileCount = br->ReadUInt32(), num = br->ReadUInt32();
			patchname = Path::GetFileName(patchname);
			try
			{
				std::string author = br->ReadCString();
				Console->ForegroundColor = ConsoleColor::Cyan;
				std::cout << "Loading patch " << patchname << " version " << num << " by " << author << std::endl;
				Console::ResetColor();
				br->Seek(oaAuther, SeekOrigin::Begin);
				unsigned int os1 = br->ReadUInt32(), os2 = br->ReadUInt32(), os3 = br->ReadUInt32();
				br->Seek(oaAuther + os1, SeekOrigin::Begin);
				num = br->ReadUInt32();
				if (num > 0)
				{
					br->Seek(num*4, SeekOrigin::Current);
					std::cout << "Changelog:" << std::endl;
					Console->ForegroundColor = ConsoleColor::Green;
					while (num > 0)
					{
						--num;
						std::cout << " * " << br->ReadCString() << std::endl;
					}
				}
				br->Seek(oaAuther + os2, SeekOrigin::Begin);
				num = br->ReadUInt32();
				if (num > 0)
				{
					br->Seek(num*4, SeekOrigin::Current);
					Console::ResetColor();
					std::cout << "Credits:" << std::endl;
					Console->ForegroundColor = ConsoleColor::Green;
					while (num > 0)
					{
						--num;
						std::cout << " * " << br->ReadCString() << std::endl;
					}
					Console::ResetColor();
				}
				br->Seek(oaAuther + os3, SeekOrigin::Begin);
				author = br->ReadCString();
				if (author.length() != 0)
				{
					std::cout << "Other information:\r\n" << std::endl;
					Console->ForegroundColor = ConsoleColor::Green;
					std::cout << author << std::endl;
				}
				Console::ResetColor();
			}
			catch (std::exception &e)
			{
				Console->ForegroundColor = ConsoleColor::Red;
				std::cout << "Error reading patch header: " << e.GetType() << ": " << e.what() << std::endl;
				Console::ResetColor();
			}
			std::cout << "" << std::endl;
			br->Seek(obFileCount, SeekOrigin::Begin);
			num = br->ReadUInt32();
			while (num > 0)
			{
				--num;
				Patch *nPatch = new Patch();
				nPatch->Hash = br->ReadUInt32();
				oaAuther = br->ReadUInt32();
				nPatch->CompressedSize = br->ReadUInt32();
				nPatch->UncompressedSize = br->ReadUInt32();
				nPatch->Parent = br->ReadUInt32();
				nPatch->Relink = br->ReadUInt32();
				nPatch->Compressed = br->ReadUInt32() != 0;
				nPatch->IsNew = br->ReadUInt32() == 1; //Custom
				if (!nPatch->getIsRelink())
				{
					if (nPatch->CompressedSize != 0)
					{
						nPatch->Stream = new Substream(ms, oaAuther, nPatch->CompressedSize);
					}
					else
					{
						throw new InvalidDataException("File length is 0, but not relinking.");
					}
				}
				// Use the last file patch
				if (patches.find(nPatch->Hash) != patches.end())
				{
					Console->ForegroundColor = ConsoleColor::Red;
					std::cout << "The file " << HashPairs::NameFromHash(nPatch->Hash) << " has been included multiple times. Using the one from " << patchname << std::endl;
					patches[nPatch->Hash]->Dispose();
					patches.erase(nPatch->Hash);
					Console::ResetColor();
				}
				patches.insert(make_pair(nPatch->Hash, nPatch));
				//Global checks
				if (!getKH2Changed() && nPatch->getIsInKH2() || nPatch->getIsInKH2Sub())
				{
					setKH2Changed(true);
				}
				else if (!getOVLChanged() && nPatch->getIsInOVL())
				{
					setOVLChanged(true);
				}
				else if (!getISOChanged() && nPatch->getIsinISO())
				{
					setISOChanged(true);
				}
				if (nPatch->IsNew)
				{
					AddToNewFiles(nPatch);
				}
				br->Seek(60, SeekOrigin::Current);
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (br != 0)
				br.Dispose();
		}
	}

	void PatchManager::AddPatch(const std::string &patchname)
	{
		FileStream *fs = 0;
		try
		{
			fs = new FileStream(patchname, FileMode::Open, FileAccess::Read, FileShare::Read);
			if (fs->ReadByte() == 0x4B && fs->ReadByte() == 0x48 && fs->ReadByte() == 0x32 && fs->ReadByte() == 0x50)
			{
				fs->Position = 0;
				AddPatch(fs, patchname);
				return;
			}
			if (fs->Length > int::MaxValue)
			{
				throw new OutOfMemoryException("File too large");
			}

			try
			{
				fs->Position = 0;
				unsigned char buffer[fs->Length];
				fs->Read(buffer, 0, static_cast<int>(fs->Length));
				GYXor(buffer);
				AddPatch(new MemoryStream(buffer), patchname);
			}

			catch (std::exception &e1)
			{
				fs->Position = 0;
				unsigned char buffer[fs->Length];
				fs->Read(buffer, 0, static_cast<int>(fs->Length));
				XeeyXor(buffer);
				AddPatch(new MemoryStream(buffer), patchname);
				Program::WriteWarning("Old format is used, Please use the new one!");
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				delete fs;
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
				delete fs;
			}
		}
		catch (std::exception &e)
		{
			if (fs != 0)
			{
				delete fs;
			}
			std::cout << "Failed to parse patch: " << e.what() << std::endl;
		}
	}

	void PatchManager::InitializeInstanceFields()
	{
		newfiles = std::map<unsigned int, std::vector<unsigned int>*>();
		patches = std::map<unsigned int, Patch*>();
	}
}
