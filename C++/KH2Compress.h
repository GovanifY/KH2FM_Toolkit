#pragma once

#include <string>
#include <iostream>
#include <iomanip>
#include <stdexcept>

//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System;
//C# TO C++ CONVERTER TODO TASK: The .NET System namespace is not available from native C++:
//using namespace System::Diagnostics;

namespace KHCompress
{
//C# TO C++ CONVERTER NOTE: The following .NET attribute has no direct equivalent in native C++:
//[Serializable]
	class NotCompressableException : public std::exception
	{
	public:
		NotCompressableException();

		NotCompressableException(const std::string &message);

		NotCompressableException(const std::string &message, std::exception &inner);
	};

	class KH2Compressor
	{
		/// <summary>How far back to search for matches</summary>
	private:
		static const unsigned char bufferSize = 255;

		/// <summary>Maximum characters to match - 3</summary>
		static const unsigned char maxMatch = 255;

		/// <summary>Finds the least used byte in a set of data</summary>
		/// <param name="data">Byte array to search in</param>
		/// <returns>Most uncommon byte</returns>
		static unsigned char findLeastByte(unsigned char data[]);

	public:
		static unsigned char *compress(unsigned char input[]);

		static unsigned char *decompress(unsigned char input[], unsigned int uSize);
	};
}
