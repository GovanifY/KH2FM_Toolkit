#pragma once

#include <string>
#include <cmath>

// Ver 1.3.0

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Text;

namespace GovanifY
{
	namespace Utility
	{
		class BinaryStream
		{
			// Must be at least 8, for UInt64.
		private:
			static const int _bufferLength = 8;
			static const int _dynaBufferLength = 4096;
			const bool _leaveOpen;

			/// <summary>true to use little endian when reading, false to use big endian.</summary>
		public:
			bool IsLittleEndian;

		private:
			unsigned char _buffer[_bufferLength];
			Decoder *_cDecoderV;
			Encoding *_cEncoding;
			Stream *_stream;

			/// <summary>
			///     Initializes a new instance of the <c>BinaryStream</c> class based on the supplied stream and a UTF-16
			///     character encoding.
			/// </summary>
			/// <param name="stream">The supplied stream.</param>
			/// <param name="littleEndian">true to read multi-byte numbers as little endian; false for big endian.</param>
			/// <param name="leaveOpen">
			///     true to leave the stream open after the <c>BinaryStream</c> object is disposed; otherwise,
			///     false.
			/// </param>
			/// <exception cref="System.ArgumentException">
			///     The stream does not support reading or writing, or the stream is already
			///     closed.
			/// </exception>
			/// <exception cref="System.ArgumentNullException"><c>stream</c> is null.</exception>
		public:
//C# TO C++ CONVERTER TODO TASK: Calls to same-class constructors are not supported in C++ prior to C++0x:
//ORIGINAL LINE: public BinaryStream(Stream stream, bool littleEndian = true, bool leaveOpen = false) : this(stream, Encoding.Unicode, littleEndian, leaveOpen)
			BinaryStream(Stream *stream, bool littleEndian = true, bool leaveOpen = false);

			/// <summary>Initializes a new instance of the <c>BinaryStream</c> class based on the supplied stream.</summary>
			/// <param name="stream">The supplied stream.</param>
			/// <param name="encoding">The character encoding to use.</param>
			/// <param name="littleEndian">true to read multi-byte numbers as little endian; false for big endian.</param>
			/// <param name="leaveOpen">
			///     true to leave the stream open after the <c>BinaryStream</c> object is disposed; otherwise,
			///     false.
			/// </param>
			/// <exception cref="System.ArgumentException">
			///     The stream does not support reading or writing, or the stream is already
			///     closed.
			/// </exception>
			/// <exception cref="System.ArgumentNullException"><c>stream</c> or <c>encoding</c> is null.</exception>
			BinaryStream(Stream *stream, Encoding *encoding, bool littleEndian = true, bool leaveOpen = false);

		private:
			const Decoder &get_cDecoder() const;

			/// <summary>The character encoding to use.</summary>
		public:
			const Encoding &getTextEncoding() const;
			void setTextEncoding(const Encoding &value);

			/// <summary>Exposes access to the underlying stream of the <c>BinaryStream</c>.</summary>
			const Stream &getBaseStream() const;

			/// <summary>true if the stream is open, false if it has been disposed.</summary>
			const bool &getIsOpen() const;

			/// <summary>true if the stream supports reading, false if not.</summary>
			const bool &getCanRead() const;

			/// <summary>true if the stream supports writing, false if not.</summary>
			const bool &getCanWrite() const;

			/// <summary>Releases the managed resources.</summary>
			~BinaryStream();

			/// <summary>Closes the current <c>BinaryStream</c> and the underlying stream.</summary>
			void Close();

		private:
			void SwapBufferBytes(int length);

			/// <summary>Sets the position within the stream.</summary>
			/// <param name="offset">A byte offset relative to <c>origin</c>.</param>
			/// <param name="origin">A value indicating the reference point from which the new position is to be obtained.</param>
			/// <returns>The position with the current stream.</returns>
		public:
			long long Seek(long long offset, SeekOrigin origin);

			/// <summary>Returns the current position within the stream.</summary>
			/// <returns>The position with the current stream.</returns>
			long long Tell();


		private:
			void FillBuffer(int count);

			int IntReadChars(wchar_t buffer[], int index, int count, Encoding *encode, Decoder *decoder);

			/// <summary>Reads the specified number of bytes from the stream, starting from a specified point in the byte array.</summary>
			/// <param name="buffer">The buffer to read data into.</param>
			/// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
			/// <param name="count">The number of bytes to read.</param>
			/// <returns>
			///     The number of bytes read into <c>buffer</c>. This might be less than the number of bytes requested if that
			///     many bytes are not available, or it might be zero if the end of the stream is reached.
			/// </returns>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
		public:
			int Read(unsigned char buffer[], int index, int count);

			/// <summary>Reads a Boolean value from the current stream and advances the current position of the stream by one byte.</summary>
			/// <returns>true if the byte is nonzero; otherwise, false.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			bool ReadBoolean();

			/// <summary>Reads the next byte from the current stream and advances the current position of the stream by one byte.</summary>
			/// <returns>The next byte read from the current stream.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			unsigned char ReadByte();

			/// <summary>
			///     Reads the specified number of bytes from the current stream into a byte array and advances the current
			///     position by that number of bytes.
			/// </summary>
			/// <param name="count">
			///     The number of bytes to read. This value must be 0 or a non-negative number or an exception will
			///     occur.
			/// </param>
			/// <returns>
			///     A byte array containing data read from the underlying stream. This might be less than the number of bytes
			///     requested if the end of the stream is reached.
			/// </returns>
			/// <exception cref="System.ArgumentOutOfRangeException"><c>count</c> is negative.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			unsigned char *ReadBytes(int count);

			/// <summary>
			///     Reads the specified number of characters from the current stream, returns the data in a character array, and
			///     advances the current position in accordance with the Encoding and the specific characters being read from the
			///     stream.
			/// </summary>
			/// <param name="count">The number of characters to read.</param>
			/// <returns>
			///     A character array containing data read from the underlying stream. This might be less than the number of
			///     characters requested if the end of the stream is reached.
			/// </returns>
			/// <exception cref="System.ArgumentOutOfRangeException"><c>count</c> is negative.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			wchar_t *ReadChars(int count);

			/// <summary>Reads a NULL-terminated string from the current stream.</summary>
			/// <returns>The string being read without the terminating NULL.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			std::string ReadCString();

			/// <summary>
			///     Reads a 2-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the
			///     position of the stream by two bytes.
			/// </summary>
			/// <returns>A 2-byte unsigned integer read from this stream.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			unsigned short ReadUInt16();

			/// <summary>
			///     Reads a 4-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the
			///     position of the stream by four bytes.
			/// </summary>
			/// <returns>A 4-byte unsigned integer read from this stream.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			unsigned int ReadUInt32();

			/// <summary>
			///     Reads a 8-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the
			///     position of the stream by eight bytes.
			/// </summary>
			/// <returns>A 8-byte unsigned integer read from this stream.</returns>
			/// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			unsigned long long ReadUInt64();

			short ReadInt16();

			int ReadInt32();

			long long ReadInt64();

			/// <summary>Copies this stream to another stream.</summary>
			/// <param name="v">A <c>Stream</c> to write to.</param>
			/// <exception cref="System.ArgumentNullException"><c>v</c> is null.</exception>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">
			///     The stream does not support reading or <c>v</c> does not support
			///     writing.
			/// </exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void CopyTo(Stream *v);



			/// <summary>Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.</summary>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Flush();

			/// <summary>
			///     Writes a string to this stream in the Encoding, and advances the current position of the stream in accordance
			///     with the encoding used and the specific characters being written to the stream.
			/// </summary>
			/// <param name="v">The value to write.</param>
			/// <exception cref="IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(wchar_t v[]);

			/// <summary>Writes a region of a byte array to the current stream.</summary>
			/// <param name="buffer">A byte array containing the data to write.</param>
			/// <param name="index">The starting point in <c>buffer</c> at which to begin writing.</param>
			/// <param name="count">The number of bytes to write.</param>
			/// <exception cref="System.ArgumentException">The buffer length minus <c>index</c> is less than <c>count</c>.</exception>
			/// <exception cref="System.ArgumentNullException"><c>buffer</c> is null.</exception>
			/// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> or <c>count</c> is negative.</exception>
			/// <exception cref="IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned char buffer[], int index, int count);

			/// <summary>Writes a one-byte Boolean value to the current stream, with 0 representing false and 1 representing true.</summary>
			/// <param name="v">The Boolean value to write.</param>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(bool v);

			/// <summary>Writes an unsigned byte to the current stream and advances the stream position by one byte.</summary>
			/// <param name="v">The unsigned byte to write.</param>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned char v);

			/// <summary>Writes a byte array to the underlying stream.</summary>
			/// <param name="buffer">A byte array containing the data to write.</param>
			/// <exception cref="System.ArgumentNullException"><c>buffer</c> is null.</exception>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned char buffer[]);

			/// <summary>
			///     Writes a two-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the
			///     stream position by two bytes.
			/// </summary>
			/// <param name="v">The two-byte unsigned integer to write.</param>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned short v);

			/// <summary>
			///     Writes a four-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the
			///     stream position by four bytes.
			/// </summary>
			/// <param name="v">The four-byte unsigned integer to write.</param>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned int v);

			/// <summary>
			///     Writes an eight-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the
			///     stream position by eight bytes.
			/// </summary>
			/// <param name="v">The eight-byte unsigned integer to write.</param>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(unsigned long long v);

			void Write(short v);

			void Write(int v);

			void Write(long long v);

			/// <summary>Writes a <c>Stream</c> to the underlying stream.</summary>
			/// <param name="v">A <c>Stream</c> to write.</param>
			/// <exception cref="System.ArgumentNullException"><c>v</c> is null.</exception>
			/// <exception cref="System.IOException">An I/O error occurs.</exception>
			/// <exception cref="System.NotSupportedException">
			///     The stream does not support writing or <c>v</c> does not support
			///     reading.
			/// </exception>
			/// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
			void Write(Stream *v);

		};
	}
}
