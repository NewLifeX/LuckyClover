﻿#if NET45_OR_GREATER || NETCOREAPP
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LuckyClover;

internal class ZipCommand
{
    public void Compress(String[] args)
    {
        if (args == null || args.Length < 3) return;

        if (args.Length == 3 && !args[2].Contains("*"))
        {
            var dst = args[1];
            var src = args[2];

            Log.WriteLine("Zip压缩 \e[31;1m{0}\e[0m 到 \e[31;1m{1}\e[0m", src, dst);

            if (File.Exists(dst)) File.Delete(dst);

#if NET7_0_OR_GREATER
            ZipFile.CreateFromDirectory(src, dst, CompressionLevel.SmallestSize, false);
#else
            ZipFile.CreateFromDirectory(src, dst, CompressionLevel.Optimal, false);
#endif
        }
        else
        {
#if NET7_0_OR_GREATER
            var dst = args[1];
            var root = Environment.CurrentDirectory;

            Log.WriteLine("Zip压缩多个文件到 \e[31;1m{0}\e[0m", dst);
            Log.WriteLine("当前工作目录 \e[31;1m{0}\e[0m", root);

            if (File.Exists(dst)) File.Delete(dst);

            // -r 递归压缩
            var recurse = false;

            var compressionLevel = CompressionLevel.SmallestSize;
            using var zip = ZipFile.Open(dst, ZipArchiveMode.Create);

            // 遍历多个目录或文件
            for (var i = 2; i < args.Length; i++)
            {
                // 当前位置是 -r 参数，表示后续递归压缩
                if (args[i] == "-r")
                {
                    recurse = true;
                    continue;
                }
                if (args[i] == "-R")
                {
                    recurse = false;
                    continue;
                }
                if (args[i] == "-d")
                {
                    root = Path.GetFullPath(args[++i]);
                    Log.WriteLine("当前工作目录 \e[31;1m{0}\e[0m", root);
                    continue;
                }

                // 分离路径中的目录和文件掩码
                var src = args[i];
                var pt = "*";
                var p = src.LastIndexOfAny(['/', '\\']);
                if (p > 0)
                {
                    pt = src.Substring(p + 1);
                    src = src.Substring(0, p);
                }
                else if (src.Contains('*') || !Directory.Exists(src))
                {
                    pt = src;
                    src = ".";
                }

                var di = new DirectoryInfo(Path.Combine(root, src));
                var fullName = di.FullName;
                Log.WriteLine("压缩目录：\e[32;1m{0}\e[0m 匹配：\e[32;1m{1}\e[0m", fullName, pt);

                // 如果fullName是当前工作目录的子目录，则以当前工作目录为根目录
                if (fullName.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    fullName = root;
                }

                // 没有匹配项时，该路径作为一个子目录
                if (String.IsNullOrEmpty(pt))
                {
                    //fullName = di.Parent.FullName;

                    var length = di.FullName.Length - fullName.Length;
                    var entryName2 = EntryFromPath(di.FullName, fullName.Length, length, true);
                    zip.CreateEntry(entryName2);
                    Console.WriteLine("\t添加目录：{0}", entryName2);
                }

                // 遍历所有文件
                var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var fi in di.EnumerateFileSystemInfos(pt, searchOption))
                {
                    var length = fi.FullName.Length - fullName.Length;
                    if (fi is FileInfo)
                    {
                        var entryName = EntryFromPath(fi.FullName, fullName.Length, length, false);
                        zip.CreateEntryFromFile(fi.FullName, entryName, compressionLevel);
                        Console.WriteLine("\t添加文件：{0}", entryName);
                        continue;
                    }
                    if (fi is DirectoryInfo di2 && IsDirEmpty(di2))
                    {
                        var entryName2 = EntryFromPath(fi.FullName, fullName.Length, length, true);
                        zip.CreateEntry(entryName2);
                        Console.WriteLine("\t添加目录：{0}", entryName2);
                    }
                }
            }
#endif
        }
    }

    static String EntryFromPath(String entry, Int32 offset, Int32 length, Boolean appendPathSeparator = false)
    {
        while (length > 0 && (entry[offset] == Path.DirectorySeparatorChar || entry[offset] == Path.AltDirectorySeparatorChar))
        {
            offset++;
            length--;
        }
        if (length == 0) return !appendPathSeparator ? String.Empty : "/";

        var num = appendPathSeparator ? (length + 1) : length;
        var buffer = new Char[num];
        entry.CopyTo(offset, buffer, 0, length);
        for (var i = 0; i < length; i++)
        {
            var c = buffer[i];
            if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
            {
                buffer[i] = '/';
            }
        }
        if (appendPathSeparator)
        {
            buffer[length] = '/';
        }
        return new String(buffer, 0, num);
    }

    static Boolean IsDirEmpty(DirectoryInfo possiblyEmptyDir)
    {
        using var enumerator = Directory.EnumerateFileSystemEntries(possiblyEmptyDir.FullName).GetEnumerator();
        return !enumerator.MoveNext();
    }

    public void Extract(String[] args)
    {
        if (args == null || args.Length < 3) return;

        var src = args[1];
        var dst = args[2];

        Console.WriteLine("UnZip解压缩 \e[31;1m{0}\e[0m 到 \e[31;1m{1}\e[0m", src, dst);

#if NET45_OR_GREATER
        ZipFile.ExtractToDirectory(src, dst);
#else
        ZipFile.ExtractToDirectory(src, dst, true);
#endif
    }
}
#endif
