﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GovanifY.Utility;

namespace ISO_Tools
{
    //For Defaults, I'm using values similar to what a written PS2 disc would contain
    static internal class Utilities
    {
        /// <summary>Returns the specified number in both-endian format</summary>
        /// <param name="i">A number to convert.</param>
        //internal static UInt64 BEInt32(UInt32 i) { return i | (UInt64)(i & 0xFF000000) << 8 | (UInt64)(i & 0x00FF0000) << 24 | (UInt64)(i & 0x0000FF00) << 40 | (UInt64)(i & 0x000000FF) << 56; }
        public static byte[] BEInt32(UInt32 i) {
            return new byte[8]{
                (byte)(i),
                (byte)(i>>8),
                (byte)(i>>16),
                (byte)(i>>24),
                (byte)(i>>24),
                (byte)(i>>16),
                (byte)(i>>8),
                (byte)(i)
            };
        }
        /// <summary>Convert a VolumeTime to DateTime</summary>
        /// <param name="array">Array containing the VolumeTime</param>
        /// <param name="offset">Offset to VolumeTime</param>
        /// <returns>Equivalent DateTime</returns>
        public static System.DateTime DateTimeFromVolumeTime(byte[] array, long offset = 0)
        {
            try
            {
                System.DateTime date = new System.DateTime(
                    (array[offset] - '0') * 1000 + (array[offset + 1] - '0') * 100 + (array[offset + 2] - '0') * 10 + (array[offset + 3] - '0'),
                    (array[offset + 4] - '0') * 10 + (array[offset + 5] - '0'),
                    (array[offset + 6] - '0') * 10 + (array[offset + 7] - '0'),
                    (array[offset + 8] - '0') * 10 + (array[offset + 9] - '0'),
                    (array[offset + 10] - '0') * 10 + (array[offset + 11] - '0'),
                    (array[offset + 12] - '0') * 10 + (array[offset + 13] - '0'),
                    (array[offset + 14] - '0') * 100 + (array[offset + 15] - '0') * 10,
                    DateTimeKind.Utc
                );
                return date.AddMinutes(-(15 * (sbyte)array[offset + 16]));
            }
            catch (ArgumentOutOfRangeException) { return System.DateTime.MinValue; }
        }
        /// <summary>Convert a DateTime to VolumeTime</summary>
        /// <param name="date">Input Time</param>
        /// <param name="array">Target array</param>
        /// <param name="offset">Offset to begin writing</param>
        public static void VolumeTimeFromDateTime(System.DateTime date, byte[] array, int offset = 0)
        {
            if (date == System.DateTime.MinValue)
            {
                for (int i = 0; i < 16; ++i) { array[offset + i] = (byte)'0'; }
            }
            else
            {
                Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes(date.ToString("yyyyMMddHHmmssff")), 0, array, offset, 16);
            }
            array[offset + 16] = 0;  //UTC
        }
        public static byte[] VolumeTimeFromDateTime(System.DateTime date)
        {
            byte[] buf = new byte[17];
            VolumeTimeFromDateTime(date, buf, 0);
            return buf;
        }
        /// <summary>Convert a DirectoryTime to DateTime</summary>
        /// <param name="array">Array containing the DirectoryTime</param>
        /// <param name="offset">Offset to DirectoryTime</param>
        /// <returns>Equivalent DateTime</returns>
        public static System.DateTime DateTimeFromDirectoryTime(byte[] array, long offset = 0)
        {
            try
            {
                System.DateTime date = new System.DateTime(
                    1900 + array[offset],
                    array[offset + 1],
                    array[offset + 2],
                    array[offset + 3],
                    array[offset + 4],
                    array[offset + 5],
                    DateTimeKind.Utc
                );
                return date.AddMinutes(-(15 * (sbyte)array[offset + 6]));
            }
            catch (ArgumentOutOfRangeException) { return System.DateTime.MinValue; }
        }
        /// <summary>Convert a DateTime to DirectoryTime</summary>
        /// <param name="date">Input Time</param>
        /// <param name="array">Target array</param>
        /// <param name="offset">Offset to begin writing</param>
        public static void DirectoryTimeFromDateTime(System.DateTime date, byte[] array, int offset = 0)
        {
            if (date == System.DateTime.MinValue) { Array.Clear(array, offset, 7); }
            else
            {
                array[offset] = (byte)(date.Year - 1900);
                array[offset + 1] = (byte)date.Month;
                array[offset + 2] = (byte)date.Day;
                array[offset + 3] = (byte)date.Hour;
                array[offset + 4] = (byte)date.Minute;
                array[offset + 5] = (byte)date.Second;
                array[offset + 6] = 0;  //UTC
            }
        }
        public static byte[] DirectoryTimeFromDateTime(System.DateTime date)
        {
            byte[] buf = new byte[7];
            DirectoryTimeFromDateTime(date, buf, 0);
            return buf;
        }
        public static bool isAChar(char c) { return c >= 'A' && c <= 'Z' || c >= '%' && c <= '9' || c >= ' ' && c <= '"' || c >= ':' && c <= '?' || c == '_'; }
        public static bool isDChar(char c) { return c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_'; }
        public static bool isAString(char[] str) { foreach (char c in str) { if (!isAChar(c)) { return false; } } return true; }
        public static bool isDString(char[] str) { foreach (char c in str) { if (!isDChar(c)) { return false; } } return true; }
        public static bool isFileName(char[] str)
        {
            bool dot = false, semi = false;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (!isDChar(c))
                {
                    //There must be 1 dot, before the semicolon and after the filename
                    if (c == '.' && !dot && !semi && i != 0) { dot = true; }
                    //There must be 1 semicolon, after the dot and before the version
                    else if (c == ';' && !semi && dot && i + 1 != str.Length) { semi = true; }
                    else { return false; }
                }
            }
            return dot && semi;
        }
        public static string parseADString(char[] str)
        {
            int len = str.Length;
            while (--len >= 0 && (str[len] == ' ')) { }
            return new string(str, 0, len + 1);
        }
        public static string parseADFileName(char[] str)
        {
            int len = str.Length;
            while (--len >= 0 && (str[len] == ' ')) { }
            if (len == 0)
            {
                switch ((byte)str[0])
                {
                    case 0: return ".";
                    case 1: return "..";
                }
            }
            return new string(str, 0, len + 1);
        }
    }
    internal sealed class FileDescriptor : IEnumerable<FileDescriptor>
    {
        [Flags]
        public enum Flags : byte
        {
            None = 0,
            Hidden = 1,
            Directory = 2,
            Associated = 4,
            Extended = 8,
            Permissions = 16,
            NotFinal = 128
        }
        /// <summary>Thrown when a descriptor is empty (<c>Length</c> < 34)</summary>
        [Serializable]
        public class EmptyDescriptorException : Exception { public EmptyDescriptorException() { } }
        /// <summary>Raw byte offset in ISO</summary>
        public long RawOffset;
        /// <summary>Length of Directory Record</summary>
        public byte Length;
        /// <summary>Extended Attribute Record length</summary>
        public byte ExtendedAttributeRecordLength;
        /// <summary>Location of extent (LBA)</summary>
        public uint ExtentLBA;
        /// <summary>Data length (size of extent)</summary>
        public uint ExtentLength;
        /// <summary>Recording date and time</summary>
        public DateTime RecordingDate;
        /// <summary>File flags</summary>
        public Flags FileFlags;
        /// <summary>File unit size for files recorded in interleaved mode, zero otherwise</summary>
        public byte FileUnitSize;
        /// <summary>Interleave gap size for files recorded in interleaved mode, zero otherwise</summary>
        public byte InterleaveGapSize;
        /// <summary>Volume sequence number</summary>
        public ushort VolumeSequence;

        /// <summary>File identifier RAW</summary>
        private string _FileIdentifier;
        /// <summary><para>File identifier accessor</para><para>Resets <c>FileName</c> & <c>FullName</c> when set (non-recursive)</para></summary>
        public string FileIdentifier { get { return this._FileIdentifier; } set { this._FileIdentifier = value; this._FileName = this._FullName = null; } }
        /// <summary>Filename RAW</summary>
        private string _FileName = null;
        /// <summary>Filename</summary>
        public string FileName { get { if (this._FileName == null) { if (this.IsDirectory) { this._FileName = this._FileIdentifier; } else { this._FileName = this._FileIdentifier.Substring(0, this._FileIdentifier.IndexOf(';')); } } return this._FileName; } }
        /// <summary>Full path name RAW</summary>
        private string _FullName = null;
        /// <summary>Full path name</summary>
        public string FullName { get { if (this.parent == null) { return this.FileName; } if (this._FullName == null) { this._FullName = this.parent.FullName + "/" + this.FileName; } return this._FullName; } }
        /// <summary>Parent FileDescriptor, or null if root</summary>
        public FileDescriptor parent { get; private set; }
        /// <summary>List of children FileDescriptors</summary>
        private List<FileDescriptor> children = new List<FileDescriptor>();
        /// <summary>Number of children</summary>
        public int ChildrenCount { get { return this.children.Count; } }
        /// <summary>True if this represents a directory</summary>
        public bool IsDirectory { get { return FileFlags.HasFlag(Flags.Directory); } }
        /// <summary>True if the file is not hidden</summary>
        public bool IsVisible { get { return !FileFlags.HasFlag(Flags.Hidden); } }
        /// <summary>True if this represents itself or its parent</summary>
        public bool IsSpecial { get { return this.FileIdentifier == "." || this.FileIdentifier == ".."; } }
        public FileDescriptor(FileDescriptor parent = null)
        {
            this.parent = parent;
            this.Length = 34;
            this.ExtendedAttributeRecordLength = 0;
            this.ExtentLBA = 0;
            this.ExtentLength = 0;
            this.RecordingDate = DateTime.MinValue;
            this.FileFlags = Flags.None;
            this.FileUnitSize = 0;
            this.InterleaveGapSize = 0;
            this.VolumeSequence = 1;
            this._FileIdentifier = "";
            this.RawOffset = -1;
        }
        public FileDescriptor(BinaryStream br, FileDescriptor parent = null)
        {
            if (br.TextEncoding != System.Text.Encoding.ASCII) { br.TextEncoding = System.Text.Encoding.ASCII; }
            this.parent = parent;
            this.RawOffset = br.Tell();
            this.Length = br.ReadByte();
            if (this.Length < 34) { throw new EmptyDescriptorException(); }
            if (this.RawOffset + 1 + this.Length > br.BaseStream.Length) { throw new ArgumentException("Input stream is too small", "br"); }
            this.ExtendedAttributeRecordLength = br.ReadByte();
            this.ExtentLBA = br.ReadUInt32();
            br.Seek(4, SeekOrigin.Current);
            this.ExtentLength = br.ReadUInt32();
            br.Seek(4, SeekOrigin.Current);
            this.RecordingDate = Utilities.DateTimeFromDirectoryTime(br.ReadBytes(7));
            this.FileFlags = (Flags)br.ReadByte();
            this.FileUnitSize = br.ReadByte();
            this.InterleaveGapSize = br.ReadByte();
            this.VolumeSequence = br.ReadUInt16();
            br.Seek(2, SeekOrigin.Current);
            int filenamelen = br.ReadByte();
            this._FileIdentifier = Utilities.parseADFileName(br.ReadChars(filenamelen));
            br.Seek(this.Length - (33 + filenamelen), SeekOrigin.Current);
            Debug.WriteLine(String.Format("----- FileDescriptor Begin -----\r\nExtendedAttributeRecordLength: {0}\r\nExtentLBA: {1}\r\nExtentLength: {2}\r\nRecordingDate: {3:O}\r\nFileFlags: {4}\r\nFileUnitSize: {5}\r\nInterleaveGapSize: {6}\r\nVolumeSequence: {7}\r\nfilenamelen: {8}\r\nFileIdentifier: {9}\r\nLength: {10}\r\nSkip at end: {11}\r\n----- FileDescriptor End -----", this.ExtendedAttributeRecordLength, this.ExtentLBA, this.ExtentLength, this.RecordingDate, this.FileFlags, this.FileUnitSize, this.InterleaveGapSize, this.VolumeSequence, filenamelen, this.FileIdentifier, this.Length, this.Length - (33 + filenamelen)));
        }
        //Pass through
        public IEnumerator<FileDescriptor> GetEnumerator() { return this.children.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        /// <summary>Parse all children of this directory</summary>
        /// <param name="iso">ISO containing this FileDescriptor</param>
        /// <param name="recurse">Parse children directories as well</param>
        public void ParseSubFiles(Stream iso, bool recurse = false)
        {
            if (!this.IsDirectory) { throw new NotSupportedException("Only directories can have children"); }
            using (BinaryStream br = new BinaryStream(iso, leaveOpen:true)) { this.ParseSubFiles(br, recurse, 0); }
        }
        /// <summary>Parse all children of this directory</summary>
        /// <param name="iso">ISO containing this FileDescriptor</param>
        /// <param name="recurse">Parse children directories as well</param>
        public void ParseSubFiles(BinaryStream iso, bool recurse = false)
        {
            if (!this.IsDirectory) { throw new NotSupportedException("Only directories can have children"); }
            this.ParseSubFiles(iso, recurse, 0);
        }
        /// <summary>Parse all children of this directory</summary>
        /// <param name="br"><c>BinaryReader</c> with access to the ISO data</param>
        /// <param name="recurse">Parse children directories as well</param>
        /// <param name="recurseC">Current recursion level</param>
        private void ParseSubFiles(BinaryStream br, bool recurse = false, int recurseC = 0)
        {
            this.children.Clear();
            long remaining = this.ExtentLength;
            br.Seek(this.ExtentLBA * ISOFile.sectorSize, SeekOrigin.Begin);
            while (remaining >= 34)
            {
                try
                {
                    FileDescriptor child = new FileDescriptor(br, this);
                    remaining -= child.Length;
                    // Don't add self or parent
                    if (child.IsSpecial) { continue; }
                    this.children.Add(child);
                    //Max directory depth is 8. Also, prevent recursive loops.
                    if (recurse && child.IsDirectory && recurseC < 7)
                    {
                        long posC = br.Tell();
                        child.ParseSubFiles(br, true, recurseC + 1);
                        br.Seek(posC, SeekOrigin.Begin);
                    }
                }
                catch (EmptyDescriptorException) { Debug.WriteLine("EmptyDescriptorException At position: " + br.Tell()); return; }
            }
        }
        /// <summary>Get a child FileDescriptor by name</summary>
        /// <param name="name">Filename</param>
        /// <returns>Specified FileDescriptor</returns>
        /// <exception cref="FileNotFoundException">Specified <c>FileDescriptor</c> was not found</exception>
        public FileDescriptor GetChild(string name)
        {
            foreach (FileDescriptor child in this.children)
            {
                if (child.FileName == name) { return child; }
            }
            throw new FileNotFoundException("Failed to find file in self");
        }
        /// <summary>Add specified FileDescriptor as a child</summary>
        /// <param name="newchild">FileDescriptor</param>
        public void AddChild(FileDescriptor newchild)
        {
            if (newchild.parent != null) { newchild.parent.children.Remove(newchild); }
            newchild.parent = this;
            children.Add(newchild);
        }
    }
    internal sealed class PrimaryVolumeDescriptor : VolumeDesciptor, IEnumerable<FileDescriptor>
    {
        /// <summary>Always 0</summary>
        //private byte Unused;
        /// <summary><para>strA 32 bytes</para><para>The name of the system that can act upon sectors 0x00-0x0F for the volume</para></summary>
        public string SystemIdentifier;
        /// <summary><para>strD 32 bytes</para><para>Identification of this volume.</para></summary>
        public string VolumeIdentifier;
        //private UInt64 Unused2;
        /// <summary><para>Both-endian UInt32</para><para>Number of Logical Blocks in which the volume is recorded</para></summary>
        public uint VolumeSpaceSize;
        /// <summary><para>Both-endian UInt16</para><para>The size of the set in this logical volume (number of disks)</para></summary>
        public ushort VolumeSetSize;
        /// <summary><para>Both-endian UInt16</para><para>The number of this disk in the Volume Set</para></summary>
        public ushort VolumeSequenceNumber;
        /// <summary><para>Both-endian UInt16</para><para>The size in bytes of a logical block</para></summary>
        public ushort LogicalBlockSize;
        /// <summary><para>Both-endian UInt32</para><para>The size in bytes of the path table</para></summary>
        public uint PathTableSize;
        /// <summary><para>Little-endian UInt32</para><para>LBA location of the path table</para><para> The path table pointed to contains only little-endian values</para></summary>
        public uint PathLPosition;
        /// <summary><para>Little-endian UInt32</para><para>LBA location of the optional path table</para><para> The path table pointed to contains only little-endian values</para></summary>
        public uint PathLOptionalPosition;
        /// <summary><para>Big-endian UInt32</para><para>LBA location of the path table</para><para> The path table pointed to contains only big-endian values</para></summary>
        //public uint PathMPosition;
        /// <summary><para>Big-endian UInt32</para><para>LBA location of the optional path table</para><para> The path table pointed to contains only big-endian values</para></summary>
        //public uint PathMOptionalPosition;
        /// <summary>Directory entry for the root directory</summary>
        public FileDescriptor RootDirectory;
        /// <summary><para>strD 128 bytes</para><para>Identifier of the volume set of which this volume is a member</para></summary>
        public string VolumeSetIdentifier;
        /// <summary><para>strA 128 bytes</para><para>The volume publisher</para><para>For extended information, the first byte should be 0x5F, followed by the filename of a file in the root directory. If not specified, all bytes should be 0x20</para></summary>
        public string PublisherIdentifier;
        /// <summary><para>strA 128 bytes</para><para>The identifier of the person(s) who prepared the data for this volume</para><para>For extended information, the first byte should be 0x5F, followed by the filename of a file in the root directory. If not specified, all bytes should be 0x20</para></summary>
        public string DataPreparerIdentifier;
        /// <summary><para>strA 128 bytes</para><para>Identifies how the data are recorded on this volume</para><para>For extended information, the first byte should be 0x5F, followed by the filename of a file in the root directory. If not specified, all bytes should be 0x20</para></summary>
        public string ApplicationIdentifier;
        /// <summary><para>strD 38 bytes</para><para>Filename of a file in the root directory that contains copyright information for this volume set</para><para>If not specified, all bytes should be 0x20</para></summary>
        public string CopyrightFileIdentifier;
        /// <summary><para>strD 36 bytes</para><para>Filename of a file in the root directory that contains abstract information for this volume set</para><para>If not specified, all bytes should be 0x20</para></summary>
        public string AbstractFileIdentifier;
        /// <summary><para>strD 37 bytes</para><para>Filename of a file in the root directory that contains bibliographic information for this volume set</para><para>If not specified, all bytes should be 0x20</para></summary>
        public string BibliographicFileIdentifier;
        /// <summary>The date and time of when the volume was created</summary>
        public DateTime VolumeCreation;
        /// <summary>The date and time of when the volume was modified</summary>
        public DateTime VolumeModification;
        /// <summary><para>The date and time after which this volume is considered to be obsolete</para><para>If not specified, then the volume is never considered to be obsolete</para></summary>
        public DateTime VolumeExpiration;
        /// <summary><para>The date and time after which the volume may be used</para><para>If not specified, the volume may be used immediately</para></summary>
        public DateTime VolumeEffective;
        /// <summary>Always 1</summary>
        //public byte FileStructureVersion;
        //private byte Unused3;
        //512 byte application reserved
        //653 byte ISO reserved
        public PrimaryVolumeDescriptor()
            : base(Type.PrimaryVolumeDescriptor, "CD001", 1)
        {
            this.SystemIdentifier = "PLAYSTATION";
            this.VolumeIdentifier = "";
            this.VolumeSpaceSize = 0;
            this.VolumeSetSize = 1;
            this.VolumeSequenceNumber = 1;
            this.LogicalBlockSize = 2048;
            this.PathTableSize = 0;
            this.PathLPosition = 0;
            this.PathLOptionalPosition = 0;
            this.RootDirectory = new FileDescriptor();
            this.VolumeSetIdentifier = "";
            this.PublisherIdentifier = "";
            this.DataPreparerIdentifier = "";
            this.ApplicationIdentifier = "PLAYSTATION";
            this.CopyrightFileIdentifier = "";
            this.AbstractFileIdentifier = "";
            this.BibliographicFileIdentifier = "";
            this.VolumeCreation = DateTime.MinValue;
            this.VolumeModification = DateTime.MinValue;
            this.VolumeExpiration = DateTime.MinValue;
            this.VolumeEffective = DateTime.MinValue;
        }
        public PrimaryVolumeDescriptor(byte[] sector, Stream iso = null)
            : base(sector)
        {
            if (sector.Length < 883) { throw new ArgumentException("sector is too small", "sector"); }
            if (this.VD_Type != Type.PrimaryVolumeDescriptor || this.VD_Identifier != "CD001" || this.VD_Version != 1) { throw new InvalidDataException("Type is not of a Primary Volume Descriptor"); }
            using (BinaryStream br = new BinaryStream(new MemoryStream(sector, false), System.Text.Encoding.ASCII))
            {
                br.Seek(8, SeekOrigin.Begin);
                this.SystemIdentifier = Utilities.parseADString(br.ReadChars(32));
                this.VolumeIdentifier = Utilities.parseADString(br.ReadChars(32));
                br.Seek(8, SeekOrigin.Current);
                this.VolumeSpaceSize = br.ReadUInt32();
                br.Seek(4 + 32, SeekOrigin.Current);
                this.VolumeSetSize = br.ReadUInt16();
                br.Seek(2, SeekOrigin.Current);
                this.VolumeSequenceNumber = br.ReadUInt16();
                br.Seek(2, SeekOrigin.Current);
                this.LogicalBlockSize = br.ReadUInt16();
                if (this.LogicalBlockSize != ISOFile.sectorSize) { throw new NotSupportedException("Block sizes other than " + ISOFile.sectorSize + " not supported"); }
                br.Seek(2, SeekOrigin.Current);
                this.PathTableSize = br.ReadUInt32();
                br.Seek(4, SeekOrigin.Current);
                this.PathLPosition = br.ReadUInt32();
                this.PathLOptionalPosition = br.ReadUInt32();
                br.Seek(8, SeekOrigin.Current);
                {
                    //Just in-case it has some weird value
                    long pos = br.Tell() + 34;
                    this.RootDirectory = new FileDescriptor(br);
                    br.Seek(pos, SeekOrigin.Begin);
                }
                this.VolumeSetIdentifier = Utilities.parseADString(br.ReadChars(128));
                this.PublisherIdentifier = Utilities.parseADString(br.ReadChars(128));
                this.DataPreparerIdentifier = Utilities.parseADString(br.ReadChars(128));
                this.ApplicationIdentifier = Utilities.parseADString(br.ReadChars(128));
                this.CopyrightFileIdentifier = Utilities.parseADString(br.ReadChars(38));
                this.AbstractFileIdentifier = Utilities.parseADString(br.ReadChars(36));
                this.BibliographicFileIdentifier = Utilities.parseADString(br.ReadChars(37));
                this.VolumeCreation = Utilities.DateTimeFromVolumeTime(sector, br.Tell());
                br.Seek(17, SeekOrigin.Current);
                this.VolumeModification = Utilities.DateTimeFromVolumeTime(sector, br.Tell());
                br.Seek(17, SeekOrigin.Current);
                this.VolumeExpiration = Utilities.DateTimeFromVolumeTime(sector, br.Tell());
                br.Seek(17, SeekOrigin.Current);
                this.VolumeEffective = Utilities.DateTimeFromVolumeTime(sector, br.Tell());
                Debug.WriteLine(String.Format("----- PrimaryVolumeDescriptor Begin -----\r\nSystemIdentifier: {0}\r\nVolumeIdentifier: {1}\r\nVolumeSpaceSize: {2}\r\nVolumeSetSize: {3}\r\nVolumeSequenceNumber: {4}\r\nLogicalBlockSize: {5}\r\nPathTableSize: {6}\r\nPathLPosition: {7}\r\nPathLOptionalPosition: {8}\r\nVolumeSetIdentifier: {9}\r\nPublisherIdentifier: {10}\r\nDataPreparerIdentifier: {11}\r\nApplicationIdentifier: {12}\r\nCopyrightFileIdentifier: {13}\r\nAbstractFileIdentifier: {14}\r\nBibliographicFileIdentifier: {15}\r\nVolumeCreation: {16:O}\r\nVolumeModification: {17:O}\r\nVolumeExpiration: {18:O}\r\nVolumeEffective: {19:O}\r\n----- PrimaryVolumeDescriptor End -----", this.SystemIdentifier, this.VolumeIdentifier, this.VolumeSpaceSize, this.VolumeSetSize, this.VolumeSequenceNumber, this.LogicalBlockSize, this.PathTableSize, this.PathLPosition, this.PathLOptionalPosition, this.VolumeSetIdentifier, this.PublisherIdentifier, this.DataPreparerIdentifier, this.ApplicationIdentifier, this.CopyrightFileIdentifier, this.AbstractFileIdentifier, this.BibliographicFileIdentifier, this.VolumeCreation, this.VolumeModification, this.VolumeExpiration, this.VolumeEffective));
                if (iso != null) { this.RootDirectory.ParseSubFiles(iso, true); }
            }
        }
        /// <summary>Find a file by path</summary>
        /// <param name="path">Path to the file</param>
        /// <returns>FileDescriptor describing the file</returns>
        /// <exception cref="FileNotFoundException">The specified file was not found or there was a problem traversing the path</exception>
        public FileDescriptor FindFile(string path)
        {
            string[] parts = path.ToUpperInvariant().Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            FileDescriptor current = this.RootDirectory;
            for (int i = 0; i < parts.Length; ++i)
            {
                if (parts[i] == ".") { continue; }
                else if (parts[i] == "..") { current = current.parent; }
                else { current = current.GetChild(parts[i]); }
                if (!current.IsDirectory && i + 1 != parts.Length) { throw new FileNotFoundException("File is not a directory"); }
            }
            if (current.IsDirectory) { throw new FileNotFoundException("Target file is a directory"); }
            return current;
        }
        private static IEnumerator<FileDescriptor> GetEnumerator(FileDescriptor parent)
        {
            foreach (FileDescriptor file in parent)
            {
                if (file.IsSpecial) { continue; }
                if (file.IsDirectory)
                {
                    //Dafaq is this? Allows recursion of multiple directories.
                    using (IEnumerator<FileDescriptor> e = GetEnumerator(file))
                    {
                        while (e.MoveNext()) { yield return e.Current; }
                    }
                }
                else
                {
                    yield return file;
                }
            }
        }
        /// <summary>Loop through all non-directory files</summary>
        public IEnumerator<FileDescriptor> GetEnumerator() { return GetEnumerator(this.RootDirectory); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(this.RootDirectory); }
        /// <summary><para>Returns a shallow clone of the current object.</para><para><c>RootDirectory</c> is null in the clone.</para></summary>
        /// <returns>Shallow clone</returns>
        public PrimaryVolumeDescriptor Clone()
        {
            PrimaryVolumeDescriptor t = this.MemberwiseClone() as PrimaryVolumeDescriptor;
            t.RootDirectory = null;
            return t;
        }
    }
    internal abstract class VolumeDesciptor
    {
        public enum Type : byte
        {
            BootRecord = 0,
            PrimaryVolumeDescriptor = 1,
            SupplementaryVolumeDescriptor = 2,
            VolumePartitionDescriptor = 3,
            VolumeDescriptorSetTerminator = 255
        }
        public readonly Type VD_Type;
        public readonly string VD_Identifier;
        public readonly byte VD_Version;
        public VolumeDesciptor() : this(Type.VolumeDescriptorSetTerminator, "", 1) { }
        public VolumeDesciptor(Type t, string i, byte v)
        {
            this.VD_Type = t;
            this.VD_Identifier = i;
            this.VD_Version = v;
        }
        public VolumeDesciptor(byte[] b)
        {
            if (b.Length < 7) { throw new ArgumentException("sector is too small", "sector"); }
            this.VD_Type = (Type)b[0];
            this.VD_Identifier = System.Text.Encoding.ASCII.GetString(b, 1, 5);
            this.VD_Version = b[6];
        }

        /// <summary>Checks if the specified array is a valid ISO Volume Descriptor</summary>
        /// <param name="buffer">An array of bytes to check</param>
        /// <returns>True if it is valid, false otherwise.</returns>
        public static bool isValid(byte[] buffer, out string identifier)
        {
            identifier = "";
            for (int i = 1; i < 6; ++i)
            {
                char b = (char)buffer[i];
                if (!Utilities.isDChar(b)) { return false; }
                identifier += b;
            }
            return true;
        }
    }
    internal abstract class ISOFile : IDisposable
    {
        /// <summary><para>Sector size of the ISO</para><para>Almost always 2048 bytes</para></summary>
        public const int sectorSize = 2048;
        
        /// <summary>ISO file stream</summary>
        public Stream file { get; private set; }
        /// <summary>Primary Volume Descriptor of the ISO</summary>
        protected PrimaryVolumeDescriptor pvd = null;
        /// <summary>Clone of the ISO's Primary Volume Descriptor</summary>
        public PrimaryVolumeDescriptor PrimaryVolumeDescriptor { get { return this.pvd.Clone(); } }
        public long pvdSector { get; protected set; }
        public ISOFile(Stream file)
        {
            this.file = file;
        }
        public void Dispose() { this.Dispose(true); }
        public virtual void Dispose(bool disposing)
        {
            if (this.file != null) { this.file.Dispose(); this.file = null; }
            if (disposing)
            {
                if (this.pvd != null) { this.pvd = null; }
            }
        }
        /// <summary>Returns the current filesize of the stream</summary>
        public long FileSize { get { return this.file.Length; } }
        /// <summary>Returns the current number of sectors in the stream</summary>
        public uint SectorCount { get { return (uint)(this.file.Length / sectorSize); } }
        /// <summary>Current sector position</summary>
        public uint SectorPosition { get { return (uint)(this.file.Position / sectorSize); } }

        protected void EnsureBoundary()
        {
            if ((this.file.Position % sectorSize) != 0)
            {
                int rem = sectorSize - (int)(this.file.Position % sectorSize);
                byte[] buf = new byte[4];
                while (rem > 3) { this.file.Write(buf, 0, 4); rem -= 4; }
                this.file.Write(buf, 0, rem);
            }
        }

        /// <summary>Read the specified number of bytes and write them to a destination stream</summary>
        /// <param name="destination">The stream that will contain the specified data</param>
        /// <param name="length">Maximum length of data to copy</param>
        public void CopyTo(Stream destination, uint length)
        {
            int num;
            byte[] buffer = new byte[sectorSize * 2];
            while (length > 0 && (num = this.file.Read(buffer, 0, (int)Math.Min(sectorSize * 2, length))) != 0)
            {
                destination.Write(buffer, 0, num);
                length -= (uint)num;
            }
        }
        /// <summary>Read a sector from the stream</summary>
        /// <param name="buffer">An array of bytes to read into</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read</param>
        /// <param name="sector">A sector to <c>Seek</c> to before reading</param>
        /// <returns>True if a full sector was read, false otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown when buffer is too small to hold a sector</exception>
        internal bool ReadSector(byte[] buffer, int offset = 0, int sector = -1)
        {
            if (buffer.Length < offset + sectorSize) { throw new ArgumentException("Buffer is too small to hold a sector", "buffer"); }
            if (sector != -1) { this.Seek(sector); }
            return this.file.Read(buffer, offset, sectorSize) == sectorSize;
        }
        /// <summary>Sets the position within the stream</summary>
        /// <param name="sector">Sector index to seek to</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>sector</c> is beyond the stream's range</exception>
        public void Seek(long sector)
        {
            if (sector < 0 || sector > this.SectorCount) { throw new ArgumentOutOfRangeException("sector"); }
            this.file.Position = sector * sectorSize;
        }
        /// <summary>Write a sector to the stream</summary>
        /// <param name="buffer">An array of bytes to write</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin writing the data</param>
        /// <param name="sector">A sector to <c>Seek</c> to before reading</param>
        /// <exception cref="ArgumentException">Thrown when buffer is too small to hold a sector</exception>
        protected void WriteSector(byte[] buffer, int offset = 0, int sector = -1)
        {
            if (buffer.Length < offset + sectorSize) { throw new ArgumentException("Buffer is too small to hold a sector", "buffer"); }
            if (sector != -1) { this.Seek(sector); }
            this.file.Write(buffer, offset, sectorSize);
        }

        /// <summary>Find a file by name</summary>
        /// <param name="path">Path to the file</param>
        /// <returns>FileDescriptor describing the file</returns>
        /// <exception cref="FileNotFoundException">The specified file was not found or there was a problem traversing the path</exception>
        public FileDescriptor FindFile(string path) { return pvd.FindFile(path); }
    }
    internal class ISOFileReader : ISOFile, IEnumerable<FileDescriptor>
    {
        public bool IsUDF { get; private set; }
        public int BEASector { get; private set; }
        public List<int> NSRSector = new List<int>();
        public int TEASector { get; private set; }
        public ISOFileReader(Stream file)
            : base(file) 
        {
            if (file.Length < 17 * sectorSize) { throw new InvalidDataException("ISO too small to be valid"); }
            this.IsUDF = false;
            int i = 15;
            int BNT = 0;
            byte[] sector = new byte[sectorSize];
            do
            {
                string type;
                if (!this.ReadSector(sector, 0, ++i) || !VolumeDesciptor.isValid(sector, out type)) { break; }
                Debug.WriteLine("Volume Structure Descriptor type: " + type);
                if (pvd == null && type == "CD001" && sector[0] == 1)
                {
                    this.pvdSector = i;
                    this.pvd = new PrimaryVolumeDescriptor(sector, file);
                }
                else if (BNT < 3)
                {
                    if (BNT == 0 && type == "BEA01") { ++BNT; this.BEASector = i; Debug.WriteLine("Found UDF descriptors starting at sector " + i); }
                    else if (BNT == 1 && (type == "NSR02" || type == "NSR03")) { this.IsUDF = true; this.NSRSector.Add(this.TEASector = i); }
                    else if (BNT == 1 && type == "TEA01") { ++BNT; this.TEASector = i; Debug.WriteLine("Found UDF descriptors ending at sector " + i); }
                }
            } while (true);
            if (pvd == null) { throw new InvalidDataException("Failed to find root directory on ISO"); }
            Debug.WriteLine("Successfully opened ISO image");
        }
        /// <summary>Copy specified file to target stream</summary>
        /// <param name="file">FileDescriptor describing the file to copy</param>
        /// <param name="target">The stream that will contain the specified data</param>
        public void CopyFile(FileDescriptor file, Stream target)
        {
            this.Seek(file.ExtentLBA);
            this.CopyTo(target, file.ExtentLength);
        }
        /// <summary>Returns a <c>Substream</c> of the file</summary>
        /// <param name="file">FileDescriptor describing the file</param>
        /// <param name="includeSectorBuffer">Include the buffer after the file.</param>
        /// <returns><c>Substream</c> containing only the requested file</returns>
        public Substream GetFileStream(FileDescriptor file, bool includeSectorBuffer = false)
        {
            return new Substream(this.file, (long)file.ExtentLBA * sectorSize, includeSectorBuffer == false ? file.ExtentLength : ((long)Math.Ceiling((double)file.ExtentLength / sectorSize) * sectorSize));
        }
        /// <summary>Enumerate over files in the ISO</summary>
        public IEnumerator<FileDescriptor> GetEnumerator() { return pvd.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
    internal class ISOCopyWriter : ISOFile
    {
        public ISOFileReader source { get; private set; }
        private bool final = false;

        public ISOCopyWriter(Stream file, ISOFileReader source)
            : base(file) 
        {
            this.source = source;
            file.Position = 0;
            //Find lowest first LBA
            uint LBA = uint.MaxValue;
            foreach (FileDescriptor f in source) { if (f.ExtentLBA < LBA) { LBA = f.ExtentLBA; } }
            if (LBA == uint.MaxValue) { throw new InvalidDataException("No files found"); }
            //Copy first sectors, including the "encrypted" bootscreen
            source.Seek(0);
            source.CopyTo(file, LBA * sectorSize);
            //I don't have the code to update UDF, nor do I really want to write that... so I'll just destroy the NSR descriptor.
            //I'll move everything up too, and not just destroy the whole block. Gonna be nice here. ;)
            if (source.IsUDF)
            {
                byte[] sector = new byte[sectorSize];
                for (int i = source.BEASector + 1, j = i; i <= source.TEASector; ++i, ++j)
                {
                    while (source.NSRSector.Contains(j)) { ++j; }
                    if (i != j)
                    {
                        if (i < source.TEASector)
                        {
                            Debug.WriteLine("Move sector " + j + " to " + i);
                            this.ReadSector(sector, 0, j);
                            this.WriteSector(sector, 0, i);
                        }
                        else
                        {
                            if (i == source.TEASector) { Array.Clear(sector, 0, sectorSize); }
                            Debug.WriteLine("Clear sector " + source.TEASector);
                            this.WriteSector(sector, 0, i);
                        }
                    }
                }
            }
        }
        private void mFinalize()
        {
            long pvdo = this.source.pvdSector * sectorSize;
            this.file.Position = pvdo + 80;
            //VolumeSpaceSize
            this.file.Write(Utilities.BEInt32(this.SectorCount), 0, 8);
            //VolumeModification
            this.file.Position = pvdo + 830;
            this.file.Write(Utilities.VolumeTimeFromDateTime(DateTime.UtcNow), 0, 17);
            this.final = true;
        }
        public void AddFile(FileDescriptor f, Stream data)
        {
            this.SeekEnd();
            this.EnsureBoundary();
            f.ExtentLBA = this.SectorPosition;
            f.ExtentLength = (uint)data.Length;
            data.CopyTo(this.file);
            this.EnsureBoundary();
            this.PatchFile(f);
        }
        public void CopyFile(FileDescriptor f) { using (Stream data = this.source.GetFileStream(f)) { this.AddFile(f, data); } }
        public void PatchFile(FileDescriptor f)
        {
            this.file.Position = f.RawOffset + 2;
            this.file.Write(Utilities.BEInt32(f.ExtentLBA), 0, 8);
            this.file.Write(Utilities.BEInt32(f.ExtentLength), 0, 8);
            this.file.Write(Utilities.DirectoryTimeFromDateTime(f.RecordingDate), 0, 7);
        }
        public void SeekEnd()
        {
            this.file.Seek(0, SeekOrigin.End);
            this.EnsureBoundary();
        }
        public override void Dispose(bool disposing)
        {
            if (!this.final) { this.mFinalize(); }
            if (this.source != null) { this.source.Dispose(); this.source = null; }
            base.Dispose(disposing);
        }
    }
}