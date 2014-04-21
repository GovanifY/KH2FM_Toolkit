#include "IDX+IMG.h"

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

	const long long &IDXFile::IDXEntry::getOffset() const
	{
		return DataLBA*2048;
	}

	const bool &IDXFile::IDXEntry::getIsCompressed() const
	{
		return (Compression & 0x4000u) == 0x4000u;
	}

	void IDXFile::IDXEntry::setIsCompressed(const bool &value)
	{
		Compression = static_cast<unsigned short>((value ? 0x4000u : 0) | (Compression & 0xBFFFu));
	}

	const bool &IDXFile::IDXEntry::getIsDualHash() const
	{
		return (Compression & 0x8000u) == 0x8000u;
	}

	void IDXFile::IDXEntry::setIsDualHash(const bool &value)
	{
		Compression = static_cast<unsigned short>((value ? 0x8000u : 0) | (Compression & 0x7FFFu));
	}

	const unsigned int &IDXFile::IDXEntry::getCompressedDataLength() const
	{
		// KH2 does get this value by and-ing with 0x3FFF, so that's confirmed
		unsigned int size = (Compression & 0x3FFFu) + 1u;
		//Fixes file 0x10303F6F:
		//  Real compressed size = 12,093,440 bytes (Verified manually)
		//  Compression = 0x4710
		//  (0x4710 & 0x3FFF) + 1 = 0x711 * 2048 = 3,704,832 + 0x800000 = 12,093,440 bytes
		//What is happening is the size is getting truncated (see the set function).
		//0x10303F6F is the only compressed file that hits this limitation officially, and uncompressed files can bypass it (use DataLength).
		//Also note this increases *all* files about the specified size
		if (Hash == 0x10303F6F && getIsCompressed() && DataLength > 0xC00000)
		{
			size += 0x1000u;
		}
		return size*2048u;
	}

	void IDXFile::IDXEntry::setCompressedDataLength(const unsigned int &value)
	{
		if (value == 0)
		{
			Compression &= static_cast<unsigned short>(0xC000)u;
			return;
		}
		unsigned int size = static_cast<unsigned int>(ceil(static_cast<double>(value) / 2048)) - 1;
		//Seems that the "size" component just truncated if it's too large, as seen by the larger files (videos, Title.vas)
		/* Size component:
		 * * Includes at minimum 0x0FFF
		 * * Does NOT include 0x1000 (obj/B_LK120_RAW.mset; vagstream/Title.vas; zmovie/fm/me3.pss; zmovie/fm/opn.pss)
		 * * Does include 0x2000 (zmovie/fm/me3.pss; zmovie/fm/opn.pss)
		 * * 0x4000 is the compression flag
		 * * 0x8000 is the alt hash flag
		 * Nothing has broken these rules, that I can find
		*/
		Compression = static_cast<unsigned short>((Compression & 0xC000u) | (size & 0x2FFFu));
	}

	void IDXFile::IDXEntry::InitializeInstanceFields()
	{
		Compression = 0;
		HashAlt = 0;
	}

	IDXFile::IDXFile()
	{
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
		delete file;
	}

	IDXFile::IDXFile(Stream *input, bool newidx = false, bool leaveOpen = false) : leaveOpen(leaveOpen)
	{
		file = new BinaryReader(input);
		input->Position = 0;
		if (newidx)
		{
			setCount(0);
			input->Write(new unsigned char[] {0, 0, 0, 0}, 0, 4);
		}
		else
		{
			setCount(file->ReadUInt32());
		}
		setPosition(0);
	}

	const unsigned int &IDXFile::getCount() const
	{
		return privateCount;
	}

	void IDXFile::setCount(const unsigned int &value)
	{
		privateCount = value;
	}

	const unsigned int &IDXFile::getPosition() const
	{
		return privatePosition;
	}

	void IDXFile::setPosition(const unsigned int &value)
	{
		privatePosition = value;
	}

	IDXFile::~IDXFile()
	{
		if (file != 0)
		{
			if (!leaveOpen)
			{
				file->Close();
			}
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete file;
		}
	}

	IEnumerator<IDXEntry*> *IDXFile::GetEnumerator()
	{
		for (unsigned int i = 0; i < getCount(); ++i)
		{
//C# TO C++ CONVERTER TODO TASK: C++ does not have an equivalent to the C# 'yield' keyword:
			yield return ReadEntry(i);
		}
	}

	IEnumerator *IDXFile::IEnumerable_GetEnumerator()
	{
		return GetEnumerator();
	}

	IDX_Tools::IDXFile::IDXEntry *IDXFile::ReadEntry(long long index = -1)
	{
		if (index >= 0)
		{
			setPosition(static_cast<unsigned int>(index));
		}
		if (getPosition() >= getCount())
		{
			return new IDXEntry {Hash = 0};
		}
		file->BaseStream->Position = 4 + 16*getPosition();
	setPosition(getPosition() + 1);
		IDXEntry *entry = new IDXEntry {Hash = file->ReadUInt32(), HashAlt = file->ReadUInt16(), Compression = file->ReadUInt16(), DataLBA = file->ReadUInt32(), DataLength = file->ReadUInt32()};
		return entry;
	}

	void IDXFile::WriteEntry(IDXEntry *entry, unsigned int count = 0)
	{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var bw = new BinaryStream(file.BaseStream, leaveOpen: true))
		BinaryStream *bw = new BinaryStream(file->BaseStream, leaveOpen: true);
		try
		{
			bw->Write(entry->Hash);
			bw->Write(entry->HashAlt);
			bw->Write(entry->Compression);
			bw->Write(entry->DataLBA);
			bw->Write(entry->DataLength);
			if (count != 0)
			{
				bw->getBaseStream()->Position = 0;
				bw->Write(count);
			}
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (bw != 0)
				bw.Dispose();
		}
	}

	void IDXFile::FindEntryByHash(unsigned int hash)
	{
		file->BaseStream->Position = 4;
		for (unsigned int i = 0; i < getCount(); ++i)
		{
			if (file->ReadUInt32() == hash)
			{
				file->BaseStream->Position -= 4;
				return;
			}
			file->BaseStream->Position += 12;
		}
		throw new FileNotFoundException();
	}

	void IDXFile::RelinkEntry(unsigned int hash, unsigned int target)
	{
		FindEntryByHash(target);
		file->BaseStream->Position += 4;
		IDXEntry *entry = new IDXEntry {Hash = hash, HashAlt = file->ReadUInt16(), Compression = file->ReadUInt16(), DataLBA = file->ReadUInt32(), DataLength = file->ReadUInt32()};
		FindEntryByHash(hash);
		WriteEntry(entry);
	}

	void IDXFile::ModifyEntry(IDXEntry *entry)
	{
		FindEntryByHash(entry->Hash);
		WriteEntry(entry);
	}

	void IDXFile::AddEntry(IDXEntry *entry)
	{
		file->BaseStream->Position = 4 + 16*getCount();
		setCount(getCount() + 1);
		WriteEntry(entry, getCount());
	}

	void IDXFileWriter::AddEntry(IDXFile::IDXEntry *entry)
	{
		entries.push_back(entry);
	}

	void IDXFileWriter::RelinkEntry(unsigned int hash, unsigned int target)
	{
//C# TO C++ CONVERTER TODO TASK: Lambda expressions and anonymous methods are not converted to native C++ unless the option to convert to C++0x lambdas is selected:
		IDXFile::IDXEntry *t = entries.Find(e => e->Hash == target);
		if (t->Hash == 0)
		{
			throw new FileNotFoundException();
		}
		entries.push_back(new IDXFile::IDXEntry {Hash = hash, HashAlt = 0, Compression = t->Compression, DataLBA = t->DataLBA, DataLength = t->DataLength});
	}

	MemoryStream *IDXFileWriter::GetStream()
	{
//C# TO C++ CONVERTER TODO TASK: Lambda expressions and anonymous methods are not converted to native C++ unless the option to convert to C++0x lambdas is selected:
		entries.Sort((a, b) => a::Hash < b::Hash ? - 1 : (a::Hash > b::Hash ? 1 : 0));
		MemoryStream *ms = new MemoryStream();
		try
		{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//			using (var bw = new BinaryStream(ms, leaveOpen: true))
			BinaryStream *bw = new BinaryStream(ms, leaveOpen: true);
			try
			{
				bw->Write(entries.size());
				for (std::vector<IDXFile::IDXEntry*>::const_iterator entry = entries.begin(); entry != entries.end(); ++entry)
				{
					bw->Write((*entry)->Hash);
					bw->Write((*entry)->HashAlt);
					bw->Write((*entry)->Compression);
					bw->Write((*entry)->DataLBA);
					bw->Write((*entry)->DataLength);
				}
			}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
			finally
			{
				if (bw != 0)
					bw.Dispose();
			}
		}
		catch (std::exception &e1)
		{
			ms->Close();
			throw;
		}
		ms->Position = 0;
		return ms;
	}

	IMGFile::IMGFile(Stream *file, long long offset = 0, bool leaveOpen = false) : leaveOpen(leaveOpen), offset(offset)
	{
		this->file = file;
	}

	IMGFile::~IMGFile()
	{
		this->Dispose(true);
	}

	void IMGFile::Dispose(bool disposing)
	{
		if (file != 0)
		{
			if (!leaveOpen)
			{
				delete file;
			}
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete file;
		}
	}

	void IMGFile::ReadFileBuffer(Stream *destination, long long origin, unsigned int length)
	{
		//Not thread safe, but I'm not using threads
		file->Position = offset + origin;
		int num;
		while (length > 0 && (num = file->Read(copyBuffer, 0, static_cast<int>(__min(2048*2, length)))) != 0)
		{
			destination->Write(copyBuffer, 0, num);
			length -= static_cast<unsigned int>(num);
		}
	}

	void IMGFile::EnsureBoundary()
	{
		if (((file->Position - offset) % 2048) != 0)
		{
			int rem = 2048 - static_cast<int>((file->Position - offset) % 2048);
//ORIGINAL LINE: byte[] buf = {0, 0, 0, 0};
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
			unsigned char *buf = {0, 0, 0, 0};
			while (rem > 3)
			{
				file->Write(buf, 0, 4);
				rem -= 4;
			}
			while (--rem >= 0)
			{
				file->WriteByte(0);
			}
		}
		if (((file->Position - offset) % 2048) != 0)
		{
			throw new DataMisalignedException();
		}
	}

	GovanifY::Utility::Substream *IMGFile::GetFileStream(IDXFile::IDXEntry *entry)
	{
		return new Substream(file, offset + entry->DataLBA*2048, entry->getIsCompressed() ? entry->getCompressedDataLength() : entry->DataLength);
	}

	void IMGFile::Seek(unsigned int sector)
	{
		file->Position = offset + sector*2048;
	}

	void IMGFile::ReadFile(IDXFile::IDXEntry *entry, Stream *target, bool AdSize)
	{
		if (entry->getIsCompressed())
		{
			if (entry->getCompressedDataLength() > int::MaxValue)
			{
				throw new NotSupportedException("File to big to decompress");
			}
			unsigned char input[entry->CompressedDataLength];
			Seek(entry->DataLBA);
			file->Read(input, 0, static_cast<int>(entry->getCompressedDataLength()));
			try
			{
//ORIGINAL LINE: byte[] output = KH2Compressor.decompress(input, entry.DataLength);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
				unsigned char *output = KH2Compressor::decompress(input, entry->DataLength);
				target->Write(output, 0, sizeof(output) / sizeof(output[0]));
				if (AdSize)
				{
					std::cout << "Size (unpacked): " << sizeof(output) / sizeof(output[0]) << std::endl;
				}
			}
			catch (std::exception &e)
			{
				Program::WriteError(" ERROR: Failed to decompress: " + e.what());
			}
		}
		else
		{
			ReadFileBuffer(target, entry->getOffset(), entry->DataLength);
		}
	}

	void IMGFile::WriteFile(Stream *data)
	{
		if (data->Length > 0xFFFFFFFF)
		{
			throw new NotSupportedException("data too big to store");
		}
		EnsureBoundary();
		data->CopyTo(file);
		EnsureBoundary();
	}

	void IMGFile::AppendFile(IDXFile::IDXEntry *entry, Stream *data)
	{
		if (data->Length > 0xFFFFFFFF)
		{
			throw new NotSupportedException("data too big to store");
		}
		file->Seek(0, SeekOrigin::End);
		EnsureBoundary();
		entry->DataLBA = static_cast<unsigned int>(file->Position - offset) / 2048;
		data->CopyTo(file);
		EnsureBoundary();
	}

	IDXIMGWriter::IDXIMGWriter(Stream *img, long long imgoffset = 0, bool leaveOpen = false)
	{
		InitializeInstanceFields();
		this->setimg(new IMGFile(img, imgoffset));
		this->setimg(new IMGFile(img, imgoffset, leaveOpen));
	}

	const IMGFile &IDXIMGWriter::getimg() const
	{
		return privateimg;
	}

	void IDXIMGWriter::setimg(const IMGFile &value)
	{
		privateimg = value;
	}

	IDXIMGWriter::~IDXIMGWriter()
	{
		InitializeInstanceFields();
		this->Dispose(true);
	}

	void IDXIMGWriter::Dispose(bool disposing)
	{
		if (getimg() != 0)
		{
			delete getimg();
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete getimg();
		}
		if (disposing && idx != 0)
		{
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete idx;
		}
	}

	void IDXIMGWriter::AddFile(IDXFile::IDXEntry *file, Stream *data)
	{
		getimg()->AppendFile(file, data);
		idx->AddEntry(file);
	}

	void IDXIMGWriter::RelinkFile(unsigned int hash, unsigned int target)
	{
		idx->RelinkEntry(hash, target);
	}

	MemoryStream *IDXIMGWriter::GetCurrentIDX()
	{
		return idx->GetStream();
	}

	void IDXIMGWriter::InitializeInstanceFields()
	{
		idx = new IDXFileWriter();
	}
}
