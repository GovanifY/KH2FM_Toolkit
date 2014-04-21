#include "KH2Compress.h"

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics;

namespace KHCompress
{

	NotCompressableException::NotCompressableException()
	{
	}

	NotCompressableException::NotCompressableException(const std::string &message) : Exception(message)
	{
	}

	NotCompressableException::NotCompressableException(const std::string &message, std::exception &inner) : Exception(message, inner)
	{
	}

	unsigned char KH2Compressor::findLeastByte(unsigned char data[])
	{
		unsigned int cnt[256];
		for (unsigned char::const_iterator i = data->begin(); i != data->end(); ++i)
		{
			++cnt[*i];
		}
		unsigned int fC = unsigned int::MaxValue;
		unsigned char f = 0x13;
		//flag cannot be NULL (compressed file can be buffered with NULLs at end)
		for (int i = 1; i < sizeof(cnt) / sizeof(cnt[0]); ++i)
		{
			if (cnt[i] < fC)
			{
				f = static_cast<unsigned char>(i);
				fC = cnt[i];
				if (fC == 0)
				{
					break;
				}
			}
		}
		return f;
	}

	unsigned char *KH2Compressor::compress(unsigned char input[])
	{
		// Compressed format has max of 4 bytes for length
		if (input->LongLength > unsigned int::MaxValue)
		{
			throw new NotCompressableException("Source too big");
		}
			// 10 bytes is the absolute smallest that can be compressed. "000000000" -> "+++0LLLLF".
		if (sizeof(input) / sizeof(input[0]) < 10)
		{
			throw new NotCompressableException("Source too small");
		}
		unsigned char flag = findLeastByte(input); // Get the least-used byte for a flag
		int i = sizeof(input) / sizeof(input[0]), o = i - 6; // Output position (-6 for the 5 bytes added at the end + 1 byte smaller then input minimum)
			// Input position
		unsigned char outbuf[o]; // Output buffer (since we can't predict how well the file will compress)
		while (--i >= 0 && --o >= 0)
		{
			if (i > 2 && o >= 2)
			{
				/*Attempt compression*/
				int buffEnd = sizeof(input) / sizeof(input[0]) <= i + bufferSize ? sizeof(input) / sizeof(input[0]) : i + bufferSize + 1;
				int mLen = 3; //minimum = 4, so init this to 3
				unsigned char mPos = 0;
				for (int j = i + 1; j < buffEnd; ++j)
				{
					int cnt = 0;
					while (i >= cnt && input[j - cnt] == input[i - cnt])
					{
						if (++cnt == maxMatch + 3)
						{
							mLen = maxMatch + 3;
							mPos = static_cast<unsigned char>(j - i);
							j = buffEnd; // Break out of for loop
							break; // Break out of while loop
						}
					}
					if (cnt > mLen)
					{
						mLen = cnt;
						mPos = static_cast<unsigned char>(j - i);
					}
				}
				if (mLen > 3)
				{
					outbuf[o] = flag;
					outbuf[--o] = mPos;
					outbuf[--o] = static_cast<unsigned char>(mLen - 3);
					i -= (mLen - 1);
					continue;
				}
			}

			if ((outbuf[o] = input[i]) == flag) // No match was made, so copy the byte
			{
				if (--o < 0)
				{
					break; // There's not enough room to store the literal
				}
				outbuf[o] = 0; // Output 0 to mean the byte is literal, and not a flag
			}
		}
		if (o < 0)
		{
			throw new NotCompressableException("Compressed data is as big as original");
		}

		// get length of compressed data (-1 for minimum 1 byte smaller)
		i = sizeof(input) / sizeof(input[0]) - o - 1;
		unsigned char output[i];
		Array::Copy(outbuf, o, output, 0, i - 5);
		output[i - 5] = static_cast<unsigned char>(sizeof(input) / sizeof(input[0]) >> 24);
		output[i - 4] = static_cast<unsigned char>(sizeof(input) / sizeof(input[0]) >> 16);
		output[i - 3] = static_cast<unsigned char>(sizeof(input) / sizeof(input[0]) >> 8);
		output[i - 2] = static_cast<unsigned char>(sizeof(input) / sizeof(input[0]));
		output[i - 1] = flag;
		std::cout << "  Compressed to " << std::dec << std::setw(0) << std::setprecision(0) << static_cast<double>(i) / sizeof(input) / sizeof(input[0]) << std::endl;
		return output;
	}

	unsigned char *KH2Compressor::decompress(unsigned char input[], unsigned int uSize)
	{
		if (input->LongLength > unsigned int::MaxValue)
		{
			throw new ArgumentOutOfRangeException("data", "Array to large to handle");
		}
		unsigned int inputOffset = static_cast<unsigned int>(input->LongLength);
		// Can be buffered with NULLs at the end
		while (input[--inputOffset] == 0)
		{
		}
		unsigned char magic = input[inputOffset];
	#if defined(DEBUG)
		unsigned int outputOffset = BitConverter::ToUInt32(new unsigned char[] {input[--inputOffset], input[--inputOffset], input[--inputOffset], input[--inputOffset]}, 0);
		Debug::WriteLineIf(outputOffset != uSize, "Got size " + uSize + "from IDX, but " + outputOffset + " internally");
		outputOffset = uSize;
	#else
		// KH2 internally skips the 4 "size" bytes and uses what the IDX says
		inputOffset -= 4;
		unsigned int outputOffset = uSize;
	#endif
		unsigned char output[outputOffset];
		while (inputOffset > 0) // && outputOffset > 0
			//I could check for outputOffset too, but if it goes below 0 the file is probably corrupt. Let the caller handle that.
		{
			unsigned char c = input[--inputOffset], offset;
			if (c == magic && (offset = input[--inputOffset]) != 0)
			{
				int count = input[--inputOffset] + 3;
				while (--count >= 0)
				{
					output[--outputOffset] = output[offset + outputOffset];
				}
			}
			else
			{
				output[--outputOffset] = c;
			}
		}
		return output;
	}
}
