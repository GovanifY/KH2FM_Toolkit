using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using IDX_Tools;
using KH2FM_Toolkit;
using KH2FM_Toolkit.Properties;

namespace HashList
{
    public sealed class HashPairs
    {
        public static Dictionary<UInt32, string> pairs;
        public static string version { get; private set; }
        public static string author { get; private set; }

        public static void loadHashPairs(string filename = "HashList.bin", bool forceReload = false,
            bool printInfo = false)

        {
            if (File.Exists(filename))
            {
                if (printInfo)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Debug.WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
                    Console.ResetColor();
                }
            }

            #region EncryptionRessource

            byte[] Hashlist = Resources.HashList;
            PatchManager.GYXor(Hashlist);
            string Hashtemp = Path.GetTempFileName();
            File.WriteAllBytes(Hashtemp, Hashlist);
            if (pairs == null)
            {
                pairs = new Dictionary<UInt32, string>();
            }
            else if (forceReload)
            {
                pairs.Clear();
            }
            else
            {
                return;
            }
            version = author = "";
            if (!File.Exists(filename)) // If it's not in the current directory, try the EXE's location
            {
                filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" +
                           filename;
                if (File.Exists(filename))
                {
                    if (printInfo)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Debug.WriteLine("HASHLIST file found! Loading this file instead of the basic one!");
                        Console.ResetColor();
                    }
                }
            }
            if (!File.Exists(filename))
            {
                #endregion EncryptionRessource

                #region Loadressource

                using (
                    var rd =
                        new StreamReader(File.Open(Hashtemp, FileMode.Open, FileAccess.Read, FileShare.Read),
                            Encoding.UTF8, false, 1024))
                {
                    string line;
                    int index;
                    while ((line = rd.ReadLine()) != null)
                    {
                        if (line.Length != 0)
                        {
                            if (line[0] == '#')
                            {
                                if (line.StartsWith("#Version ") && line.Length > 9)
                                {
                                    //^#Version ([\.0-9]+)(?: (.+))?$
                                    index = line.IndexOf(' ', 9);
                                    if (index > 9 && index < line.Length - 1)
                                    {
                                        author = line.Substring(index + 1).Trim();
                                    }
                                    else
                                    {
                                        index = line.Length;
                                    }
                                    version = line.Substring(9, index - 9).Trim();
                                }
                            }
                            else
                            {
                                index = line.IndexOf('=');
                                if (index > 0 && index < line.Length - 1 &&
                                    (filename = line.Substring(index + 1).Trim()).Length > 0)
                                {
                                    try
                                    {
                                        pairs.Add(Convert.ToUInt32(line.Substring(0, index).Trim(), 16), filename);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("HASHLIST: Failed to parse line \"{0}\"\n{1} {2}", line,
                                            e.GetType(), e.Message);
                                    }
                                }
                            }
                        }
                    }
                }
            }
                #endregion Loadressource
                #region Loadexternal

            else
            {
                byte[] Hashlistencrypted = File.ReadAllBytes(filename);
                PatchManager.GYXor(Hashlistencrypted);
                string Hashtemp2;
                Hashtemp2 = Path.GetTempFileName();
                File.WriteAllBytes(Hashtemp2, Hashlistencrypted);
                using (
                    var rd =
                        new StreamReader(File.Open(Hashtemp2, FileMode.Open, FileAccess.Read, FileShare.Read),
                            Encoding.UTF8, false, 1024))
                {
                    string line;
                    int index;
                    while ((line = rd.ReadLine()) != null)
                    {
                        if (line.Length != 0)
                        {
                            if (line[0] == '#')
                            {
                                if (line.StartsWith("#Version ") && line.Length > 9)
                                {
                                    //^#Version ([\.0-9]+)(?: (.+))?$
                                    index = line.IndexOf(' ', 9);
                                    if (index > 9 && index < line.Length - 1)
                                    {
                                        author = line.Substring(index + 1).Trim();
                                    }
                                    else
                                    {
                                        index = line.Length;
                                    }
                                    version = line.Substring(9, index - 9).Trim();
                                }
                            }
                            else
                            {
                                index = line.IndexOf('=');
                                if (index > 0 && index < line.Length - 1 &&
                                    (filename = line.Substring(index + 1).Trim()).Length > 0)
                                {
                                    try
                                    {
                                        pairs.Add(Convert.ToUInt32(line.Substring(0, index).Trim(), 16), filename);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("HASHLIST: Failed to parse line \"{0}\"\n{1} {2}", line,
                                            e.GetType(), e.Message);
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion Loadexternal

                #region PrintInfoexternal

                if (printInfo)
                    if (File.Exists(Hashtemp2))
                    {

                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(@"Loaded HASHLIST file version {0}, created by {1} with {2} entries.",
                                (version.Length > 0 ? version : "?"), (author.Length > 0 ? "" + author + "" : ""),
                                pairs.Count);
                            Console.ResetColor();
                        }
                    }

                #endregion PrintInfoexternal
            }
        }

        public static string NameFromHash(uint hash)
        {
            if (pairs == null)
            {
                loadHashPairs();
            }
            string ret;
            if (!pairs.TryGetValue(hash, out ret) || ret.Length < 3)
            {
                ret = String.Format("@noname/{0:X8}.bin", hash);
            }
            return ret;
        }
    }

    internal static class Extensions
    {
        public static string FileName(this IDXFile.IDXEntry entry)
        {
            return HashPairs.NameFromHash(entry.Hash);
        }
    }
}