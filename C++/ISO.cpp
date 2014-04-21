#include "ISO.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Collections::Generic;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Text;
using namespace GovanifY::Utility;

namespace ISO_Tools
{

	unsigned char *Utilities::BEInt32(unsigned int i)
	{
		return new unsigned char[8] {static_cast<unsigned char>(i), static_cast<unsigned char>(i >> 8), static_cast<unsigned char>(i >> 16), static_cast<unsigned char>(i >> 24), static_cast<unsigned char>(i >> 24), static_cast<unsigned char>(i >> 16), static_cast<unsigned char>(i >> 8), static_cast<unsigned char>(i)};
	}

	DateTime Utilities::DateTimeFromVolumeTime(array_Renamed<unsigned char> *array_Renamed, long long offset = 0)
	{
		try
		{
			DateTime date = DateTime((array_Renamed[offset] - '0')*1000 + (array_Renamed[offset + 1] - '0')*100 + (array_Renamed[offset + 2] - '0')*10 + (array_Renamed[offset + 3] - '0'), (array_Renamed[offset + 4] - '0')*10 + (array_Renamed[offset + 5] - '0'), (array_Renamed[offset + 6] - '0')*10 + (array_Renamed[offset + 7] - '0'), (array_Renamed[offset + 8] - '0')*10 + (array_Renamed[offset + 9] - '0'), (array_Renamed[offset + 10] - '0')*10 + (array_Renamed[offset + 11] - '0'), (array_Renamed[offset + 12] - '0')*10 + (array_Renamed[offset + 13] - '0'), (array_Renamed[offset + 14] - '0')*100 + (array_Renamed[offset + 15] - '0')*10, DateTimeKind::Utc);
			return date.AddMinutes(-(15*(char) array_Renamed[offset + 16]));
		}
		catch (ArgumentOutOfRangeException *e1)
		{
			return DateTime::MinValue;
		}
	}

	void Utilities::VolumeTimeFromDateTime(DateTime date, array_Renamed<unsigned char> *array_Renamed, int offset = 0)
	{
		if (date == DateTime::MinValue)
		{
			for (int i = 0; i < 16; ++i)
			{
				array_Renamed[offset + i] = static_cast<unsigned char>('0');
			}
		}
		else
		{
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to 'ToString':
			Buffer::BlockCopy(Encoding::ASCII->GetBytes(date.ToString("yyyyMMddHHmmssff")), 0, array_Renamed, offset, 16);
		}
		array_Renamed[offset + 16] = 0; //UTC
	}

	unsigned char *Utilities::VolumeTimeFromDateTime(DateTime date)
	{
		unsigned char buf[17];
		VolumeTimeFromDateTime(date, buf, 0);
		return buf;
	}

	DateTime Utilities::DateTimeFromDirectoryTime(array_Renamed<unsigned char> *array_Renamed, long long offset = 0)
	{
		try
		{
			DateTime date = DateTime(1900 + array_Renamed[offset], array_Renamed[offset + 1], array_Renamed[offset + 2], array_Renamed[offset + 3], array_Renamed[offset + 4], array_Renamed[offset + 5], DateTimeKind::Utc);
			return date.AddMinutes(-(15*(char) array_Renamed[offset + 6]));
		}
		catch (ArgumentOutOfRangeException *e1)
		{
			return DateTime::MinValue;
		}
	}

	void Utilities::DirectoryTimeFromDateTime(DateTime date, array_Renamed<unsigned char> *array_Renamed, int offset = 0)
	{
		if (date == DateTime::MinValue)
		{
			Array->Clear(array_Renamed, offset, 7);
		}
		else
		{
			array_Renamed[offset] = static_cast<unsigned char>(date.Year - 1900);
			array_Renamed[offset + 1] = static_cast<unsigned char>(date.Month);
			array_Renamed[offset + 2] = static_cast<unsigned char>(date.Day);
			array_Renamed[offset + 3] = static_cast<unsigned char>(date.Hour);
			array_Renamed[offset + 4] = static_cast<unsigned char>(date.Minute);
			array_Renamed[offset + 5] = static_cast<unsigned char>(date.Second);
			array_Renamed[offset + 6] = 0; //UTC
		}
	}

	unsigned char *Utilities::DirectoryTimeFromDateTime(DateTime date)
	{
		unsigned char buf[7];
		DirectoryTimeFromDateTime(date, buf, 0);
		return buf;
	}

	bool Utilities::isAChar(wchar_t c)
	{
		return c >= 'A' && c <= 'Z' || c >= '%' && c <= '9' || c >= ' ' && c <= '"' || c >= ':' && c <= '?' || c == '_';
	}

	bool Utilities::isDChar(wchar_t c)
	{
		return c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_';
	}

	bool Utilities::isAString(wchar_t str[])
	{
		for (wchar_t::const_iterator c = str->begin(); c != str->end(); ++c)
		{
			if (!isAChar(*c))
			{
				return false;
			}
		}
		return true;
	}

	bool Utilities::isDString(wchar_t str[])
	{
		for (wchar_t::const_iterator c = str->begin(); c != str->end(); ++c)
		{
			if (!isDChar(*c))
			{
				return false;
			}
		}
		return true;
	}

	bool Utilities::isFileName(wchar_t str[])
	{
		bool dot = false, semi = false;
		for (int i = 0; i < sizeof(str) / sizeof(str[0]); ++i)
		{
			wchar_t c = str[i];
			if (!isDChar(c))
			{
				//There must be 1 dot, before the semicolon and after the filename
				if (c == '.' && !dot && !semi && i != 0)
				{
					dot = true;
				}
					//There must be 1 semicolon, after the dot and before the version
				else if (c == ';' && !semi && dot && i + 1 != sizeof(str) / sizeof(str[0]))
				{
					semi = true;
				}
				else
				{
					return false;
				}
			}
		}
		return dot && semi;
	}

	std::string Utilities::parseADString(wchar_t str[])
	{
		int len = sizeof(str) / sizeof(str[0]);
		while (--len >= 0 && (str[len] == ' '))
		{
		}
		return std::string(str, 0, len + 1);
	}

	std::string Utilities::parseADFileName(wchar_t str[])
	{
		int len = sizeof(str) / sizeof(str[0]);
		while (--len >= 0 && (str[len] == ' '))
		{
		}
		if (len == 0)
		{
			switch (static_cast<unsigned char>(str[0]))
			{
				case 0:
					return ".";
				case 1:
					return "..";
			}
		}
		return std::string(str, 0, len + 1);
	}

	FileDescriptor::FileDescriptor(FileDescriptor *parent = 0) : children(new List<FileDescriptor>())
	{
		this->setparent(parent);
		Length = 34;
		ExtendedAttributeRecordLength = 0;
		ExtentLBA = 0;
		ExtentLength = 0;
		RecordingDate = DateTime::MinValue;
		FileFlags = None;
		FileUnitSize = 0;
		InterleaveGapSize = 0;
		VolumeSequence = 1;
		_FileIdentifier = "";
		RawOffset = -1;
	}

	FileDescriptor::FileDescriptor(BinaryStream *br, FileDescriptor *parent = 0) : children(new List<FileDescriptor>())
	{
		if (br->getTextEncoding() != Encoding::ASCII)
		{
			br->setTextEncoding(Encoding::ASCII);
		}
		this->setparent(parent);
		RawOffset = br->Tell();
		Length = br->ReadByte();
		if (Length < 34)
		{
			throw new EmptyDescriptorException();
		}
		if (RawOffset + 1 + Length > br->getBaseStream()->Length)
		{
			throw new ArgumentException("Input stream is too small", "br");
		}
		ExtendedAttributeRecordLength = br->ReadByte();
		ExtentLBA = br->ReadUInt32();
		br->Seek(4, SeekOrigin::Current);
		ExtentLength = br->ReadUInt32();
		br->Seek(4, SeekOrigin::Current);
		RecordingDate = Utilities::DateTimeFromDirectoryTime(br->ReadBytes(7));
		FileFlags = static_cast<Flags>(br->ReadByte());
		FileUnitSize = br->ReadByte();
		InterleaveGapSize = br->ReadByte();
		VolumeSequence = br->ReadUInt16();
		br->Seek(2, SeekOrigin::Current);
		int filenamelen = br->ReadByte();
		_FileIdentifier = Utilities::parseADFileName(br->ReadChars(filenamelen));
		br->Seek(Length - (33 + filenamelen), SeekOrigin::Current);
		Debug::WriteLine("----- FileDescriptor Begin -----\r\nExtendedAttributeRecordLength: {0}\r\nExtentLBA: {1}\r\nExtentLength: {2}\r\nRecordingDate: {3:O}\r\nFileFlags: {4}\r\nFileUnitSize: {5}\r\nInterleaveGapSize: {6}\r\nVolumeSequence: {7}\r\nfilenamelen: {8}\r\nFileIdentifier: {9}\r\nLength: {10}\r\nSkip at end: {11}\r\n----- FileDescriptor End -----", ExtendedAttributeRecordLength, ExtentLBA, ExtentLength, RecordingDate, FileFlags, FileUnitSize, InterleaveGapSize, VolumeSequence, filenamelen, getFileIdentifier(), Length, Length - (33 + filenamelen));
	}

	const std::string &FileDescriptor::getFileIdentifier() const
	{
		return _FileIdentifier;
	}

	void FileDescriptor::setFileIdentifier(const std::string &value)
	{
		_FileIdentifier = value;
		_FileName = _FullName = "";
	}

	const std::string &FileDescriptor::getFileName() const
	{
		if (_FileName == "")
		{
			if (getIsDirectory())
			{
				_FileName = _FileIdentifier;
			}
			else
			{
				_FileName = _FileIdentifier.substr(0, _FileIdentifier.find(';'));
			}
		}
		return _FileName;
	}

	const std::string &FileDescriptor::getFullName() const
	{
		if (getparent() == 0)
		{
			return getFileName();
		}
		if (_FullName == "")
		{
			_FullName = getparent()->getFullName() + "/" + getFileName();
		}
		return _FullName;
	}

	const FileDescriptor &FileDescriptor::getparent() const
	{
		return privateparent;
	}

	void FileDescriptor::setparent(const FileDescriptor &value)
	{
		privateparent = value;
	}

	const int &FileDescriptor::getChildrenCount() const
	{
		return children.size();
	}

	const bool &FileDescriptor::getIsDirectory() const
	{
		return FileFlags::HasFlag(Directory);
	}

	const bool &FileDescriptor::getIsVisible() const
	{
		return !FileFlags::HasFlag(Hidden);
	}

	const bool &FileDescriptor::getIsSpecial() const
	{
		return getFileIdentifier() == "." || getFileIdentifier() == "..";
	}

	IEnumerator<FileDescriptor*> *FileDescriptor::GetEnumerator()
	{
		return children.begin();
	}

	IEnumerator *FileDescriptor::IEnumerable_GetEnumerator()
	{
		return GetEnumerator();
	}

	void FileDescriptor::ParseSubFiles(Stream *iso, bool recurse = false)
	{
		if (!getIsDirectory())
		{
			throw new NotSupportedException("Only directories can have children");
		}
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//C# TO C++ CONVERTER TODO TASK: C# to C++ Converter does not resolve named parameters in method calls:
//		using (var br = new BinaryStream(iso, leaveOpen: true))
		BinaryStream *br = new BinaryStream(iso, leaveOpen: true);
		try
		{
			ParseSubFiles(br, recurse, 0);
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (br != 0)
				br.Dispose();
		}
	}

	void FileDescriptor::ParseSubFiles(BinaryStream *iso, bool recurse = false)
	{
		if (!getIsDirectory())
		{
			throw new NotSupportedException("Only directories can have children");
		}
		ParseSubFiles(iso, recurse, 0);
	}

	void FileDescriptor::ParseSubFiles(BinaryStream *br, bool recurse = false, int recurseC = 0)
	{
		children.clear();
		long long remaining = ExtentLength;
		br->Seek(ExtentLBA*ISOFile::sectorSize, SeekOrigin::Begin);
		while (remaining >= 34)
		{
			try
			{
				FileDescriptor *child = new FileDescriptor(br, this);
				remaining -= child->Length;
				// Don't add self or parent
				if (child->getIsSpecial())
				{
					continue;
				}
				children.push_back(child);
				//Max directory depth is 8. Also, prevent recursive loops.
				if (recurse && child->getIsDirectory() && recurseC < 7)
				{
					long long posC = br->Tell();
					child->ParseSubFiles(br, true, recurseC + 1);
					br->Seek(posC, SeekOrigin::Begin);
				}
			}
			catch (EmptyDescriptorException *e1)
			{
				Debug::WriteLine("EmptyDescriptorException At position: " + br->Tell());
				return;
			}
		}
	}

	FileDescriptor *FileDescriptor::GetChild(const std::string &name)
	{
		for (std::vector<FileDescriptor*>::const_iterator child = children.begin(); child != children.end(); ++child)
		{
			if ((*child)->getFileName() == name)
			{
				return child;
			}
		}
		throw new FileNotFoundException("Failed to find file in self");
	}

	void FileDescriptor::AddChild(FileDescriptor *newchild)
	{
		if (newchild->getparent() != 0)
		{
			newchild->getparent()->children->Remove(newchild);
		}
		newchild->setparent(this);
		children.push_back(newchild);
	}

	PrimaryVolumeDescriptor::PrimaryVolumeDescriptor() : VolumeDesciptor(PrimaryVolumeDescriptor, "CD001", 1)
	{
		SystemIdentifier = "PLAYSTATION";
		VolumeIdentifier = "";
		VolumeSpaceSize = 0;
		VolumeSetSize = 1;
		VolumeSequenceNumber = 1;
		LogicalBlockSize = 2048;
		PathTableSize = 0;
		PathLPosition = 0;
		PathLOptionalPosition = 0;
		RootDirectory = new FileDescriptor();
		VolumeSetIdentifier = "";
		PublisherIdentifier = "";
		DataPreparerIdentifier = "";
		ApplicationIdentifier = "PLAYSTATION";
		CopyrightFileIdentifier = "";
		AbstractFileIdentifier = "";
		BibliographicFileIdentifier = "";
		VolumeCreation = DateTime::MinValue;
		VolumeModification = DateTime::MinValue;
		VolumeExpiration = DateTime::MinValue;
		VolumeEffective = DateTime::MinValue;
	}

	PrimaryVolumeDescriptor::PrimaryVolumeDescriptor(unsigned char sector[], Stream *iso = 0) : VolumeDesciptor(sector)
	{
		if (sizeof(sector) / sizeof(sector[0]) < 883)
		{
			throw new ArgumentException("sector is too small", "sector");
		}
		if (VD_Type != PrimaryVolumeDescriptor || VD_Identifier != "CD001" || VD_Version != 1)
		{
			throw new InvalidDataException("Type is not of a Primary Volume Descriptor");
		}
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//		using (var br = new BinaryStream(new MemoryStream(sector, false), Encoding.ASCII))
		BinaryStream *br = new BinaryStream(new MemoryStream(sector, false), Encoding::ASCII);
		try
		{
			br->Seek(8, SeekOrigin::Begin);
			SystemIdentifier = Utilities::parseADString(br->ReadChars(32));
			VolumeIdentifier = Utilities::parseADString(br->ReadChars(32));
			br->Seek(8, SeekOrigin::Current);
			VolumeSpaceSize = br->ReadUInt32();
			br->Seek(4 + 32, SeekOrigin::Current);
			VolumeSetSize = br->ReadUInt16();
			br->Seek(2, SeekOrigin::Current);
			VolumeSequenceNumber = br->ReadUInt16();
			br->Seek(2, SeekOrigin::Current);
			LogicalBlockSize = br->ReadUInt16();
			if (LogicalBlockSize != ISOFile::sectorSize)
			{
				throw new NotSupportedException("Block sizes other than " + ISOFile::sectorSize + " not supported");
			}
			br->Seek(2, SeekOrigin::Current);
			PathTableSize = br->ReadUInt32();
			br->Seek(4, SeekOrigin::Current);
			PathLPosition = br->ReadUInt32();
			PathLOptionalPosition = br->ReadUInt32();
			br->Seek(8, SeekOrigin::Current);
				//Just in-case it has some weird value
				long long pos = br->Tell() + 34;
				RootDirectory = new FileDescriptor(br);
				br->Seek(pos, SeekOrigin::Begin);
		}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
		finally
		{
			if (br != 0)
				br.Dispose();
		}
			VolumeSetIdentifier = Utilities::parseADString(br->ReadChars(128));
			PublisherIdentifier = Utilities::parseADString(br->ReadChars(128));
			DataPreparerIdentifier = Utilities::parseADString(br->ReadChars(128));
			ApplicationIdentifier = Utilities::parseADString(br->ReadChars(128));
			CopyrightFileIdentifier = Utilities::parseADString(br->ReadChars(38));
			AbstractFileIdentifier = Utilities::parseADString(br->ReadChars(36));
			BibliographicFileIdentifier = Utilities::parseADString(br->ReadChars(37));
			VolumeCreation = Utilities::DateTimeFromVolumeTime(sector, br->Tell());
			br->Seek(17, SeekOrigin::Current);
			VolumeModification = Utilities::DateTimeFromVolumeTime(sector, br->Tell());
			br->Seek(17, SeekOrigin::Current);
			VolumeExpiration = Utilities::DateTimeFromVolumeTime(sector, br->Tell());
			br->Seek(17, SeekOrigin::Current);
			VolumeEffective = Utilities::DateTimeFromVolumeTime(sector, br->Tell());
			Debug::WriteLine("----- PrimaryVolumeDescriptor Begin -----\r\nSystemIdentifier: {0}\r\nVolumeIdentifier: {1}\r\nVolumeSpaceSize: {2}\r\nVolumeSetSize: {3}\r\nVolumeSequenceNumber: {4}\r\nLogicalBlockSize: {5}\r\nPathTableSize: {6}\r\nPathLPosition: {7}\r\nPathLOptionalPosition: {8}\r\nVolumeSetIdentifier: {9}\r\nPublisherIdentifier: {10}\r\nDataPreparerIdentifier: {11}\r\nApplicationIdentifier: {12}\r\nCopyrightFileIdentifier: {13}\r\nAbstractFileIdentifier: {14}\r\nBibliographicFileIdentifier: {15}\r\nVolumeCreation: {16:O}\r\nVolumeModification: {17:O}\r\nVolumeExpiration: {18:O}\r\nVolumeEffective: {19:O}\r\n----- PrimaryVolumeDescriptor End -----", SystemIdentifier, VolumeIdentifier, VolumeSpaceSize, VolumeSetSize, VolumeSequenceNumber, LogicalBlockSize, PathTableSize, PathLPosition, PathLOptionalPosition, VolumeSetIdentifier, PublisherIdentifier, DataPreparerIdentifier, ApplicationIdentifier, CopyrightFileIdentifier, AbstractFileIdentifier, BibliographicFileIdentifier, VolumeCreation, VolumeModification, VolumeExpiration, VolumeEffective);
			if (iso != 0)
			{
				RootDirectory->ParseSubFiles(iso, true);
			}
	}

	IEnumerator<FileDescriptor*> *<missing_class_definition>::GetEnumerator()
	{
		return GetEnumerator(RootDirectory);
	}

	IEnumerator *<missing_class_definition>::GetEnumerator()
	{
		return GetEnumerator(RootDirectory);
	}

	FileDescriptor *<missing_class_definition>::FindFile(const std::string &path)
	{
//ORIGINAL LINE: string[] parts = path.ToUpperInvariant().Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
//C# TO C++ CONVERTER TODO TASK: There is no direct native C++ equivalent to the .NET String 'Split' method:
		std::string *parts = path.ToUpperInvariant()->Split(new wchar_t[] {'/', '\\'}, StringSplitOptions::RemoveEmptyEntries);
		FileDescriptor *current = RootDirectory;
		for (int i = 0; i < sizeof(parts) / sizeof(parts[0]); ++i)
		{
			if (parts[i] == ".")
			{
				continue;
			}
			if (parts[i] == "..")
			{
				current = current->getparent();
			}
			else
			{
				current = current->GetChild(parts[i]);
			}
			if (!current->getIsDirectory() && i + 1 != sizeof(parts) / sizeof(parts[0]))
			{
				throw new FileNotFoundException("File is not a directory");
			}
		}
		if (current->getIsDirectory())
		{
			throw new FileNotFoundException("Target file is a directory");
		}
		return current;
	}

	IEnumerator<FileDescriptor*> *<missing_class_definition>::GetEnumerator(FileDescriptor *parent)
	{
		for (ISO_Tools::FileDescriptor::const_iterator file = parent->begin(); file != parent->end(); ++file)
		{
			if ((*file)->getIsSpecial())
			{
				continue;
			}
			if ((*file)->getIsDirectory())
			{
				//Dafaq is this? Allows recursion of multiple directories.
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//				using (IEnumerator<FileDescriptor> e = GetEnumerator(file))
				IEnumerator<FileDescriptor*> *e = GetEnumerator(*file);
				try
				{
					while (e->MoveNext())
					{
//C# TO C++ CONVERTER TODO TASK: C++ does not have an equivalent to the C# 'yield' keyword:
						yield return e->Current;
						e++;
					}
				}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
				finally
				{
				}
			}
			else
			{
//C# TO C++ CONVERTER TODO TASK: C++ does not have an equivalent to the C# 'yield' keyword:
				yield return file;
			}
		}
	}

	PrimaryVolumeDescriptor *<missing_class_definition>::Clone()
	{
		PrimaryVolumeDescriptor *t = dynamic_cast<PrimaryVolumeDescriptor*>(MemberwiseClone());
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
		delete t->RootDirectory;
		return t;
	}
}

VolumeDesciptor::VolumeDesciptor()
{
}

VolumeDesciptor::VolumeDesciptor(Type *t, const std::string &i, unsigned char v)
{
	VD_Type = t;
	VD_Identifier = i;
	VD_Version = v;
}

VolumeDesciptor::VolumeDesciptor(unsigned char b[])
{
	if (sizeof(b) / sizeof(b[0]) < 7)
	{
		throw new ArgumentException("sector is too small", "sector");
	}
	VD_Type = static_cast<Type*>(b[0]);
	VD_Identifier = Encoding::ASCII->GetString(b, 1, 5);
	VD_Version = b[6];
}

bool VolumeDesciptor::isValid(unsigned char buffer[], std::string &identifier)
{
	identifier = "";
	for (int i = 1; i < 6; ++i)
	{
		wchar_t b = static_cast<wchar_t>(buffer[i]);
		if (!Utilities::isDChar(b))
		{
			return false;
		}
		identifier += b;
	}
	return true;
}

ISOFile::ISOFile(Stream *file)
{
	this->file = file;
}

const Stream &ISOFile::getfile() const
{
	return privatefile;
}

void ISOFile::setfile(const Stream &value)
{
	privatefile = value;
}

const PrimaryVolumeDescriptor &ISOFile::getPrimaryVolumeDescriptor() const
{
	return pvd->Clone();
}

const long long &ISOFile::getpvdSector() const
{
	return privatepvdSector;
}

void ISOFile::setpvdSector(const long long &value)
{
	privatepvdSector = value;
}

const long long &ISOFile::getFileSize() const
{
	return file->Length;
}

const unsigned int &ISOFile::getSectorCount() const
{
	return static_cast<unsigned int>(file->Length / sectorSize);
}

const unsigned int &ISOFile::getSectorPosition() const
{
	return static_cast<unsigned int>(file::Position / sectorSize);
}

ISOFile::~ISOFile()
{
	this->Dispose(true);
}

void ISOFile::Dispose(bool disposing)
{
	if (file != 0)
	{
		delete file;
		file = 0;
	}
	if (disposing)
	{
		if (pvd != 0)
		{
			pvd = 0;
		}
	}
}

void ISOFile::EnsureBoundary()
{
	if ((file::Position % sectorSize) != 0)
	{
		int rem = sectorSize - static_cast<int>(file::Position % sectorSize);
		unsigned char buf[4];
		while (rem > 3)
		{
			file::Write(buf, 0, 4);
			rem -= 4;
		}
		file::Write(buf, 0, rem);
	}
}

void ISOFile::CopyTo(Stream *destination, unsigned int length)
{
	int num;
	unsigned char buffer[sectorSize*2];
	while (length > 0 && (num = file::Read(buffer, 0, static_cast<int>(__min(sectorSize*2, length)))) != 0)
	{
		destination->Write(buffer, 0, num);
		length -= static_cast<unsigned int>(num);
	}
}

bool ISOFile::ReadSector(unsigned char buffer[], int offset = 0, int sector = -1)
{
	if (sizeof(buffer) / sizeof(buffer[0]) < offset + sectorSize)
	{
		throw new ArgumentException("Buffer is too small to hold a sector", "buffer");
	}
	if (sector != -1)
	{
		Seek(sector);
	}
	return file::Read(buffer, offset, sectorSize) == sectorSize;
}

void ISOFile::Seek(long long sector)
{
	if (sector < 0 || sector > SectorCount)
	{
		throw new ArgumentOutOfRangeException("sector");
	}
	file->Position = sector*sectorSize;
}

void ISOFile::WriteSector(unsigned char buffer[], int offset = 0, int sector = -1)
{
	if (sizeof(buffer) / sizeof(buffer[0]) < offset + sectorSize)
	{
		throw new ArgumentException("Buffer is too small to hold a sector", "buffer");
	}
	if (sector != -1)
	{
		Seek(sector);
	}
	file::Write(buffer, offset, sectorSize);
}

FileDescriptor *ISOFile::FindFile(const std::string &path)
{
	return pvd::FindFile(path);
}

ISOFileReader::ISOFileReader(Stream *file) : ISOFile(file)
{
	if (file->Length < 17*sectorSize)
	{
		throw new InvalidDataException("ISO too small to be valid");
	}
	IsUDF = false;
	int i = 15;
	int BNT = 0;
	unsigned char sector[sectorSize];
	do
	{
		std::string type;
		if (!ReadSector(sector, 0, ++i) || !VolumeDesciptor::isValid(sector, type))
		{
			break;
		}
		Debug::WriteLine("Volume Structure Descriptor type: " + type);
		if (pvd == 0 && type == "CD001" && sector[0] == 1)
		{
			pvdSector = i;
			pvd = new PrimaryVolumeDescriptor(sector, file);
		}
		else if (BNT < 3)
		{
			if (BNT == 0 && type == "BEA01")
			{
				++BNT;
				BEASector = i;
				Debug::WriteLine("Found UDF descriptors starting at sector " + i);
			}
			else if (BNT == 1 && (type == "NSR02" || type == "NSR03"))
			{
				IsUDF = true;
				NSRSector->Add(TEASector = i);
			}
			else if (BNT == 1 && type == "TEA01")
			{
				++BNT;
				TEASector = i;
				Debug::WriteLine("Found UDF descriptors ending at sector " + i);
			}
		}
	} while (true);
	if (pvd == 0)
	{
		throw new InvalidDataException("Failed to find root directory on ISO");
	}
	Debug::WriteLine("Successfully opened ISO image");
}

const bool &ISOFileReader::getIsUDF() const
{
	return privateIsUDF;
}

void ISOFileReader::setIsUDF(const bool &value)
{
	privateIsUDF = value;
}

const int &ISOFileReader::getBEASector() const
{
	return privateBEASector;
}

void ISOFileReader::setBEASector(const int &value)
{
	privateBEASector = value;
}

const int &ISOFileReader::getTEASector() const
{
	return privateTEASector;
}

void ISOFileReader::setTEASector(const int &value)
{
	privateTEASector = value;
}

IEnumerator<FileDescriptor*> *ISOFileReader::GetEnumerator()
{
	return pvd->GetEnumerator();
}

IEnumerator *ISOFileReader::GetEnumerator()
{
	return GetEnumerator();
}

void ISOFileReader::CopyFile(FileDescriptor *file, Stream *target)
{
	Seek(file->ExtentLBA);
	CopyTo(target, file->ExtentLength);
}

GovanifY::Utility::Substream *ISOFileReader::GetFileStream(FileDescriptor *file, bool includeSectorBuffer = false)
{
	return new Substream(this->file, static_cast<long long>(file->ExtentLBA)*sectorSize, includeSectorBuffer == false ? file->ExtentLength : (static_cast<long long>(ceil(static_cast<double>(file->ExtentLength) / sectorSize))*sectorSize));
}

ISOCopyWriter::ISOCopyWriter(Stream *file, ISOFileReader *source) : ISOFile(file)
{
	this->source = source;
	file->Position = 0;
	//Find lowest first LBA
	unsigned int LBA = unsigned int::MaxValue;
	for (ISOFileReader::const_iterator f = source->begin(); f != source->end(); ++f)
	{
		if ((*f)->ExtentLBA < LBA)
		{
			LBA = (*f)->ExtentLBA;
		}
	}
	if (LBA == unsigned int::MaxValue)
	{
		throw new InvalidDataException("No files found");
	}
	//Copy first sectors, including the "encrypted" bootscreen
	source->Seek(0);
	source->CopyTo(file, LBA*sectorSize);
	//I don't have the code to update UDF, nor do I really want to write that... so I'll just destroy the NSR descriptor.
	//I'll move everything up too, and not just destroy the whole block. Gonna be nice here. ;)
	if (source->IsUDF)
	{
		unsigned char sector[sectorSize];
		for (int i = source->BEASector + 1, j = i; i <= source->TEASector; ++i, ++j)
		{
			while (source->NSRSector->Contains(j))
			{
				++j;
			}
			if (i != j)
			{
				if (i < source->TEASector)
				{
					Debug::WriteLine("Move sector " + j + " to " + i);
					ReadSector(sector, 0, j);
					WriteSector(sector, 0, i);
				}
				else
				{
					if (i == source->TEASector)
					{
						Array->Clear(sector, 0, sectorSize);
					}
					Debug::WriteLine("Clear sector " + source->TEASector);
					WriteSector(sector, 0, i);
				}
			}
		}
	}
}

const ISOFileReader &ISOCopyWriter::getsource() const
{
	return privatesource;
}

void ISOCopyWriter::setsource(const ISOFileReader &value)
{
	privatesource = value;
}

void ISOCopyWriter::mFinalize()
{
	long long pvdo = source::pvdSector*sectorSize;
	file->Position = pvdo + 80;
	//VolumeSpaceSize
	file::Write(Utilities::BEInt32(SectorCount), 0, 8);
	//VolumeModification
	file->Position = pvdo + 830;
	file::Write(Utilities::VolumeTimeFromDateTime(DateTime::UtcNow), 0, 17);
	final = true;
}

void ISOCopyWriter::AddFile2(FileDescriptor *f, Stream *data, const std::string &name)
{
#if defined(extract)
	Stream *Extracted = File->Open(name, FileMode::Create, FileAccess::Write);
	data->CopyTo(Extracted);
	delete this;
#endif
	SeekEnd();
	EnsureBoundary();
	f->ExtentLBA = SectorPosition;
	f->ExtentLength = static_cast<unsigned int>(data->Length);
	data->CopyTo(file);
	EnsureBoundary();
	PatchFile(f);
}

void ISOCopyWriter::AddFile(FileDescriptor *f, Stream *data)
{
	SeekEnd();
	EnsureBoundary();
	f->ExtentLBA = SectorPosition;
	f->ExtentLength = static_cast<unsigned int>(data->Length);
	data->CopyTo(file);
	EnsureBoundary();
	PatchFile(f);
}

void ISOCopyWriter::CopyFile(FileDescriptor *f)
{
//C# TO C++ CONVERTER NOTE: The following 'using' block is replaced by its C++ equivalent:
//	using (Stream data = source.GetFileStream(f))
	Stream *data = source::GetFileStream(f);
	try
	{
		AddFile(f, data);
	}
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to the exception 'finally' clause:
	finally
	{
		if (data != 0)
			data.Dispose();
	}
}

void ISOCopyWriter::PatchFile(FileDescriptor *f)
{
	file->Position = f->RawOffset + 2;
	file::Write(Utilities::BEInt32(f->ExtentLBA), 0, 8);
	file::Write(Utilities::BEInt32(f->ExtentLength), 0, 8);
	file::Write(Utilities::DirectoryTimeFromDateTime(f->RecordingDate), 0, 7);
}

void ISOCopyWriter::SeekEnd()
{
	file::Seek(0, SeekOrigin::End);
	EnsureBoundary();
}

void ISOCopyWriter::Dispose(bool disposing)
{
	if (!final)
	{
		mFinalize();
	}
	if (source != 0)
	{
		delete source;
		source = 0;
	}
	ISOFile::Dispose(disposing);
}
