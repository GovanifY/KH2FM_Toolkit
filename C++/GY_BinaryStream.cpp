#include "GY_BinaryStream.h"

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

		BinaryStream::BinaryStream(Stream *stream, bool littleEndian = true, bool leaveOpen = false)
		{
		}

		BinaryStream::BinaryStream(Stream *stream, Encoding *encoding, bool littleEndian = true, bool leaveOpen = false) : _leaveOpen(leaveOpen)
		{
			if (stream == 0)
			{
				throw new ArgumentNullException("stream");
			}
			if (encoding == 0)
			{
				throw new ArgumentNullException("encoding");
			}
			if (!stream->CanRead && !stream->CanWrite)
			{
				throw new ArgumentException("Cannot read or write from stream.", "stream");
			}
			_stream = stream;
			_cEncoding = encoding;
			IsLittleEndian = littleEndian;
		}

		const Decoder &BinaryStream::get_cDecoder() const
		{
			if (_cDecoderV == 0)
			{
				_cDecoderV = _cEncoding->GetDecoder();
			}
			return _cDecoderV;
		}

		const Encoding &BinaryStream::getTextEncoding() const
		{
			return _cEncoding;
		}

		void BinaryStream::setTextEncoding(const Encoding &value)
		{
			if (value == 0)
			{
				throw new ArgumentNullException();
			}
			_cEncoding = value;
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
			delete _cDecoderV;
		}

		const Stream &BinaryStream::getBaseStream() const
		{
			if (_stream->CanWrite)
			{
				_stream->Flush();
			}
			return _stream;
		}

		const bool &BinaryStream::getIsOpen() const
		{
			return _stream != 0;
		}

		const bool &BinaryStream::getCanRead() const
		{
			return _stream != 0 ? _stream->CanRead : false;
		}

		const bool &BinaryStream::getCanWrite() const
		{
			return _stream != 0 ? _stream->CanWrite : false;
		}

		BinaryStream::~BinaryStream()
		{
			if (_stream != 0)
			{
				if (!_leaveOpen)
				{
					_stream->Close();
				}
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
				delete _stream;
			}
			_buffer = 0;
		}

		void BinaryStream::Close()
		{
			delete this;
		}

		void BinaryStream::SwapBufferBytes(int length)
		{
			for (int off = 0, t = length / 2; off < t; ++off)
			{
				unsigned char b = _buffer[off];
				_buffer[off] = _buffer[length - off - 1];
				_buffer[length - off - 1] = b;
			}
		}

		long long BinaryStream::Seek(long long offset, SeekOrigin origin)
		{
			return _stream->Seek(offset, origin);
		}

		long long BinaryStream::Tell()
		{
			return _stream->Position;
		}

		void BinaryStream::FillBuffer(int count)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanRead)
			{
				throw new NotSupportedException();
			}
			if (count < 0 || count > _bufferLength)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			int t;
			if (count == 1)
			{
				t = _stream->ReadByte();
				if (t == -1)
				{
					throw new EndOfStreamException();
				}
				_buffer[0] = static_cast<unsigned char>(t);
			}
			else if (count != 0)
			{
				int off = 0;
				do
				{
					t = _stream->Read(_buffer, off, count - off);
					if (t == 0)
					{
						throw new EndOfStreamException();
					}
					off += t;
				} while (off < count);
				if (!IsLittleEndian)
				{
					SwapBufferBytes(count);
				}
			}
		}

		int BinaryStream::IntReadChars(wchar_t buffer[], int index, int count, Encoding *encode, Decoder *decoder)
		{
			int cpS = 1;
			if (dynamic_cast<UnicodeEncoding*>(encode) != 0)
			{
				cpS = 2;
			}
			else if (dynamic_cast<UTF32Encoding*>(encode) != 0)
			{
				cpS = 4;
			}
			int rem = count;
			while (rem > 0)
			{
				int read = __min(rem*cpS, _bufferLength);
				read = _stream->Read(_buffer, 0, read);
				if (read == 0)
				{
					return count - rem;
				}
				read = decoder->GetChars(_buffer, 0, read, buffer, index);
				rem -= read;
				index += read;
			}
			return count;
		}

		int BinaryStream::Read(unsigned char buffer[], int index, int count)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanRead)
			{
				throw new NotSupportedException();
			}
			return _stream->Read(buffer, index, count);
		}

		bool BinaryStream::ReadBoolean()
		{
			FillBuffer(1);
			return _buffer[0] != 0;
		}

		unsigned char BinaryStream::ReadByte()
		{
			FillBuffer(1);
			return _buffer[0];
		}

		unsigned char *BinaryStream::ReadBytes(int count)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanRead)
			{
				throw new NotSupportedException();
			}
			if (count == 0)
			{
				return new unsigned char[0];
			}
			unsigned char buffer[count];
			int off = 0;
			do
			{
				int t = _stream->Read(buffer, off, count);
				if (t == 0)
				{
					break;
				}
				off += t;
				count -= t;
			} while (count > 0);
			if (off != count)
			{
				Array::Resize(buffer, off);
			}
			return buffer;
		}

		wchar_t *BinaryStream::ReadChars(int count)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanRead)
			{
				throw new NotSupportedException();
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			wchar_t buffer[count];
			int read = IntReadChars(buffer, 0, count, _cEncoding, get_cDecoder());
			if (read != count)
			{
				Array::Resize(buffer, read);
			}
			return buffer;
		}

		std::string BinaryStream::ReadCString()
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanRead)
			{
				throw new NotSupportedException();
			}
			StringBuilder *s = new StringBuilder();
			Encoding *encoding = _cEncoding;
			Decoder *decoder = get_cDecoder();
			wchar_t buffer[1];
			do
			{
				if (IntReadChars(buffer, 0, 1, encoding, decoder) == 0)
				{
					throw new EndOfStreamException();
				}
				if (buffer[0] == '\0')
				{
					break;
				}
				s->Append(buffer[0]);
			} while (true);
//C# TO C++ CONVERTER TODO TASK: There is no native C++ equivalent to 'ToString':
			return s->ToString();
		}

		unsigned short BinaryStream::ReadUInt16()
		{
			FillBuffer(2);
			return static_cast<unsigned short>(_buffer[0] | (_buffer[1] << 8));
		}

		unsigned int BinaryStream::ReadUInt32()
		{
			FillBuffer(4);
			return static_cast<unsigned int>(_buffer[0] | (_buffer[1] << 8) | (_buffer[2] << 16) | (_buffer[3] << 24));
		}

		unsigned long long BinaryStream::ReadUInt64()
		{
			FillBuffer(8);
			return _buffer[0] | (static_cast<unsigned long long>(_buffer[1]) << 8) | (static_cast<unsigned long long>(_buffer[2]) << 16) | (static_cast<unsigned long long>(_buffer[3]) << 24) | (static_cast<unsigned long long>(_buffer[4]) << 32) | (static_cast<unsigned long long>(_buffer[5]) << 40) | (static_cast<unsigned long long>(_buffer[6]) << 48) | (static_cast<unsigned long long>(_buffer[7]) << 56);
		}

		short BinaryStream::ReadInt16()
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: return unchecked((short) ReadUInt16());
			return static_cast<short>(ReadUInt16());
		}

		int BinaryStream::ReadInt32()
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: return unchecked((int) ReadUInt32());
			return static_cast<int>(ReadUInt32());
		}

		long long BinaryStream::ReadInt64()
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: return unchecked((long) ReadUInt64());
			return static_cast<long long>(ReadUInt64());
		}

		void BinaryStream::CopyTo(Stream *v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (v == 0)
			{
				throw new ArgumentNullException("v");
			}
			if (!_stream->CanRead || !v->CanWrite)
			{
				throw new NotSupportedException();
			}
			int num;
			unsigned char buffer[_dynaBufferLength];
			while ((num = v->Read(buffer, 0, _dynaBufferLength)) != 0)
			{
				_stream->Write(buffer, 0, num);
			}
		}

		void BinaryStream::Flush()
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			_stream->Flush();
		}

		void BinaryStream::Write(wchar_t v[])
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
//ORIGINAL LINE: byte[] buffer = _cEncoding.GetBytes(v, 0, v.Length);
//C# TO C++ CONVERTER WARNING: Since the array size is not known in this declaration, C# to C++ Converter has converted this array to a pointer.  You will need to call 'delete[]' where appropriate:
			unsigned char *buffer = _cEncoding->GetBytes(v, 0, sizeof(v) / sizeof(v[0]));
			_stream->Write(buffer, 0, sizeof(buffer) / sizeof(buffer[0]));
		}

		void BinaryStream::Write(unsigned char buffer[], int index, int count)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			if (buffer == 0)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if (sizeof(buffer) / sizeof(buffer[0]) - index < count)
			{
				throw new ArgumentException();
			}
			_stream->Write(buffer, index, count);
		}

		void BinaryStream::Write(bool v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			_stream->WriteByte(v ? static_cast<unsigned char>(1) : static_cast<unsigned char>(0));
		}

		void BinaryStream::Write(unsigned char v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			_stream->WriteByte(v);
		}

		void BinaryStream::Write(unsigned char buffer[])
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			if (buffer == 0)
			{
				throw new ArgumentNullException("buffer");
			}
			_stream->Write(buffer, 0, sizeof(buffer) / sizeof(buffer[0]));
		}

		void BinaryStream::Write(unsigned short v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			_buffer[0] = static_cast<unsigned char>(v);
			_buffer[1] = static_cast<unsigned char>(v >> 8);
			if (!IsLittleEndian)
			{
				SwapBufferBytes(2);
			}
			_stream->Write(_buffer, 0, 2);
		}

		void BinaryStream::Write(unsigned int v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			_buffer[0] = static_cast<unsigned char>(v);
			_buffer[1] = static_cast<unsigned char>(v >> 8);
			_buffer[2] = static_cast<unsigned char>(v >> 16);
			_buffer[3] = static_cast<unsigned char>(v >> 24);
			if (!IsLittleEndian)
			{
				SwapBufferBytes(4);
			}
			_stream->Write(_buffer, 0, 4);
		}

		void BinaryStream::Write(unsigned long long v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (!_stream->CanWrite)
			{
				throw new NotSupportedException();
			}
			_buffer[0] = static_cast<unsigned char>(v);
			_buffer[1] = static_cast<unsigned char>(v >> 8);
			_buffer[2] = static_cast<unsigned char>(v >> 16);
			_buffer[3] = static_cast<unsigned char>(v >> 24);
			_buffer[4] = static_cast<unsigned char>(v >> 32);
			_buffer[5] = static_cast<unsigned char>(v >> 40);
			_buffer[6] = static_cast<unsigned char>(v >> 48);
			_buffer[7] = static_cast<unsigned char>(v >> 56);
			if (!IsLittleEndian)
			{
				SwapBufferBytes(8);
			}
			_stream->Write(_buffer, 0, 8);
		}

		void BinaryStream::Write(short v)
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: Write(unchecked((ushort) v));
			Write(static_cast<unsigned short>(v));
		}

		void BinaryStream::Write(int v)
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: Write(unchecked((uint) v));
			Write(static_cast<unsigned int>(v));
		}

		void BinaryStream::Write(long long v)
		{
//C# TO C++ CONVERTER TODO TASK: There is no C++ equivalent to 'unchecked' in this context:
//ORIGINAL LINE: Write(unchecked((ulong) v));
			Write(static_cast<unsigned long long>(v));
		}

		void BinaryStream::Write(Stream *v)
		{
			if (_stream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (v == 0)
			{
				throw new ArgumentNullException("v");
			}
			if (!_stream->CanWrite || !v->CanRead)
			{
				throw new NotSupportedException();
			}
			int num;
			unsigned char buffer[_dynaBufferLength];
			while ((num = v->Read(buffer, 0, _dynaBufferLength)) != 0)
			{
				_stream->Write(buffer, 0, num);
			}
		}
	}
}
