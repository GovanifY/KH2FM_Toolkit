#pragma once

#include "Properties/Resources.Designer.h"
#include "PatchManager.h"
#include "IDX+IMG.h"
#include <string>
#include <map>
#include <iostream>
#include <stdexcept>

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
	class HashPairs
	{
	public:
		static std::map<unsigned int, std::string> pairs;
	private:
		static std::string privateversion;
	public:
		const static std::string &getversion() const;
		static void setversion(const std::string &value);
	private:
		static std::string privateauthor;
	public:
		const static std::string &getauthor() const;
		static void setauthor(const std::string &value);

		static void loadHashPairs(const std::string &filename = "HashList.bin", bool forceReload = false, bool printInfo = false);

		static std::string NameFromHash(unsigned int hash);
	};

	class Extensions
	{
	public:
//C# TO C++ CONVERTER TODO TASK: Extension methods are not available in C++:
//ORIGINAL LINE: public static string FileName(this IDXFile.IDXEntry entry)
		static std::string FileName(IDXFile::IDXEntry *entry);
	};
}
