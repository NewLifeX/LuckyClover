using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace LuckyClover;

#if NET20
delegate void Action();
#endif

internal class Program
{
    private static readonly Dictionary<String, Action> _menus = new(StringComparer.OrdinalIgnoreCase);
    private const String _baseUrl = "http://x.newlifex.com/dotnet";

    private static void Main(String[] args)
    {
        //XTrace.UseConsole();

        var asm = Assembly.GetEntryAssembly();
        Console.WriteLine("幸运草 LuckyClover v{0}", asm.GetName().Version);
        //Console.WriteLine("无依赖编译为linux-arm/linux-x86/windows，用于自动安装主流.NET运行时");
#if NETFRAMEWORK
        var atts = asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
        Console.WriteLine((atts[0] as AssemblyDescriptionAttribute).Description);
#else
        Console.WriteLine(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
#endif
        Console.WriteLine("{0}", Environment.OSVersion);
        Console.WriteLine();

        var net = new NetRuntime
        {
            BaseUrl = _baseUrl,
            Hashs = NetRuntime.LoadMD5s(),
        };

        // 读取命令行，一般放在最后
        var flag = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("http"))
            {
                var url = args[i];
                if (url[url.Length - 1] == '/') url = url.Substring(0, url.Length - 1);
                net.BaseUrl = url;
                flag = true;

                break;
            }
        }
        // 读取本目录 server.txt
        if (!flag)
        {
            var f = Path.GetFullPath("server.txt");
            if (File.Exists(f))
            {
                var url = File.ReadAllText(f).Trim();
                if (!String.IsNullOrEmpty(url))
                {
                    if (url[url.Length - 1] == '/') url = url.Substring(0, url.Length - 1);
                    if (!url.EndsWith("/dotnet", false, null)) url += "/dotnet";
                    net.BaseUrl = url;
                    flag = true;
                }
            }
        }
        // 读取静默安装标记
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-silent", StringComparison.InvariantCultureIgnoreCase))
            {
                net.Silent = true;

                break;
            }
        }

        // 根据操作系统，自动选择安装NET版本
        _menus["net"] = net.AutoInstallNet;

        _menus["net45"] = net.InstallNet45;
        _menus["net48"] = net.InstallNet48;
        _menus["net40"] = net.InstallNet40;

        _menus["net6"] = () => net.InstallNet6(null);
        _menus["net6-desktop"] = () => net.InstallNet6("desktop");
        _menus["net6-aspnet"] = () => net.InstallNet6("aspnet");
        _menus["net6-host"] = () => net.InstallNet6("host");
        _menus["net7"] = () => net.InstallNet7(null);
        _menus["net7-desktop"] = () => net.InstallNet7("desktop");
        _menus["net7-aspnet"] = () => net.InstallNet7("aspnet");
        _menus["net7-host"] = () => net.InstallNet7("host");

        _menus["md5"] = () => ShowMd5(args);
#if NET45_OR_GREATER || NETCOREAPP
        _menus["zip"] = () => Zip(args);
        _menus["unzip"] = () => Unzip(args);
#endif

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];
        if (String.IsNullOrEmpty(cmd)) cmd = ShowMenu();

        if (_menus.TryGetValue(cmd, out var func))
            func();
        else
            Console.WriteLine("无法识别命令：{0}", cmd);
    }

    private static String ShowMenu()
    {
        var vers = new List<VerInfo>();
        if (Environment.OSVersion.Platform <= PlatformID.WinCE)
        {
            vers.AddRange(NetRuntime.Get1To45VersionFromRegistry());
            vers.AddRange(NetRuntime.Get45PlusFromRegistry());
        }
        vers.AddRange(NetRuntime.GetNetCore());

        Console.WriteLine("已安装版本：");
        foreach (var item in vers)
        {
            if (String.IsNullOrEmpty(item.Sp))
                Console.WriteLine("{0,-10} {1}", item.Name, item.Version);
            else
                Console.WriteLine("{0,-10} {1} Sp{2}", item.Name, item.Version, item.Sp);
        }
        Console.WriteLine("");

        Console.WriteLine("命令：clover");
        Console.WriteLine("运行时：{0}", Environment.Version);
        Console.WriteLine("");

        var ms = new String[_menus.Count];
        _menus.Keys.CopyTo(ms, 0);
        Console.WriteLine("可用命令：{0}", String.Join(", ", ms));

        var line = Console.ReadLine()?.Trim();

        return line;
    }

    private static void ShowMd5(String[] args)
    {
        var pt = "*.*";
        if (args != null && args.Length >= 2) pt = args[1];
        //Console.WriteLine("pt={0}", pt);

        var di = new DirectoryInfo("./");
        foreach (var fi in di.GetFiles(pt))
        {
            Console.WriteLine("{0}\t{1}", fi.Name, NetRuntime.GetMD5(fi.FullName));
        }
    }

#if NET45_OR_GREATER || NETCOREAPP
    private static void Zip(String[] args)
    {
        if (args == null || args.Length < 3) return;

        if (args.Length == 3 && !args[2].Contains("*"))
        {
            var dst = args[1];
            var src = args[2];

            Console.WriteLine("Zip压缩 {0} 到 {1}", src, dst);

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

            Console.WriteLine("Zip压缩多个文件到 {0}", dst);

            //if (File.Exists(dst)) File.Delete(dst);

            var compressionLevel = CompressionLevel.SmallestSize;
            using var zip = ZipFile.Open(dst, ZipArchiveMode.Create);

            // 遍历多个目录或文件
            for (var i = 2; i < args.Length; i++)
            {
                // 分离路径中的目录和文件掩码
                var src = args[i];
                var pt = "*";
                var p = src.LastIndexOfAny(new[] { '/', '\\' });
                if (p > 0)
                {
                    pt = src.Substring(p + 1);
                    src = src.Substring(0, p);
                }

                var di = new DirectoryInfo(src);
                var fullName = di.FullName;
                Console.WriteLine("压缩目录：{0} 匹配：{1}", fullName, pt);

                // 没有匹配项时，该路径作为一个子目录
                if (String.IsNullOrEmpty(pt))
                {
                    fullName = di.Parent.FullName;

                    var length = di.FullName.Length - fullName.Length;
                    var entryName2 = EntryFromPath(di.FullName, fullName.Length, length, true);
                    zip.CreateEntry(entryName2);
                    Console.WriteLine("\t添加目录：{0}", entryName2);
                }

                // 遍历所有文件
                foreach (var fi in di.EnumerateFileSystemInfos(pt, SearchOption.AllDirectories))
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

    private static void Unzip(String[] args)
    {
        if (args == null || args.Length < 3) return;

        var src = args[1];
        var dst = args[2];

        Console.WriteLine("UnZip解压缩 {0} 到 {1}", src, dst);

#if NET45_OR_GREATER
        ZipFile.ExtractToDirectory(src, dst);
#else
        ZipFile.ExtractToDirectory(src, dst, true);
#endif
    }
#endif
}