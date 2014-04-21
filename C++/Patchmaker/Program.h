#pragma once

#include "KH2Compress.h"
#include "PatchManager.h"
#include "HashList.h"
#include <string>
#include <vector>
#include <iostream>
#include <iomanip>
#include <cmath>
#include <stdexcept>

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
	class PatchFile
	{
	public:
		class FileEntry
		{
			/// <summary>
			///     <para>File data, uncompressed</para>
			///     <para>NULL if relinking</para>
			/// </summary>
		public:
			Stream *Data;

			/// <summary>Target file hash</summary>
			unsigned int Hash;

			bool IsCompressed;

			/// <summary>
			///     <para>Custom field</para>
			///     <para>Specified whether the file should be ADDED to the IDX if it's missing</para>
			/// </summary>
			bool IsNewFile;

			/// <summary>Parent IDX Hash</summary>
			unsigned int ParentHash;

			/// <summary>Relink to this file</summary>
			unsigned int Relink;

			/// <summary>Filename, used in UI</summary>
			std::string name;

			~FileEntry();

		private:
			void InitializeInstanceFields();
		};
	public:
		static const unsigned int Signature = 0x5032484B;
		static const unsigned int Signaturec = 0x4332484B;
	private:
		const std::vector<unsigned char[]> Changelogs;
	public:
		std::vector<unsigned char[]> Credits;
		std::vector<FileEntry*> Files;
		unsigned int Version;
	private:
//ORIGINAL LINE: private byte[] _Author = {0};
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
		unsigned char *_Author;
//ORIGINAL LINE: private byte[] _OtherInfo = {0};
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
		unsigned char *_OtherInfo;
	public:
		bool convertLinebreaks;

		const std::string &getAuthor() const;
		void setAuthor(const std::string &value);

		const std::string &getOtherInfo() const;
		void setOtherInfo(const std::string &value);

		void AddChange(const std::string &s);

		void AddCredit(const std::string &s);

		void WriteDecrypted(Stream *stream);

		void Write(Stream *stream);


	private:
		void InitializeInstanceFields();

public:
		PatchFile() : Changelogs(new List<byte[]>())
		{
			InitializeInstanceFields();
		}
	};

	class Program
	{
		//Define a bool who's define if the Xeeynamo's encryption is used. She's false until the command -xeey is used
	public:
		static bool DoXeey;
		static bool NewFormat;
		static bool Compression;
		static bool hvs;

	private:
		static DateTime RetrieveLinkerTimestamp();

		static unsigned int GetFileAsInput(std::string &name, bool &blank);

		static unsigned int GetFileHashAsInput(std::string &name);

	public:
		static bool GetYesNoInput();

		static void Mainp(std::string& args[]);
	};
}
