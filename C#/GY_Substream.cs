// Ver 1.0.2

using System;
using System.IO;

namespace GovanifY.Utility
{
    public sealed class Substream : Stream, IDisposable
    {
        private readonly bool leaveOpen;
        private readonly long length;
        private readonly long origin;
        private Stream baseStream;
        private long position;

        /// <summary>
        ///     <para>Initializes a new instance of the <c>Substream</c> class encompassing the whole of <c>baseStream</c>.</para>
        ///     <para>When the <c>Substream</c> is closed, <c>baseStream</c> will remain open.</para>
        /// </summary>
        /// <param name="baseStream">The <c>Stream</c> from which to create this stream.</param>
        public Substream(Stream baseStream)
        {
            this.baseStream = baseStream;
            origin = 0;
            if (baseStream.Length <= 0)
            {
                throw new ArgumentException("baseStream.Length <= 0", "baseStream");
            }
            length = baseStream.Length;
            leaveOpen = true;
        }

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
        public Substream(Stream baseStream, long origin, long length, bool leaveOpen = true)
        {
            this.baseStream = baseStream;
            this.origin = origin;
            if (origin + length > baseStream.Length)
            {
                length += (int) (baseStream.Length - (origin + length));
            }
            if (length <= 0)
            {
                throw new ArgumentException("adjusted(length) <= 0", "length");
            }
            this.length = length;
            this.leaveOpen = leaveOpen;
        }

        /// <summary>Gets a value indicating whether the current stream is open and valid.</summary>
        public bool IsOpen
        {
            get { return baseStream != null; }
        }

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        public override bool CanRead
        {
            get { return baseStream == null ? false : baseStream.CanRead; }
        }

        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        public override bool CanSeek
        {
            get { return baseStream == null ? false : baseStream.CanSeek; }
        }

        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite
        {
            get { return baseStream == null ? false : baseStream.CanWrite; }
        }

        /// <summary>Gets the length in bytes of the stream.</summary>
        public override long Length
        {
            get
            {
                if (baseStream == null)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return length;
            }
        }

        /// <summary>Gets or sets the position within the current stream.</summary>
        public override long Position
        {
            get
            {
                if (baseStream == null)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return position;
            }
            set
            {
                if (baseStream == null)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                position = value;
            }
        }

        /// <summary>Closes the current stream and optionally releases the base stream.</summary>
        public override void Close()
        {
            Dispose(true);
        }

        /// <summary>Closes the current stream and optionally releases the base stream.</summary>
        protected override void Dispose(bool disposing)
        {
            if (baseStream != null)
            {
                if (!leaveOpen)
                {
                    baseStream.Close();
                }
                baseStream = null;
            }
        }

        /// <summary>Passes a flush command to the underlying stream.</summary>
        public override void Flush()
        {
            baseStream.Flush();
        }

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
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (baseStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (position + count > length)
            {
                count += (int) (length - (position + count));
            }
            if (count <= 0)
            {
                return 0;
            }
            if (baseStream.Position != origin + position)
            {
                baseStream.Position = origin + position;
            }
            int read = baseStream.Read(buffer, offset, count);
            position += read;
            return read;
        }

        /// <summary>
        ///     Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the
        ///     end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            if (baseStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (position >= length)
            {
                return -1;
            }
            if (baseStream.Position != origin + position)
            {
                baseStream.Position = origin + position;
            }
            int read = baseStream.ReadByte();
            position++;
            return read;
        }

        /// <summary>Sets the position within the current stream.</summary>
        /// <param name="offset">A byte offset relative to the <c>origin</c> parameter.</param>
        /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (baseStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = length - offset;
                    break;
            }
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Writes a sequence of bytes to the current stream and advances the current position within this stream by the
        ///     number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <c>count</c> bytes from <c>buffer</c> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <c>buffer</c> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (baseStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (position + count > length)
            {
                count += (int) (length - (position + count));
            }
            if (count <= 0)
            {
                return;
            }
            if (baseStream.Position != origin + position)
            {
                baseStream.Position = origin + position;
            }
            baseStream.Write(buffer, offset, count);
            position += count;
        }

        /// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            if (baseStream == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (position >= length)
            {
                return;
            }
            if (baseStream.Position != origin + position)
            {
                baseStream.Position = origin + position;
            }
            baseStream.WriteByte(value);
            position++;
        }
    }
}