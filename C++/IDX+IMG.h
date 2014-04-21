#pragma once

#include "GY_Substream.h"
#include "KH2Compress.h"
#include "Program.h"
#include <vector>
#include <iostream>
#include <cmath>
#include <stdexcept>

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections::Generic;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics::CodeAnalysis;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
using namespace GovanifY::Utility;
using namespace KH2FM_Toolkit;
using namespace KHCompress;

namespace IDX_Tools
{
	class IDXFile : public IDisposable, IEnumerable<IDXFile::IDXEntry*>
	{
	public:
		class IDXEntry
		{
			/// <summary>
			///     <para>Compressed length of data, in sectors</para>
			///     <para>Bit 0x4000 flags if compressed</para>
			/// </summary>
		public:
			unsigned short Compression;

			/// <summary>Location of data in IMG (LBA)</summary>
			unsigned int DataLBA;

			/// <summary>Data length (size of file)</summary>
			unsigned int DataLength;

			/// <summary>
			///     <para>Identifier of this file</para>
			///     <para>Hashed filename</para>
			/// </summary>
			unsigned int Hash;

			/// <summary>
			///     <para>Secondary identifier of this file</para>
			///     <para>Hashed filename</para>
			/// </summary>
			unsigned short HashAlt;

			/// <summary>Location of data in IMG (bytes)</summary>
			const long long &getOffset() const;

			/// <summary>Returns true if this file is compressed</summary>
			const bool &getIsCompressed() const;
			void setIsCompressed(const bool &value);

			/// <summary>Returns true if both hashes are checked</summary>
			const bool &getIsDualHash() const;
			void setIsDualHash(const bool &value);

			/// <summary>Calculated the compressed data size, in bytes</summary>
			const unsigned int &getCompressedDataLength() const;
			void setCompressedDataLength(const unsigned int &value);

		private:
			void InitializeInstanceFields();

public:
			IDXEntry()
			{
				InitializeInstanceFields();
			}
		};
	private:
		const bool leaveOpen;
		BinaryReader *file;

	protected:
		IDXFile();

	public:
		IDXFile(Stream *input, bool newidx = false, bool leaveOpen = false);

	private:
		unsigned int privateCount;
	public:
		const unsigned int &getCount() const;
		void setCount(const unsigned int &value);
	private:
		unsigned int privatePosition;
	public:
		const unsigned int &getPosition() const;
		void setPosition(const unsigned int &value);

		~IDXFile();

		IEnumerator<IDXEntry*> *GetEnumerator();

	private:
		IEnumerator *IEnumerable_GetEnumerator();

	public:
		IDXEntry *ReadEntry(long long index = -1);

	private:
		void WriteEntry(IDXEntry *entry, unsigned int count = 0);

		void FindEntryByHash(unsigned int hash);

		/// <summary>Relink hash to target</summary>
		/// <param name="hash"></param>
		/// <param name="target"></param>
	public:
		void RelinkEntry(unsigned int hash, unsigned int target);

		void ModifyEntry(IDXEntry *entry);

		void AddEntry(IDXEntry *entry);

	};

	class IDXFileWriter
	{
	private:
		const std::vector<IDXFile::IDXEntry*> entries;

	public:
		void AddEntry(IDXFile::IDXEntry *entry);

		void RelinkEntry(unsigned int hash, unsigned int target);

//C# TO C++ CONVERTER NOTE: The following .NET attribute has no direct equivalent in native C++:
//[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		MemoryStream *GetStream();

public:
		IDXFileWriter() : entries(new List<IDXFile.IDXEntry>())
		{
		}
	};

	class IMGFile
	{
	private:
		static const unsigned char copyBuffer[2048*2];
		const bool leaveOpen;
		const long long offset;
	public:
		Stream *file;

//C# TO C++ CONVERTER WARNING: Unlike C#, there is no automatic call to this finalizer method in native C++:
		private:
		void Finalize()
		{
			this->Dispose(false);
		}

		IMGFile(Stream *file, long long offset = 0, bool leaveOpen = false);

		~IMGFile();

	private:
		void Dispose(bool disposing);

		void ReadFileBuffer(Stream *destination, long long origin, unsigned int length);

		/// <summary>Ensure position is at a 2048 boundary</summary>
		void EnsureBoundary();

	public:
		Substream *GetFileStream(IDXFile::IDXEntry *entry);

		void Seek(unsigned int sector);

		void ReadFile(IDXFile::IDXEntry *entry, Stream *target, bool AdSize);

		void WriteFile(Stream *data);

		void AppendFile(IDXFile::IDXEntry *entry, Stream *data);
	};

	class IDXIMGWriter
	{
	private:
		IDXFileWriter *idx;

//C# TO C++ CONVERTER WARNING: Unlike C#, there is no automatic call to this finalizer method in native C++:
		private:
		void Finalize()
		{
			this->Dispose(false);
		}

	public:
		IDXIMGWriter(Stream *img, long long imgoffset = 0, bool leaveOpen = false);

	private:
		IMGFile *privateimg;
	public:
		const IMGFile &getimg() const;
		void setimg(const IMGFile &value);

		~IDXIMGWriter();

	private:
		void Dispose(bool disposing);

	public:
		void AddFile(IDXFile::IDXEntry *file, Stream *data);

		void RelinkFile(unsigned int hash, unsigned int target);

		MemoryStream *GetCurrentIDX();

	private:
		void InitializeInstanceFields();
	};
}
