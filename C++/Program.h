#pragma once

#include "PatchManager.h"
#include "IDX+IMG.h"
#include "ISO.h"
#include "HashList.h"
#include "Utility.h"
#include "Properties/Resources.Designer.h"
#include <string>
#include <vector>
#include <iostream>
#include <iomanip>
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
using namespace GovanifY::Utility;
using namespace HashList;
using namespace IDX_Tools;
using namespace ISO_Tools;
using namespace KH2FM_Toolkit::Properties;
using namespace Utility;

namespace KH2FM_Toolkit
{
	class Program
	{
		/// <summary>
		///     <para>Sector size of the ISO</para>
		///     <para>Almost always 2048 bytes</para>
		/// </summary>
	public:
		static const int sectorSize = 2048;

		static FileVersionInfo *const program;

	private:
		static PatchManager *const patches;
		static bool advanced;
		static DateTime privatebuilddate;
		const static DateTime &getbuilddate() const;
		static void setbuilddate(const DateTime &value);

		static DateTime RetrieveLinkerTimestamp();

	public:
//ORIGINAL LINE: public static void WriteWarning(string format, params object[] arg)
//C# TO C++ CONVERTER TODO TASK: Use 'va_start', 'va_arg', and 'va_end' to access the parameter array within this method:
		static void WriteWarning(const std::string &format, ...);

//ORIGINAL LINE: public static void WriteError(string format, params object[] arg)
//C# TO C++ CONVERTER TODO TASK: Use 'va_start', 'va_arg', and 'va_end' to access the parameter array within this method:
		static void WriteError(const std::string &format, ...);

		/// <param name="idx">Left open.</param>
		/// <param name="img">Left open.</param>
		/// <param name="recurse">recursive</param>
		/// <param name="tfolder">Complete name</param>
		/// <param name="name">Complete name</param>
	private:
		static void ExtractIDX(IDXFile *idx, Stream *img, bool recurse = false, const std::string &tfolder = "export/", const std::string &name = "");

		static void ExtractISO(Stream *isofile, const std::string &tfolder = "export/");

		/// <param name="sidx">Left open.</param>
		/// <param name="simg">Left open.</param>
		/// <param name="timg">Left open.</param>
		static MemoryStream *PatchIDXInternal(Stream *sidx, Stream *simg, Stream *timg, long long imgOffset, unsigned int parenthash = 0);

		/// <param name="idx">Closed internally.</param>
		/// <param name="img">Closed internally.</param>
		static MemoryStream *PatchIDX(Stream *idx, Stream *img, FileDescriptor *imgd, ISOCopyWriter *niso, bool IsOVL = false);

		static void PatchISO(Stream *isofile, Stream *nisofile);

		/// <summary>The main entry point for the application.</summary>
		static void Main(std::string& args[]);
	};
}
