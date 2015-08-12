using System;
using System.Security.Cryptography;
using System.IO;


public class SimpleAES
{

    internal static byte[] Decrypt(byte[] encBytes, byte[] key, byte[] vector, RijndaelManaged _rijndaelManaged)
    {
        byte[] decBytes;

        using (var mstream = new MemoryStream())
        using (var crypto = new CryptoStream(mstream, _rijndaelManaged.CreateDecryptor(key, vector), CryptoStreamMode.Write))
        {
            crypto.Write(encBytes, 0, encBytes.Length);
            crypto.FlushFinalBlock();

            mstream.Position = 0;

            decBytes = new byte[mstream.Length];
            mstream.Read(decBytes, 0, decBytes.Length);
        }

        return decBytes;
    }

    internal static byte[] Encrypt(byte[] allBytes, byte[] key, byte[] vector, RijndaelManaged _rijndaelManaged)
    {
        byte[] encBytes;

        using (var mstream = new MemoryStream())
        using (var crypto = new CryptoStream(mstream, _rijndaelManaged.CreateEncryptor(key, vector), CryptoStreamMode.Write))
        {
            crypto.Write(allBytes, 0, allBytes.Length);
            crypto.FlushFinalBlock();

            mstream.Position = 0;

            encBytes = new byte[mstream.Length];
            mstream.Read(encBytes, 0, encBytes.Length);
        }

        return encBytes;
    }
}