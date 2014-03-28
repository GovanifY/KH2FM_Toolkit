﻿// Ver 1.3.0
namespace GovanifY.Utility
{
    public sealed class BinaryStream : System.IDisposable
    {
        private System.IO.Stream _stream;
        private readonly bool _leaveOpen;
        private System.Text.Encoding _cEncoding;
        private System.Text.Decoder _cDecoderV;
        private System.Text.Decoder _cDecoder { get { if (this._cDecoderV == null) { this._cDecoderV = this._cEncoding.GetDecoder(); } return this._cDecoderV; } }
        // Must be at least 8, for UInt64.
        private const int _bufferLength = 8;
        private const int _dynaBufferLength = 4096;
        private byte[] _buffer = new byte[_bufferLength];

        /// <summary>true to use little endian when reading, false to use big endian.</summary>
        public bool IsLittleEndian;
        /// <summary>The character encoding to use.</summary>
        public System.Text.Encoding TextEncoding
        {
            get { return this._cEncoding; }
            set
            {
                if (value == null) { throw new System.ArgumentNullException(); }
                this._cEncoding = value;
                this._cDecoderV = null;
            }
        }

        /// <summary>Exposes access to the underlying stream of the <c>BinaryStream</c>.</summary>
        public System.IO.Stream BaseStream { get { if (this._stream.CanWrite) { this._stream.Flush(); } return this._stream; } }
        /// <summary>true if the stream is open, false if it has been disposed.</summary>
        public bool IsOpen { get { return this._stream != null; } }
        /// <summary>true if the stream supports reading, false if not.</summary>
        public bool CanRead { get { return this._stream != null ? this._stream.CanRead : false; } }
        /// <summary>true if the stream supports writing, false if not.</summary>
        public bool CanWrite { get { return this._stream != null ? this._stream.CanWrite : false; } }

        /// <summary>Initializes a new instance of the <c>BinaryStream</c> class based on the supplied stream and a UTF-16 character encoding.</summary>
        /// <param name="stream">The supplied stream.</param>
        /// <param name="littleEndian">true to read multi-byte numbers as little endian; false for big endian.</param>
        /// <param name="leaveOpen">true to leave the stream open after the <c>BinaryStream</c> object is disposed; otherwise, false.</param>
        /// <exception cref="System.ArgumentException">The stream does not support reading or writing, or the stream is already closed.</exception>
        /// <exception cref="System.ArgumentNullException"><c>stream</c> is null.</exception>
        public BinaryStream(System.IO.Stream stream, bool littleEndian = true, bool leaveOpen = false) : this(stream, System.Text.Encoding.Unicode, littleEndian, leaveOpen) { }
        /// <summary>Initializes a new instance of the <c>BinaryStream</c> class based on the supplied stream.</summary>
        /// <param name="stream">The supplied stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="littleEndian">true to read multi-byte numbers as little endian; false for big endian.</param>
        /// <param name="leaveOpen">true to leave the stream open after the <c>BinaryStream</c> object is disposed; otherwise, false.</param>
        /// <exception cref="System.ArgumentException">The stream does not support reading or writing, or the stream is already closed.</exception>
        /// <exception cref="System.ArgumentNullException"><c>stream</c> or <c>encoding</c> is null.</exception>
        /// 
        public BinaryStream(System.IO.Stream stream, System.Text.Encoding encoding, bool littleEndian = true, bool leaveOpen = false)
        {
            if (stream == null) { throw new System.ArgumentNullException("stream"); }
            if (encoding == null) { throw new System.ArgumentNullException("encoding"); }
            if (!stream.CanRead && !stream.CanWrite) { throw new System.ArgumentException("Cannot read or write from stream.", "stream"); }
            this._stream = stream;
            this._cEncoding = encoding;
            this.IsLittleEndian = littleEndian;
            this._leaveOpen = leaveOpen;
        }
        /// <summary>Closes the current <c>BinaryStream</c> and the underlying stream.</summary>
        public void Close() { this.Dispose(); }
        /// <summary>Releases the managed resources.</summary>
        public void Dispose()
        {
            if (this._stream != null) { if (!this._leaveOpen) { this._stream.Close(); } this._stream = null; }
            this._buffer = null;
        }

        private void SwapBufferBytes(int length)
        {
            for (int off = 0, t = length / 2; off < t; ++off)
            {
                byte b = this._buffer[off];
                this._buffer[off] = this._buffer[length - off - 1];
                this._buffer[length - off - 1] = b;
            }
        }

        /// <summary>Sets the position within the stream.</summary>
        /// <param name="offset">A byte offset relative to <c>origin</c>.</param>
        /// <param name="origin">A value indicating the reference point from which the new position is to be obtained.</param>
        /// <returns>The position with the current stream.</returns>
        public long Seek(long offset, System.IO.SeekOrigin origin) { return this._stream.Seek(offset, origin); }
        /// <summary>Returns the current position within the stream.</summary>
        /// <returns>The position with the current stream.</returns>
        public long Tell() { return this._stream.Position; }

        #region Reading functions
        private void FillBuffer(int count)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanRead) { throw new System.NotSupportedException(); }
            if (count < 0 || count > _bufferLength) { throw new System.ArgumentOutOfRangeException("count"); }
            int t;
            if (count == 1)
            {
                t = this._stream.ReadByte();
                if (t == -1) { throw new System.IO.EndOfStreamException(); }
                this._buffer[0] = (byte)t;
            }
            else if (count != 0)
            {
                int off = 0;
                do
                {
                    t = this._stream.Read(this._buffer, off, count - off);
                    if (t == 0) { throw new System.IO.EndOfStreamException(); }
                    off += t;
                } while (off < count);
                if (!this.IsLittleEndian) { this.SwapBufferBytes(count); }
            }
        }
        private int IntReadChars(char[] buffer, int index, int count, System.Text.Encoding encode, System.Text.Decoder decoder)
        {
            int cpS = 1;
            if (encode is System.Text.UnicodeEncoding) { cpS = 2; }
            else if (encode is System.Text.UTF32Encoding) { cpS = 4; }
            int rem = count;
            while (rem > 0)
            {
                int read = System.Math.Min(rem * cpS, _bufferLength);
                read = this._stream.Read(this._buffer, 0, read);
                if (read == 0) { return count - rem; }
                read = decoder.GetChars(this._buffer, 0, read, buffer, index);
                rem -= read;
                index += read;
            }
            return count;
        }
        /// <summary>Reads the specified number of bytes from the stream, starting from a specified point in the byte array.</summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read into <c>buffer</c>. This might be less than the number of bytes requested if that many bytes are not available, or it might be zero if the end of the stream is reached.</returns>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public int Read(byte[] buffer, int index, int count)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanRead) { throw new System.NotSupportedException(); }
            return this._stream.Read(buffer, index, count);
        }
        /// <summary>Reads a Boolean value from the current stream and advances the current position of the stream by one byte.</summary>
        /// <returns>true if the byte is nonzero; otherwise, false.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public bool ReadBoolean()
        {
            FillBuffer(1);
            return this._buffer[0] != 0;
        }
        /// <summary>Reads the next byte from the current stream and advances the current position of the stream by one byte.</summary>
        /// <returns>The next byte read from the current stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public byte ReadByte()
        {
            FillBuffer(1);
            return this._buffer[0];
        }
        /// <summary>Reads the specified number of bytes from the current stream into a byte array and advances the current position by that number of bytes.</summary>
        /// <param name="count">The number of bytes to read. This value must be 0 or a non-negative number or an exception will occur.</param>
        /// <returns>A byte array containing data read from the underlying stream. This might be less than the number of bytes requested if the end of the stream is reached.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><c>count</c> is negative.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public byte[] ReadBytes(int count)
        {

            if (count < 0) { throw new System.ArgumentOutOfRangeException("count"); }
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanRead) { throw new System.NotSupportedException(); }
            if (count == 0) { return new byte[0]; }
            byte[] buffer = new byte[count];
            int off = 0;
            do
            {
                int t = this._stream.Read(buffer, off, count);
                if (t == 0) { break; }
                off += t;
                count -= t;
            } while (count > 0);
            if (off != count) { System.Array.Resize<byte>(ref buffer, off); }
            return buffer;
        }
        /// <summary>Reads the specified number of characters from the current stream, returns the data in a character array, and advances the current position in accordance with the Encoding and the specific characters being read from the stream.</summary>
        /// <param name="count">The number of characters to read.</param>
        /// <returns>A character array containing data read from the underlying stream. This might be less than the number of characters requested if the end of the stream is reached.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"><c>count</c> is negative.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public char[] ReadChars(int count)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanRead) { throw new System.NotSupportedException(); }
            if (count < 0) { throw new System.ArgumentOutOfRangeException("count"); }
            char[] buffer = new char[count];
            int read = this.IntReadChars(buffer, 0, count, this._cEncoding, this._cDecoder);
            if (read != count) { System.Array.Resize<char>(ref buffer, read); }
            return buffer;
        }
        /// <summary>Reads a NULL-terminated string from the current stream.</summary>
        /// <returns>The string being read without the terminating NULL.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public string ReadCString()
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanRead) { throw new System.NotSupportedException(); }
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            System.Text.Encoding encoding = this._cEncoding;
            System.Text.Decoder decoder = this._cDecoder;
            char[] buffer = new char[1];
            do
            {
                if (this.IntReadChars(buffer, 0, 1, encoding, decoder) == 0) { throw new System.IO.EndOfStreamException(); }
                if (buffer[0] == '\0') { break; }
                s.Append(buffer[0]);
            } while (true);
            return s.ToString();
        }
        /// <summary>Reads a 2-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the position of the stream by two bytes.</summary>
        /// <returns>A 2-byte unsigned integer read from this stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public ushort ReadUInt16()
        {
            this.FillBuffer(2);
            return (ushort)(this._buffer[0] | (this._buffer[1] << 8));
        }
        /// <summary>Reads a 4-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the position of the stream by four bytes.</summary>
        /// <returns>A 4-byte unsigned integer read from this stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public uint ReadUInt32()
        {
            this.FillBuffer(4);
            return (uint)(this._buffer[0] | (this._buffer[1] << 8) | (this._buffer[2] << 16) | (this._buffer[3] << 24));
        }
        /// <summary>Reads a 8-byte unsigned integer from the current stream respecting <c>IsLittleEndian</c> and advances the position of the stream by eight bytes.</summary>
        /// <returns>A 8-byte unsigned integer read from this stream.</returns>
        /// <exception cref="System.IO.EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public ulong ReadUInt64()
        {
            this.FillBuffer(8);
            return (ulong)((ulong)this._buffer[0] | ((ulong)this._buffer[1] << 8) | ((ulong)this._buffer[2] << 16) | ((ulong)this._buffer[3] << 24) | ((ulong)this._buffer[4] << 32) | ((ulong)this._buffer[5] << 40) | ((ulong)this._buffer[6] << 48) | ((ulong)this._buffer[7] << 56));
        }

        public short ReadInt16() { return unchecked((short)this.ReadUInt16()); }
        public int ReadInt32() { return unchecked((int)this.ReadUInt32()); }
        public long ReadInt64() { return unchecked((long)this.ReadUInt64()); }

        /// <summary>Copies this stream to another stream.</summary>
        /// <param name="v">A <c>Stream</c> to write to.</param>
        /// <exception cref="System.ArgumentNullException"><c>v</c> is null.</exception>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading or <c>v</c> does not support writing.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void CopyTo(System.IO.Stream v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (v == null) { throw new System.ArgumentNullException("v"); }
            if (!this._stream.CanRead || !v.CanWrite) { throw new System.NotSupportedException(); }
            int num;
            byte[] buffer = new byte[_dynaBufferLength];
            while ((num = v.Read(buffer, 0, _dynaBufferLength)) != 0)
            {
                this._stream.Write(buffer, 0, num);
            }
        }
        #endregion Reading functions
        #region Writing functions
        /// <summary>Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.</summary>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Flush() { if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); } this._stream.Flush(); }
        /// <summary>Writes a string to this stream in the Encoding, and advances the current position of the stream in accordance with the encoding used and the specific characters being written to the stream.</summary>
        /// <param name="v">The value to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(char[] v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            byte[] buffer = this._cEncoding.GetBytes(v, 0, v.Length);
            this._stream.Write(buffer, 0, buffer.Length);
        }
        /// <summary>Writes a region of a byte array to the current stream.</summary>
        /// <param name="buffer">A byte array containing the data to write.</param>
        /// <param name="index">The starting point in <c>buffer</c> at which to begin writing.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <exception cref="System.ArgumentException">The buffer length minus <c>index</c> is less than <c>count</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><c>buffer</c> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> or <c>count</c> is negative.</exception>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(byte[] buffer, int index, int count)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            if (buffer == null) { throw new System.ArgumentNullException("buffer"); }
            if (index < 0) { throw new System.ArgumentOutOfRangeException("index"); }
            if (count < 0) { throw new System.ArgumentOutOfRangeException("count"); }
            if (buffer.Length - index < count) { throw new System.ArgumentException(); }
            this._stream.Write(buffer, index, count);
        }
        /// <summary>Writes a one-byte Boolean value to the current stream, with 0 representing false and 1 representing true.</summary>
        /// <param name="v">The Boolean value to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(bool v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            this._stream.WriteByte(v ? (byte)1 : (byte)0);
        }
        /// <summary>Writes an unsigned byte to the current stream and advances the stream position by one byte.</summary>
        /// <param name="v">The unsigned byte to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(byte v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            this._stream.WriteByte(v);
        }
        /// <summary>Writes a byte array to the underlying stream.</summary>
        /// <param name="buffer">A byte array containing the data to write.</param>
        /// <exception cref="System.ArgumentNullException"><c>buffer</c> is null.</exception>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(byte[] buffer)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            if (buffer == null) { throw new System.ArgumentNullException("buffer"); }
            this._stream.Write(buffer, 0, buffer.Length);
        }
        /// <summary>Writes a two-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the stream position by two bytes.</summary>
        /// <param name="v">The two-byte unsigned integer to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(ushort v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            this._buffer[0] = (byte)v;
            this._buffer[1] = (byte)(v >> 8);
            if (!this.IsLittleEndian) { this.SwapBufferBytes(2); }
            this._stream.Write(this._buffer, 0, 2);
        }
        /// <summary>Writes a four-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the stream position by four bytes.</summary>
        /// <param name="v">The four-byte unsigned integer to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(uint v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            this._buffer[0] = (byte)v;
            this._buffer[1] = (byte)(v >> 8);
            this._buffer[2] = (byte)(v >> 16);
            this._buffer[3] = (byte)(v >> 24);
            if (!this.IsLittleEndian) { this.SwapBufferBytes(4); }
            this._stream.Write(this._buffer, 0, 4);
        }
        /// <summary>Writes an eight-byte unsigned integer to the current stream respecting <c>IsLittleEndian</c> and advances the stream position by eight bytes.</summary>
        /// <param name="v">The eight-byte unsigned integer to write.</param>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(ulong v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (!this._stream.CanWrite) { throw new System.NotSupportedException(); }
            this._buffer[0] = (byte)v;
            this._buffer[1] = (byte)(v >> 8);
            this._buffer[2] = (byte)(v >> 16);
            this._buffer[3] = (byte)(v >> 24);
            this._buffer[4] = (byte)(v >> 32);
            this._buffer[5] = (byte)(v >> 40);
            this._buffer[6] = (byte)(v >> 48);
            this._buffer[7] = (byte)(v >> 56);
            if (!this.IsLittleEndian) { this.SwapBufferBytes(8); }
            this._stream.Write(this._buffer, 0, 8);
        }

        public void Write(short v) { this.Write(unchecked((ushort)v)); }
        public void Write(int v) { this.Write(unchecked((uint)v)); }
        public void Write(long v) { this.Write(unchecked((ulong)v)); }

        /// <summary>Writes a <c>Stream</c> to the underlying stream.</summary>
        /// <param name="v">A <c>Stream</c> to write.</param>
        /// <exception cref="System.ArgumentNullException"><c>v</c> is null.</exception>
        /// <exception cref="System.IOException">An I/O error occurs.</exception>
        /// <exception cref="System.NotSupportedException">The stream does not support writing or <c>v</c> does not support reading.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public void Write(System.IO.Stream v)
        {
            if (this._stream == null) { throw new System.ObjectDisposedException(this.GetType().FullName); }
            if (v == null) { throw new System.ArgumentNullException("v"); }
            if (!this._stream.CanWrite || !v.CanRead) { throw new System.NotSupportedException(); }
            int num;
            byte[] buffer = new byte[_dynaBufferLength];
            while ((num = v.Read(buffer, 0, _dynaBufferLength)) != 0)
            {
                this._stream.Write(buffer, 0, num);
            }
        }
        #endregion Writing functions
    }
}
