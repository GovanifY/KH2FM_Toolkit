#pragma once

#include "GY_BinaryStream.h"
#include "GY_Substream.h"
#include <string>
#include <vector>
#include <cmath>
#include <stdexcept>

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
	//For Defaults, I'm using values similar to what a written PS2 disc would contain
	class Utilities
	{
		/// <summary>Returns the specified number in both-endian format</summary>
		/// <param name="i">A number to convert.</param>
		//internal static UInt64 BEInt32(UInt32 i) { return i | (UInt64)(i & 0xFF000000) << 8 | (UInt64)(i & 0x00FF0000) << 24 | (UInt64)(i & 0x0000FF00) << 40 | (UInt64)(i & 0x000000FF) << 56; }
	public:
		static unsigned char *BEInt32(unsigned int i);

		/// <summary>Convert a VolumeTime to DateTime</summary>
		/// <param name="array">Array containing the VolumeTime</param>
		/// <param name="offset">Offset to VolumeTime</param>
		/// <returns>Equivalent DateTime</returns>
		static DateTime DateTimeFromVolumeTime(array_Renamed<unsigned char> *array_Renamed, long long offset = 0);

		/// <summary>Convert a DateTime to VolumeTime</summary>
		/// <param name="date">Input Time</param>
		/// <param name="array">Target array</param>
		/// <param name="offset">Offset to begin writing</param>
		static void VolumeTimeFromDateTime(DateTime date, array_Renamed<unsigned char> *array_Renamed, int offset = 0);

		static unsigned char *VolumeTimeFromDateTime(DateTime date);

		/// <summary>Convert a DirectoryTime to DateTime</summary>
		/// <param name="array">Array containing the DirectoryTime</param>
		/// <param name="offset">Offset to DirectoryTime</param>
		/// <returns>Equivalent DateTime</returns>
		static DateTime DateTimeFromDirectoryTime(array_Renamed<unsigned char> *array_Renamed, long long offset = 0);

		/// <summary>Convert a DateTime to DirectoryTime</summary>
		/// <param name="date">Input Time</param>
		/// <param name="array">Target array</param>
		/// <param name="offset">Offset to begin writing</param>
		static void DirectoryTimeFromDateTime(DateTime date, array_Renamed<unsigned char> *array_Renamed, int offset = 0);

		static unsigned char *DirectoryTimeFromDateTime(DateTime date);

		static bool isAChar(wchar_t c);

		static bool isDChar(wchar_t c);

		static bool isAString(wchar_t str[]);

		static bool isDString(wchar_t str[]);

		static bool isFileName(wchar_t str[]);

		static std::string parseADString(wchar_t str[]);

		static std::string parseADFileName(wchar_t str[]);
	};

	class FileDescriptor : public IEnumerable<FileDescriptor*>
	{
	public:
//C# TO C++ CONVERTER NOTE: The following .NET attribute has no direct equivalent in native C++:
//[Serializable]
		class EmptyDescriptorException : public std::exception
		{
		};
	public:
//C# TO C++ CONVERTER NOTE: The following .NET attribute has no direct equivalent in native C++:
//[Flags]
		enum Flags
		{
			None = 0,
			Hidden = 1,
			Directory = 2,
			Associated = 4,
			Extended = 8,
			Permissions = 16,
			NotFinal = 128
		};

		/// <summary>List of children FileDescriptors</summary>
	private:
		const std::vector<FileDescriptor*> children;

		/// <summary>Extended Attribute Record length</summary>
	public:
		unsigned char ExtendedAttributeRecordLength;

		/// <summary>Location of extent (LBA)</summary>
		unsigned int ExtentLBA;

		/// <summary>Data length (size of extent)</summary>
		unsigned int ExtentLength;

		/// <summary>File flags</summary>
		Flags FileFlags;

		/// <summary>File unit size for files recorded in interleaved mode, zero otherwise</summary>
		unsigned char FileUnitSize;

		/// <summary>Interleave gap size for files recorded in interleaved mode, zero otherwise</summary>
		unsigned char InterleaveGapSize;

		/// <summary>Length of Directory Record</summary>
		unsigned char Length;

		/// <summary>Raw byte offset in ISO</summary>
		long long RawOffset;

		/// <summary>Recording date and time</summary>
		DateTime RecordingDate;

		/// <summary>Volume sequence number</summary>
		unsigned short VolumeSequence;

		/// <summary>File identifier RAW</summary>
	private:
		std::string _FileIdentifier;

		/// <summary>Filename RAW</summary>
		std::string _FileName;

		/// <summary>Full path name RAW</summary>
		std::string _FullName;

	public:
		FileDescriptor(FileDescriptor *parent = 0);

		FileDescriptor(BinaryStream *br, FileDescriptor *parent = 0);

		/// <summary>
		///     <para>File identifier accessor</para>
		///     <para>Resets <c>FileName</c> & <c>FullName</c> when set (non-recursive)</para>
		/// </summary>
		const std::string &getFileIdentifier() const;
		void setFileIdentifier(const std::string &value);

		/// <summary>Filename</summary>
		const std::string &getFileName() const;

		/// <summary>Full path name</summary>
		const std::string &getFullName() const;

		/// <summary>Parent FileDescriptor, or null if root</summary>
	private:
		FileDescriptor *privateparent;
	public:
		const FileDescriptor &getparent() const;
		void setparent(const FileDescriptor &value);

		/// <summary>Number of children</summary>
		const int &getChildrenCount() const;

		/// <summary>True if this represents a directory</summary>
		const bool &getIsDirectory() const;

		/// <summary>True if the file is not hidden</summary>
		const bool &getIsVisible() const;

		/// <summary>True if this represents itself or its parent</summary>
		const bool &getIsSpecial() const;

		//Pass through
		IEnumerator<FileDescriptor*> *GetEnumerator();

	private:
		IEnumerator *IEnumerable_GetEnumerator();

		/// <summary>Parse all children of this directory</summary>
		/// <param name="iso">ISO containing this FileDescriptor</param>
		/// <param name="recurse">Parse children directories as well</param>
	public:
		void ParseSubFiles(Stream *iso, bool recurse = false);

		/// <summary>Parse all children of this directory</summary>
		/// <param name="iso">ISO containing this FileDescriptor</param>
		/// <param name="recurse">Parse children directories as well</param>
		void ParseSubFiles(BinaryStream *iso, bool recurse = false);

		/// <summary>Parse all children of this directory</summary>
		/// <param name="br"><c>BinaryReader</c> with access to the ISO data</param>
		/// <param name="recurse">Parse children directories as well</param>
		/// <param name="recurseC">Current recursion level</param>
	private:
		void ParseSubFiles(BinaryStream *br, bool recurse = false, int recurseC = 0);

		/// <summary>Get a child FileDescriptor by name</summary>
		/// <param name="name">Filename</param>
		/// <returns>Specified FileDescriptor</returns>
		/// <exception cref="FileNotFoundException">Specified <c>FileDescriptor</c> was not found</exception>
	public:
		FileDescriptor *GetChild(const std::string &name);

		/// <summary>Add specified FileDescriptor as a child</summary>
		/// <param name="newchild">FileDescriptor</param>
		void AddChild(FileDescriptor *newchild);

		/// <summary>
		///     Thrown when a descriptor is empty (<c>Length</c> < 34)</summary>
	};

	class PrimaryVolumeDescriptor : public VolumeDesciptor, IEnumerable<FileDescriptor*>
	{
		/// <summary>
		///     <para>strD 36 bytes</para>
		///     <para>Filename of a file in the root directory that contains abstract information for this volume set</para>
		///     <para>If not specified, all bytes should be 0x20</para>
		/// </summary>
	public:
		std::string AbstractFileIdentifier;

		/// <summary>
		///     <para>strA 128 bytes</para>
		///     <para>Identifies how the data are recorded on this volume</para>
		///     <para>
		///         For extended information, the first byte should be 0x5F, followed by the filename of a file in the root
		///         directory. If not specified, all bytes should be 0x20
		///     </para>
		/// </summary>
		std::string ApplicationIdentifier;

		/// <summary>
		///     <para>strD 37 bytes</para>
		///     <para>Filename of a file in the root directory that contains bibliographic information for this volume set</para>
		///     <para>If not specified, all bytes should be 0x20</para>
		/// </summary>
		std::string BibliographicFileIdentifier;

		/// <summary>
		///     <para>strD 38 bytes</para>
		///     <para>Filename of a file in the root directory that contains copyright information for this volume set</para>
		///     <para>If not specified, all bytes should be 0x20</para>
		/// </summary>
		std::string CopyrightFileIdentifier;

		/// <summary>
		///     <para>strA 128 bytes</para>
		///     <para>The identifier of the person(s) who prepared the data for this volume</para>
		///     <para>
		///         For extended information, the first byte should be 0x5F, followed by the filename of a file in the root
		///         directory. If not specified, all bytes should be 0x20
		///     </para>
		/// </summary>
		std::string DataPreparerIdentifier;

		/// <summary>
		///     <para>Both-endian UInt16</para>
		///     <para>The size in bytes of a logical block</para>
		/// </summary>
		unsigned short LogicalBlockSize;

		/// <summary>
		///     <para>Little-endian UInt32</para>
		///     <para>LBA location of the optional path table</para>
		///     <para> The path table pointed to contains only little-endian values</para>
		/// </summary>
		unsigned int PathLOptionalPosition;

		/// <summary>
		///     <para>Little-endian UInt32</para>
		///     <para>LBA location of the path table</para>
		///     <para> The path table pointed to contains only little-endian values</para>
		/// </summary>
		unsigned int PathLPosition;

		/// <summary>
		///     <para>Both-endian UInt32</para>
		///     <para>The size in bytes of the path table</para>
		/// </summary>
		unsigned int PathTableSize;

		/// <summary>
		///     <para>strA 128 bytes</para>
		///     <para>The volume publisher</para>
		///     <para>
		///         For extended information, the first byte should be 0x5F, followed by the filename of a file in the root
		///         directory. If not specified, all bytes should be 0x20
		///     </para>
		/// </summary>
		std::string PublisherIdentifier;

		//public uint PathMPosition;
		//public uint PathMOptionalPosition;
		/// <summary>
		///     <para>Big-endian UInt32</para>
		///     <para>LBA location of the path table</para>
		///     <para> The path table pointed to contains only big-endian values</para>
		/// </summary>
		/// <summary>
		///     <para>Big-endian UInt32</para>
		///     <para>LBA location of the optional path table</para>
		///     <para> The path table pointed to contains only big-endian values</para>
		/// </summary>
		/// <summary>Directory entry for the root directory</summary>
		FileDescriptor *RootDirectory;

		//private byte Unused;
		/// <summary>Always 0</summary>
		/// <summary>
		///     <para>strA 32 bytes</para>
		///     <para>The name of the system that can act upon sectors 0x00-0x0F for the volume</para>
		/// </summary>
		std::string SystemIdentifier;

		/// <summary>The date and time of when the volume was created</summary>
		DateTime VolumeCreation;

		/// <summary>
		///     <para>The date and time after which the volume may be used</para>
		///     <para>If not specified, the volume may be used immediately</para>
		/// </summary>
		DateTime VolumeEffective;

		/// <summary>
		///     <para>The date and time after which this volume is considered to be obsolete</para>
		///     <para>If not specified, then the volume is never considered to be obsolete</para>
		/// </summary>
		DateTime VolumeExpiration;

		/// <summary>
		///     <para>strD 32 bytes</para>
		///     <para>Identification of this volume.</para>
		/// </summary>
		std::string VolumeIdentifier;

		/// <summary>The date and time of when the volume was modified</summary>
		DateTime VolumeModification;

		/// <summary>
		///     <para>Both-endian UInt16</para>
		///     <para>The number of this disk in the Volume Set</para>
		/// </summary>
		unsigned short VolumeSequenceNumber;

		/// <summary>
		///     <para>strD 128 bytes</para>
		///     <para>Identifier of the volume set of which this volume is a member</para>
		/// </summary>
		std::string VolumeSetIdentifier;

		/// <summary>
		///     <para>Both-endian UInt16</para>
		///     <para>The size of the set in this logical volume (number of disks)</para>
		/// </summary>
		unsigned short VolumeSetSize;

		/// <summary>
		///     <para>Both-endian UInt32</para>
		///     <para>Number of Logical Blocks in which the volume is recorded</para>
		/// </summary>
		unsigned int VolumeSpaceSize;

		/// <summary>Always 1</summary>
		//public byte FileStructureVersion;
		//private byte Unused3;
		//512 byte application reserved
		//653 byte ISO reserved
		PrimaryVolumeDescriptor();

		PrimaryVolumeDescriptor(unsigned char sector[], Stream *iso = 0);
	};

		/// <summary>Loop through all non-directory files</summary>
	public:
		IEnumerator<FileDescriptor*> *GetEnumerator();

	private:
		IEnumerator *GetEnumerator();

		/// <summary>Find a file by path</summary>
		/// <param name="path">Path to the file</param>
		/// <returns>FileDescriptor describing the file</returns>
		/// <exception cref="FileNotFoundException">The specified file was not found or there was a problem traversing the path</exception>
	public:
		FileDescriptor *FindFile(const std::string &path);

	private:
		static IEnumerator<FileDescriptor*> *GetEnumerator(FileDescriptor *parent);

		/// <summary>
		///     <para>Returns a shallow clone of the current object.</para>
		///     <para><c>RootDirectory</c> is null in the clone.</para>
		/// </summary>
		/// <returns>Shallow clone</returns>
	public:
		PrimaryVolumeDescriptor *Clone();
}

	class VolumeDesciptor
	{
	public:
		enum Type
		{
			BootRecord = 0,
			PrimaryVolumeDescriptor = 1,
			SupplementaryVolumeDescriptor = 2,
			VolumePartitionDescriptor = 3,
			VolumeDescriptorSetTerminator = 255
		};

	public:
		const std::string VD_Identifier;
		Type *const VD_Type;
		const unsigned char VD_Version;

//C# TO C++ CONVERTER TODO TASK: Calls to same-class constructors are not supported in C++ prior to C++0x:
//ORIGINAL LINE: public VolumeDesciptor() : this(Type.VolumeDescriptorSetTerminator, "", 1)
		VolumeDesciptor();

		VolumeDesciptor(Type *t, const std::string &i, unsigned char v);

		VolumeDesciptor(unsigned char b[]);

		/// <summary>Checks if the specified array is a valid ISO Volume Descriptor</summary>
		/// <param name="buffer">An array of bytes to check</param>
		/// <returns>True if it is valid, false otherwise.</returns>
		static bool isValid(unsigned char buffer[], std::string &identifier);
	};

	class ISOFile
	{
		/// <summary>
		///     <para>Sector size of the ISO</para>
		///     <para>Almost always 2048 bytes</para>
		/// </summary>
	public:
		static const int sectorSize = 2048;

		/// <summary>Primary Volume Descriptor of the ISO</summary>
	protected:
		PrimaryVolumeDescriptor *pvd;

	public:
		ISOFile(Stream *file);

		/// <summary>ISO file stream</summary>
	private:
		Stream *privatefile;
	public:
		const Stream &getfile() const;
		void setfile(const Stream &value);

		/// <summary>Clone of the ISO's Primary Volume Descriptor</summary>
		const PrimaryVolumeDescriptor &getPrimaryVolumeDescriptor() const;

	private:
		long long privatepvdSector;
	public:
		const long long &getpvdSector() const;
		void setpvdSector(const long long &value);

		/// <summary>Returns the current filesize of the stream</summary>
		const long long &getFileSize() const;

		/// <summary>Returns the current number of sectors in the stream</summary>
		const unsigned int &getSectorCount() const;

		/// <summary>Current sector position</summary>
		const unsigned int &getSectorPosition() const;

		~ISOFile();

	private:
		void Dispose(bool disposing);

	protected:
		void EnsureBoundary();

		/// <summary>Read the specified number of bytes and write them to a destination stream</summary>
		/// <param name="destination">The stream that will contain the specified data</param>
		/// <param name="length">Maximum length of data to copy</param>
	public:
		void CopyTo(Stream *destination, unsigned int length);

		/// <summary>Read a sector from the stream</summary>
		/// <param name="buffer">An array of bytes to read into</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read</param>
		/// <param name="sector">A sector to <c>Seek</c> to before reading</param>
		/// <returns>True if a full sector was read, false otherwise.</returns>
		/// <exception cref="ArgumentException">Thrown when buffer is too small to hold a sector</exception>
		bool ReadSector(unsigned char buffer[], int offset = 0, int sector = -1);

		/// <summary>Sets the position within the stream</summary>
		/// <param name="sector">Sector index to seek to</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <c>sector</c> is beyond the stream's range</exception>
		void Seek(long long sector);

		/// <summary>Write a sector to the stream</summary>
		/// <param name="buffer">An array of bytes to write</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin writing the data</param>
		/// <param name="sector">A sector to <c>Seek</c> to before reading</param>
		/// <exception cref="ArgumentException">Thrown when buffer is too small to hold a sector</exception>
	protected:
		void WriteSector(unsigned char buffer[], int offset = 0, int sector = -1);

		/// <summary>Find a file by name</summary>
		/// <param name="path">Path to the file</param>
		/// <returns>FileDescriptor describing the file</returns>
		/// <exception cref="FileNotFoundException">The specified file was not found or there was a problem traversing the path</exception>
	public:
		FileDescriptor *FindFile(const std::string &path);
	};

	class ISOFileReader : public ISOFile, IEnumerable<FileDescriptor*>
	{
	public:
		std::vector<int> NSRSector;

		ISOFileReader(Stream *file);

	private:
		bool privateIsUDF;
	public:
		const bool &getIsUDF() const;
		void setIsUDF(const bool &value);
	private:
		int privateBEASector;
	public:
		const int &getBEASector() const;
		void setBEASector(const int &value);
	private:
		int privateTEASector;
	public:
		const int &getTEASector() const;
		void setTEASector(const int &value);

		/// <summary>Enumerate over files in the ISO</summary>
		IEnumerator<FileDescriptor*> *GetEnumerator();

	private:
		IEnumerator *GetEnumerator();

		/// <summary>Copy specified file to target stream</summary>
		/// <param name="file">FileDescriptor describing the file to copy</param>
		/// <param name="target">The stream that will contain the specified data</param>
	public:
		void CopyFile(FileDescriptor *file, Stream *target);

		/// <summary>Returns a <c>Substream</c> of the file</summary>
		/// <param name="file">FileDescriptor describing the file</param>
		/// <param name="includeSectorBuffer">Include the buffer after the file.</param>
		/// <returns><c>Substream</c> containing only the requested file</returns>
		Substream *GetFileStream(FileDescriptor *file, bool includeSectorBuffer = false);
	};

	class ISOCopyWriter : public ISOFile
	{
	private:
		bool final;

	public:
		ISOCopyWriter(Stream *file, ISOFileReader *source);

	private:
		ISOFileReader *privatesource;
	public:
		const ISOFileReader &getsource() const;
		void setsource(const ISOFileReader &value);

	private:
		void mFinalize();

	public:
		void AddFile2(FileDescriptor *f, Stream *data, const std::string &name);

		void AddFile(FileDescriptor *f, Stream *data);

		void CopyFile(FileDescriptor *f);

		void PatchFile(FileDescriptor *f);

		void SeekEnd();

	private:
		void Dispose(bool disposing);
	};
}
