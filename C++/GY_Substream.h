#pragma once

// Ver 1.0.1

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;

namespace GovanifY
{
	namespace Utility
	{
		class Substream : public Stream, IDisposable
		{
		private:
			const bool leaveOpen;
			const long long length;
			const long long origin;
			Stream *baseStream;
			long long position;

			/// <summary>
			///     <para>Initializes a new instance of the <c>Substream</c> class encompassing the whole of <c>baseStream</c>.</para>
			///     <para>When the <c>Substream</c> is closed, <c>baseStream</c> will remain open.</para>
			/// </summary>
			/// <param name="baseStream">The <c>Stream</c> from which to create this stream.</param>
			public:
			~Substream()
			{
				this->Dispose(true);
			}

//C# TO C++ CONVERTER WARNING: Unlike C#, there is no automatic call to this finalizer method in native C++:
			private:
			void Finalize()
			{
				this->Dispose(false);
			}

		public:
			Substream(Stream *baseStream);

			/// <summary>Initializes a new instance of the <c>Substream</c> class encompassing the specified range.</summary>
			/// <param name="baseStream">The <c>Stream</c> from which to create this stream.</param>
			/// <param name="origin">A byte offset relative to the beginning of <c>baseStream</c>.</param>
			/// <param name="length">
			///     <para>The maximum number of bytes to encompass.</para>
			///     <para>
			///         If <c>origin</c>+<c>length</c> is more then <c>baseStream</c>'s length, <c>length</c> will be adjusted to
			///         fit.
			///     </para>
			/// </param>
			/// <param name="leaveOpen">
			///     true to leave <c>baseStream</c> open after the <c>Substream</c> object is disposed; otherwise,
			///     false.
			/// </param>
			Substream(Stream *baseStream, long long origin, long long length, bool leaveOpen = true);

			/// <summary>Gets a value indicating whether the current stream is open and valid.</summary>
			const bool &getIsOpen() const;

			/// <summary>Gets a value indicating whether the current stream supports reading.</summary>
			const bool &getCanRead() const;

			/// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
			const bool &getCanSeek() const;

			/// <summary>Gets a value indicating whether the current stream supports writing.</summary>
			const bool &getCanWrite() const;

			/// <summary>Gets the length in bytes of the stream.</summary>
			const long long &getLength() const;

			/// <summary>Gets or sets the position within the current stream.</summary>
			const long long &getPosition() const;
			void setPosition(const long long &value);

			/// <summary>Closes the current stream and optionally releases the base stream.</summary>
			void Close();

			/// <summary>Closes the current stream and optionally releases the base stream.</summary>
		private:
			void Dispose(bool disposing);

			/// <summary>Passes a flush command to the underlying stream.</summary>
		public:
			void Flush();

			/// <summary>
			///     Reads a sequence of bytes from the current stream and advances the position within the stream by the number of
			///     bytes read.
			/// </summary>
			/// <param name="buffer">
			///     An array of bytes. When this method returns, the buffer contains the specified byte array with the
			///     values between <c>offset</c> and (<c>offset</c> + <c>count</c> - 1) replaced by the bytes read from the current
			///     source.
			/// </param>
			/// <param name="offset">
			///     The zero-based byte offset in <c>buffer</c> at which to begin storing the data read from the
			///     current stream.
			/// </param>
			/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
			/// <returns>
			///     The total number of bytes read into the buffer. This can be less than the number of bytes requested if that
			///     many bytes are not currently available, or zero (0) if the end of the stream has been reached.
			/// </returns>
			int Read(unsigned char buffer[], int offset, int count);

			/// <summary>
			///     Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the
			///     end of the stream.
			/// </summary>
			/// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
			int ReadByte();

			/// <summary>Sets the position within the current stream.</summary>
			/// <param name="offset">A byte offset relative to the <c>origin</c> parameter.</param>
			/// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
			/// <returns>The new position within the current stream.</returns>
			long long Seek(long long offset, SeekOrigin origin);

			void SetLength(long long value);

			/// <summary>
			///     Writes a sequence of bytes to the current stream and advances the current position within this stream by the
			///     number of bytes written.
			/// </summary>
			/// <param name="buffer">An array of bytes. This method copies <c>count</c> bytes from <c>buffer</c> to the current stream.</param>
			/// <param name="offset">The zero-based byte offset in <c>buffer</c> at which to begin copying bytes to the current stream.</param>
			/// <param name="count">The number of bytes to be written to the current stream.</param>
			void Write(unsigned char buffer[], int offset, int count);

			/// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
			/// <param name="value">The byte to write to the stream.</param>
			void WriteByte(unsigned char value);
		};
	}
}
