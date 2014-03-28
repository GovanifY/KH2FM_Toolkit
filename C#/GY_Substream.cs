// Ver 1.0.1
namespace GovanifY.Utility
{
    public sealed class Substream : System.IO.Stream, System.IDisposable
    {
        private System.IO.Stream baseStream;
        private readonly long origin;
        private readonly long length;
        private long position = 0;
        private readonly bool leaveOpen;

        /// <summary><para>Initializes a new instance of the <c>Substream</c> class encompassing the whole of <c>baseStream</c>.</para><para>When the <c>Substream</c> is closed, <c>baseStream</c> will remain open.</para></summary>
        /// <param name="baseStream">The <c>Stream</c> from which to create this stream.</param>
        public Substream(System.IO.Stream baseStream)
        {
            this.baseStream = baseStream;
            this.origin = 0;
            if (baseStream.Length <= 0) { throw new System.ArgumentException("baseStream.Length <= 0", "baseStream"); }
            this.length = baseStream.Length;
            this.leaveOpen = true;
        }
        /// <summary>Initializes a new instance of the <c>Substream</c> class encompassing the specified range.</summary>
        /// <param name="baseStream">The <c>Stream</c> from which to create this stream.</param>
        /// <param name="origin">A byte offset relative to the beginning of <c>baseStream</c>.</param>
        /// <param name="length"><para>The maximum number of bytes to encompass.</para><para>If <c>origin</c>+<c>length</c> is more then <c>baseStream</c>'s length, <c>length</c> will be adjusted to fit.</para></param>
        /// <param name="leaveOpen">true to leave <c>baseStream</c> open after the <c>Substream</c> object is disposed; otherwise, false.</param>
        public Substream(System.IO.Stream baseStream, long origin, long length, bool leaveOpen = true)
        {
            this.baseStream = baseStream;
            this.origin = origin;
            if (origin + length > baseStream.Length) { length += (int)(baseStream.Length - (origin + length)); }
            if (length <= 0) { throw new System.ArgumentException("adjusted(length) <= 0", "length"); }
            this.length = length;
            this.leaveOpen = leaveOpen;
        }
        /// <summary>Closes the current stream and optionally releases the base stream.</summary>
        public override void Close() { Dispose(true); }
        /// <summary>Closes the current stream and optionally releases the base stream.</summary>
        protected override void Dispose(bool disposing)
        {
            if (this.baseStream != null)
            {
                if (!this.leaveOpen) { this.baseStream.Close(); }
                this.baseStream = null;
            }
        }

        /// <summary>Gets a value indicating whether the current stream is open and valid.</summary>
        public bool IsOpen { get { return this.baseStream != null; } }
        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        public override bool CanRead { get { return this.baseStream == null ? false : this.baseStream.CanRead; } }
        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        public override bool CanSeek { get { return this.baseStream == null ? false : this.baseStream.CanSeek; } }
        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite { get { return this.baseStream == null ? false : this.baseStream.CanWrite; } }
        /// <summary>Gets the length in bytes of the stream.</summary>
        public override long Length { get { if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); } return this.length; } }
        /// <summary>Gets or sets the position within the current stream.</summary>
        public override long Position
        {
            get { if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); } return this.position; }
            set { if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); } this.position = value; }
        }
        /// <summary>Passes a flush command to the underlying stream.</summary>
        public override void Flush() { this.baseStream.Flush(); }
        /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <c>offset</c> and (<c>offset</c> + <c>count</c> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <c>buffer</c> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); }
            if (this.position + count > this.length) { count += (int)(this.length - (this.position + count)); }
            if (count <= 0) { return 0; }
            if (this.baseStream.Position != this.origin + this.position) { this.baseStream.Position = this.origin + this.position; }
            int read = this.baseStream.Read(buffer, offset, count);
            this.position += read;
            return read;
        }
        /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); }
            if (this.position >= this.length) { return -1; }
            if (this.baseStream.Position != this.origin + this.position) { this.baseStream.Position = this.origin + this.position; }
            int read = this.baseStream.ReadByte();
            this.position++;
            return read;
        }
        /// <summary>Sets the position within the current stream.</summary>
        /// <param name="offset">A byte offset relative to the <c>origin</c> parameter.</param>
        /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); }
            switch (origin)
            {
                case System.IO.SeekOrigin.Begin:
                    this.position = offset;
                    break;
                case System.IO.SeekOrigin.Current:
                    this.position += offset;
                    break;
                case System.IO.SeekOrigin.End:
                    this.position = this.length - offset;
                    break;
            }
            return this.position;
        }
        public override void SetLength(long value) { throw new System.NotSupportedException(); }
        /// <summary>Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
        /// <param name="buffer">An array of bytes. This method copies <c>count</c> bytes from <c>buffer</c> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <c>buffer</c> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); }
            if (this.position + count > this.length) { count += (int)(this.length - (this.position + count)); }
            if (count <= 0) { return; }
            if (this.baseStream.Position != this.origin + this.position) { this.baseStream.Position = this.origin + this.position; }
            this.baseStream.Write(buffer, offset, count);
            this.position += count;
        }
        /// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            if (this.baseStream == null) { throw new System.ObjectDisposedException(GetType().FullName); }
            if (this.position >= this.length) { return; }
            if (this.baseStream.Position != this.origin + this.position) { this.baseStream.Position = this.origin + this.position; }
            this.baseStream.WriteByte(value);
            this.position++;
        }
        
    }
}
