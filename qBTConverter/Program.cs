﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace qBTConverter
{
    class Program
    {
        static bool Match(ref byte[] data, ref byte[] search, int dataIndex)
        {
            if (data.Length <= dataIndex + search.Length)
            {
                return false;
            }

            for (int i = 0; i < search.Length; ++i)
            {
                if (data[dataIndex + i] != search[i])
                {
                    return false;
                }
            }

            return true;
        }

        static int Position(ref byte[] data, string findWhat, int startPos = 0)
        {
            byte[] search = Encoding.UTF8.GetBytes(findWhat);

            for (int i = startPos; i < (data.Length - search.Length); ++i)
            {
                if (Match(ref data, ref search, i))
                {
                    return i;
                }
            }
            return -1;
        }

        static byte[] Extract(ref byte[] data, int start, int end)
        {
            int size = end - start;
            if (size < 0)
            {
                return null;
            }

            byte[] buffer = new byte[size];
            for (int i = 0; i < size; ++i)
            {
                buffer[i] = data[start + i];
            }
            return buffer;
        }

        public static byte[] Combine(byte[] first, byte[] second, byte[] third)
        {
            byte[] ret = new byte[first.Length + second.Length + third.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, ret, first.Length + second.Length, third.Length);
            return ret;
        }

        static byte[] ReplaceBytes(ref byte[] data, int from, int to, byte[] with)
        {
            byte[] first = data.Take(from).ToArray();
            byte[] second = data.Skip(to).Take(data.Length - to).ToArray();

            return Combine(first, with, second);
        }

        static bool Replace(ref byte[] data, string which, string location, string replace)
        {
            int posA = Position(ref data, which);
            if (posA == -1)
            {
                return false;
            }

            int posB = posA + Encoding.UTF8.GetBytes(which).Length;
            int posC = Position(ref data, ":", posB);

            byte[] bLen = Extract(ref data, posB, posC);
            int len = int.Parse(Encoding.UTF8.GetString(bLen));

            byte[] bPath = Extract(ref data, posC + 1, posC + len + 1);
            string path = Encoding.UTF8.GetString(bPath);

            if (!path.Contains(location))
            {
                return false;
            }

            path = path.Replace(location, replace).Replace("\\", "/");

            byte[] rPath = Encoding.UTF8.GetBytes(path.Length + ":" + path);

            data = ReplaceBytes(ref data, posB, posC + len + 1, rPath);
            return true;
        }

        // :qBt-savePath<len>:<len chars>
        // :save_path<len>:<len chars>
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: qBTConverter <path> <windows base directory> <linux base directory>");
                return;
            }

            if (args[0][args[0].Length - 1] == '\\')
            {
                args[0].Remove(args[0].Length - 1);
            }

            Console.WriteLine("Using path \"" + args[0] + "\"");
            Console.WriteLine("Search \"" + args[1] + "\"");
            Console.WriteLine("Replace \"" + args[2] + "\" & \\ -> /");

            string[] files =
                Directory.GetFiles(args[0], "*.fastresume", SearchOption.TopDirectoryOnly);

            if (files.Length < 1)
            {
                Console.WriteLine("No .fastresume files found in path");
                return;
            }

            Console.WriteLine("Found " + files.Length + " fastresume files");

            string outpath = args[0] + "\\out";

            Directory.CreateDirectory(outpath);
            Console.WriteLine("Outputting results to \"" + outpath + "\"");

            int filesUpdated = 0;
            int totalOccurences = 0;
            foreach (string file in files)
            {
                Console.WriteLine("Processing \"" + file + "\"...");
                byte[] data = File.ReadAllBytes(file);
                string outFile = file.Replace(args[0], outpath);
                Console.WriteLine("\tSaving results to \"" + outFile + "\"...");

                int occurencesReplaced = 0;
                while (Replace(ref data, ":qBt-savePath", args[1], args[2]))
                {
                    ++occurencesReplaced;
                }

                while (Replace(ref data, ":save_path", args[1], args[2]))
                {
                    ++occurencesReplaced;
                }

                if (occurencesReplaced > 0)
                {
                    File.WriteAllBytes(outFile, data);
                    ++filesUpdated;
                    totalOccurences += occurencesReplaced;
                    Console.WriteLine("\tDone, replaced " + occurencesReplaced + " occurences");
                }
            }

            Console.WriteLine("Updated " + filesUpdated + " files and " + totalOccurences + " total occurences");
        }
    }
}