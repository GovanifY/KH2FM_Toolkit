#pragma once

#include "GY_Substream.h"
#include "HashList.h"
#include "Program.h"
#include <string>
#include <map>
#include <vector>
#include <algorithm>
#include <iostream>
#include <stdexcept>

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
	class PatchManager
	{
	public:
		class Patch
		{
		public:
			bool Compressed;
			unsigned int CompressedSize;
			unsigned int Hash;
			bool IsNew;
			unsigned int Parent;
			unsigned int Relink;
			Substream *Stream;
			unsigned int UncompressedSize;

			const bool &getIsInKH2() const;

			const bool &getIsInOVL() const;

			const bool &getIsinISO() const;

			const bool &getIsInKH2Sub() const;

			const bool &getIsRelink() const;

			~Patch();
		};
	private:
		const std::vector<Stream*> patchms;

		/// <summary>Mapping of Parent IDX -> new children hashes</summary>
	public:
		std::map<unsigned int, std::vector<unsigned int>*> newfiles;

		/// <summary>Mapping of hash->Patch</summary>
		std::map<unsigned int, Patch*> patches;

		PatchManager();

	private:
		bool privateISOChanged;
	public:
		const bool &getISOChanged() const;
		void setISOChanged(const bool &value);
	private:
		bool privateOVLChanged;
	public:
		const bool &getOVLChanged() const;
		void setOVLChanged(const bool &value);
	private:
		bool privateKH2Changed;
	public:
		const bool &getKH2Changed() const;
		void setKH2Changed(const bool &value);

		~PatchManager() : patchms(new List<Stream>());

		static void XeeyXor(unsigned char buffer[]);

		static void GYXor(unsigned char buffer[]);

		static unsigned int ToHash(const std::string &name);

		static unsigned short ToHashAlt(const std::string &name);

		void AddToNewFiles(Patch *nPatch);

	private:
//C# TO C++ CONVERTER NOTE: The following .NET attribute has no direct equivalent in native C++:
//[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		void AddPatch(Stream *ms, const std::string &patchname = "");

	public:
		void AddPatch(const std::string &patchname);


	private:
		void InitializeInstanceFields();
	};
}
