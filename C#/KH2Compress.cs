using System;
using System.Diagnostics;

namespace KHCompress
{
    [Serializable]
    public class NotCompressableException : Exception
    {
        public NotCompressableException()
        {
        }

        public NotCompressableException(string message)
            : base(message)
        {
        }

        public NotCompressableException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public static class KH2Compressor
    {
        /// <summary>How far back to search for matches</summary>
        private const byte bufferSize = 255;

        /// <summary>Maximum characters to match - 3</summary>
        private const byte maxMatch = 255;

        /// <summary>Finds the least used byte in a set of data</summary>
        /// <param name="data">Byte array to search in</param>
        /// <returns>Most uncommon byte</returns>
        private static byte findLeastByte(byte[] data)
        {
            var cnt = new uint[256];
            foreach (byte i in data)
            {
                ++cnt[i];
            }
            uint fC = uint.MaxValue;
            byte f = 0x13;
            //flag cannot be NULL (compressed file can be buffered with NULLs at end)
            for (int i = 1; i < cnt.Length; ++i)
            {
                if (cnt[i] < fC)
                {
                    f = (byte)i;
                    fC = cnt[i];
                    if (fC == 0)
                    {
                        break;
                    }
                }
            }
            return f;
        }

        public static byte[] compress(byte[] input)
        {
            // Compressed format has max of 4 bytes for length
            if (input.LongLength > uint.MaxValue)
            {
                throw new NotCompressableException("Source too big");
            }
            // 10 bytes is the absolute smallest that can be compressed. "000000000" -> "+++0LLLLF".
            if (input.Length < 10)
            {
                throw new NotCompressableException("Source too small");
            }
            byte flag = findLeastByte(input); // Get the least-used byte for a flag
            int i = input.Length,
                // Input position
                o = i - 6; // Output position (-6 for the 5 bytes added at the end + 1 byte smaller then input minimum)
            var outbuf = new byte[o]; // Output buffer (since we can't predict how well the file will compress)
            while (--i >= 0 && --o >= 0)
            {
                if (i > 2 && o >= 2)
                {
                    /*Attempt compression*/
                    int buffEnd = input.Length <= i + bufferSize ? input.Length : i + bufferSize + 1;
                    int mLen = 3; //minimum = 4, so init this to 3
                    byte mPos = 0;
                    for (int j = i + 1; j < buffEnd; ++j)
                    {
                        int cnt = 0;
                        while (i >= cnt && input[j - cnt] == input[i - cnt])
                        {
                            if (++cnt == maxMatch + 3)
                            {
                                mLen = maxMatch + 3;
                                mPos = (byte)(j - i);
                                j = buffEnd; // Break out of for loop
                                break; // Break out of while loop
                            }
                        }
                        if (cnt > mLen)
                        {
                            mLen = cnt;
                            mPos = (byte)(j - i);
                        }
                    }
                    if (mLen > 3)
                    {
                        outbuf[o] = flag;
                        outbuf[--o] = mPos;
                        outbuf[--o] = (byte)(mLen - 3);
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
            i = input.Length - o - 1;
            var output = new byte[i];
            Array.Copy(outbuf, o, output, 0, i - 5);
            output[i - 5] = (byte)(input.Length >> 24);
            output[i - 4] = (byte)(input.Length >> 16);
            output[i - 3] = (byte)(input.Length >> 8);
            output[i - 2] = (byte)(input.Length);
            output[i - 1] = flag;
            Console.WriteLine("  Compressed to {0:0%} of the original size!", (double)i / input.Length);
            return output;
        }

        public static byte[] decompress(byte[] input, uint uSize)
        {
#if NODECOMPRESS
            return input;
        }
    }
}
#else
            if (input.LongLength > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException("data", "Array to large to handle");
            }
            var inputOffset = (uint) input.LongLength;
            // Can be buffered with NULLs at the end
            while (input[--inputOffset] == 0)
            {
            }
            byte magic = input[inputOffset];
#if DEBUG
            uint outputOffset =
                BitConverter.ToUInt32(
                    new[] {input[--inputOffset], input[--inputOffset], input[--inputOffset], input[--inputOffset]}, 0);
            Debug.WriteLineIf(outputOffset != uSize,
                "Got size " + uSize + "from IDX, but " + outputOffset + " internally");
            outputOffset = uSize;
#else
    // KH2 internally skips the 4 "size" bytes and uses what the IDX says
            inputOffset -= 4;
            uint outputOffset = uSize;
#endif
            var output = new byte[outputOffset];
            while (inputOffset > 0 /* && outputOffset > 0*/)
                //I could check for outputOffset too, but if it goes below 0 the file is probably corrupt. Let the caller handle that.
            {
                byte c = input[--inputOffset], offset;
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
}
#endif