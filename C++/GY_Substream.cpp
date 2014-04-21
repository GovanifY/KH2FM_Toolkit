#include "GY_Substream.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::IO;
namespace GovanifY
{
	namespace Utility
	{

		Substream::Substream(Stream *baseStream) : leaveOpen(true), length(baseStream->Length), origin(0)
		{
			this->baseStream = baseStream;
			if (baseStream->Length <= 0)
			{
				throw new ArgumentException("baseStream.Length <= 0", "baseStream");
			}
		}

		Substream::Substream(Stream *baseStream, long long origin, long long length, bool leaveOpen = true) : leaveOpen(leaveOpen), length(length), origin(origin)
		{
			this->baseStream = baseStream;
			if (origin + length > baseStream->Length)
			{
				length += static_cast<int>(baseStream->Length - (origin + length));
			}
			if (length <= 0)
			{
				throw new ArgumentException("adjusted(length) <= 0", "length");
			}
		}

		const bool &Substream::getIsOpen() const
		{
			return baseStream != 0;
		}

		const bool &Substream::getCanRead() const
		{
			return baseStream == Nullable<0*> false : baseStream->CanRead;
		}

		const bool &Substream::getCanSeek() const
		{
			return baseStream == Nullable<0*> false : baseStream->CanSeek;
		}

		const bool &Substream::getCanWrite() const
		{
			return baseStream == Nullable<0*> false : baseStream->CanWrite;
		}

		const long long &Substream::getLength() const
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			return length;
		}

		const long long &Substream::getPosition() const
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			return position;
		}

		void Substream::setPosition(const long long &value)
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			position = value;
		}

		void Substream::Close()
		{
			this->Dispose(true);
		}

		void Substream::Dispose(bool disposing)
		{
			if (baseStream != 0)
			{
				if (!leaveOpen)
				{
					baseStream->Close();
				}
//C# TO C++ CONVERTER WARNING: C# to C++ Converter converted the original 'null' assignment to a call to 'delete', but you should review memory allocation of all pointer variables in the converted code:
				delete baseStream;
			}
		}

		void Substream::Flush()
		{
			baseStream->Flush();
		}

		int Substream::Read(unsigned char buffer[], int offset, int count)
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (position + count > length)
			{
				count += static_cast<int>(length - (position + count));
			}
			if (count <= 0)
			{
				return 0;
			}
			if (baseStream->Position != origin + position)
			{
				baseStream->Position = origin + position;
			}
			int read = baseStream->Read(buffer, offset, count);
			position += read;
			return read;
		}

		int Substream::ReadByte()
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (position >= length)
			{
				return -1;
			}
			if (baseStream->Position != origin + position)
			{
				baseStream->Position = origin + position;
			}
			int read = baseStream->ReadByte();
			position++;
			return read;
		}

		long long Substream::Seek(long long offset, SeekOrigin origin)
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			switch (origin)
			{
				case SeekOrigin::Begin:
					position = offset;
					break;
				case SeekOrigin::Current:
					position += offset;
					break;
				case SeekOrigin::End:
					position = length - offset;
					break;
			}
			return position;
		}

		void Substream::SetLength(long long value)
		{
			throw new NotSupportedException();
		}

		void Substream::Write(unsigned char buffer[], int offset, int count)
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (position + count > length)
			{
				count += static_cast<int>(length - (position + count));
			}
			if (count <= 0)
			{
				return;
			}
			if (baseStream->Position != origin + position)
			{
				baseStream->Position = origin + position;
			}
			baseStream->Write(buffer, offset, count);
			position += count;
		}

		void Substream::WriteByte(unsigned char value)
		{
			if (baseStream == 0)
			{
				throw new ObjectDisposedException(GetType()->FullName);
			}
			if (position >= length)
			{
				return;
			}
			if (baseStream->Position != origin + position)
			{
				baseStream->Position = origin + position;
			}
			baseStream->WriteByte(value);
			position++;
		}
	}
}
