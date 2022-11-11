using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Microsoft.Win32;

namespace LuckyClover;

internal class Program
{
    private static readonly Dictionary<String, Action<String[]>> _menus = new(StringComparer.OrdinalIgnoreCase);

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
        Console.WriteLine();

        _menus["net48"] = InstallNet48;
        _menus["net"] = InstallNet48;

        _menus["net6"] = InstallNet6;
        _menus["net7"] = InstallNet7;

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];
        if (String.IsNullOrEmpty(cmd)) cmd = ShowMenu();

        if (_menus.TryGetValue(cmd, out var func))
            func(args);
        else
            Console.WriteLine("无法识别命令：{0}", cmd);
    }

    private static String ShowMenu()
    {
        var vers = new Dictionary<String, String>();
        Get1To45VersionFromRegistry(vers);
        Get45PlusFromRegistry(vers);
        GetNetCore(vers);
        Console.WriteLine("已安装版本：");
        foreach (var item in vers)
        {
            Console.WriteLine("{0}\t{1}", item.Key, item.Value);
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

    private static void InstallNet48(String[] args)
    {
        var ver = new Version();
        var vers = new Dictionary<String, String>();
        Get1To45VersionFromRegistry(vers);
        Get45PlusFromRegistry(vers);
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Key);
                if (v > ver) ver = v;

                Console.WriteLine(item.Value);
            }
            Console.WriteLine("");
        }

        // 目标版本
        var target = new Version("4.8");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];

        // 检查是否已安装.NET
        Console.WriteLine("InstallNet48 {0}", cmd);

        var url = "http://x.newlifex.com/dotnet/ndp481-x86-x64-allos-enu.exe";
        var fileName = Path.GetFileName(url);
        if (!File.Exists(fileName))
        {
            Console.WriteLine("正在下载：{0}", url);
            var http = new WebClient();
            http.DownloadFile(url, fileName);
        }

        Console.WriteLine("正在安装：{0}", fileName);
        var p = Process.Start(fileName, "/passive");
        if (p.WaitForExit(15_000))
        {
            Console.WriteLine("安装成功！");
            Environment.ExitCode = 0;
        }
        else
        {
            Console.WriteLine("安装超时！");
            Environment.ExitCode = 1;
        }
    }

    private static void InstallNet6(String[] args)
    {
        var ver = new Version();
        var vers = new Dictionary<String, String>();
        GetNetCore(vers);
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Key);
                if (v > ver) ver = v;

                Console.WriteLine(item.Value);
            }
            Console.WriteLine("");
        }

        // 目标版本
        var target = new Version("6.0.10");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];

        // 检查是否已安装.NET运行时
        Console.WriteLine("InstallNet6 {0}", args[0]);

        var url = "http://x.newlifex.com/dotnet/dotnet-runtime-6.0.10-win-x64.exe";
        var fileName = Path.GetFileName(url);
        if (!File.Exists(fileName))
        {
            Console.WriteLine("正在下载：{0}", url);
            var http = new WebClient();
            http.DownloadFile(url, fileName);
        }

        Console.WriteLine("正在安装：{0}", fileName);
        var p = Process.Start(fileName, "/passive");
        if (p.WaitForExit(15_000))
        {
            Console.WriteLine("安装成功！");
            Environment.ExitCode = 0;
        }
        else
        {
            Console.WriteLine("安装超时！");
            Environment.ExitCode = 1;
        }
    }

    private static void InstallNet7(String[] args)
    {
        var ver = new Version();
        var vers = new Dictionary<String, String>();
        GetNetCore(vers);
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Key);
                if (v > ver) ver = v;

                Console.WriteLine(item.Value);
            }
            Console.WriteLine("");
        }

        // 目标版本
        var target = new Version("7.0.0");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];

        // 检查是否已安装.NET运行时
        Console.WriteLine("InstallNet7 {0}", args[0]);

        var url = "http://x.newlifex.com/dotnet/dotnet-runtime-7.0.0-win-x64.exe";
        var fileName = Path.GetFileName(url);
        if (!File.Exists(fileName))
        {
            Console.WriteLine("正在下载：{0}", url);
            var http = new WebClient();
            http.DownloadFile(url, fileName);
        }

        Console.WriteLine("正在安装：{0}", fileName);
        var p = Process.Start(fileName, "/passive");
        if (p.WaitForExit(15_000))
        {
            Console.WriteLine("安装成功！");
            Environment.ExitCode = 0;
        }
        else
        {
            Console.WriteLine("安装超时！");
            Environment.ExitCode = 1;
        }
    }

    private static void Get1To45VersionFromRegistry(IDictionary<String, String> dic)
    {
        // Opens the registry key for the .NET Framework entry.
        using var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");

        foreach (var versionKeyName in ndpKey.GetSubKeyNames())
        {
            // Skip .NET Framework 4.5 version information.
            if (versionKeyName == "v4") continue;
            if (!versionKeyName.StartsWith("v")) continue;

            var versionKey = ndpKey.OpenSubKey(versionKeyName);
            // Get the .NET Framework version value.
            var name = (String)versionKey.GetValue("Version", "");
            // Get the service pack (SP) number.
            var sp = versionKey.GetValue("SP", "").ToString();

            if (!String.IsNullOrEmpty(name))
            {
                // Get the installation flag, or an empty string if there is none.
                var install = versionKey.GetValue("Install", "").ToString();
                if (String.IsNullOrEmpty(install)) // No install info; it must be in a child subkey.
                    dic.Add(name, name);
                else if (!String.IsNullOrEmpty(sp) && install == "1")
                    dic.Add(name, $"{name} sp{sp}");
            }
            else
            {
                foreach (var subKeyName in versionKey.GetSubKeyNames())
                {
                    var subKey = versionKey.OpenSubKey(subKeyName);
                    name = (String)subKey.GetValue("Version", "");
                    if (!String.IsNullOrEmpty(name))
                    {
                        sp = subKey.GetValue("SP", "").ToString();

                        var install = subKey.GetValue("Install", "").ToString();
                        if (String.IsNullOrEmpty(install)) //No install info; it must be later.
                            dic.Add(name, name);
                        else if (!String.IsNullOrEmpty(sp) && install == "1")
                            dic.Add(name, $"{name} sp{sp}");
                        else if (install == "1")
                            dic.Add(name, name);
                    }
                }
            }
        }
    }

    private static void Get45PlusFromRegistry(IDictionary<String, String> dic)
    {
        const String subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using var ndpKey = Registry.LocalMachine.OpenSubKey(subkey);

        if (ndpKey == null) return;

        //First check if there's an specific version indicated
        var name = "";
        var value = "";
        var ver = ndpKey.GetValue("Version");
        if (ver != null)
            name = ver.ToString();
        var release = ndpKey.GetValue("Release");
        if (release != null)
            value = CheckFor45PlusVersion((Int32)ndpKey.GetValue("Release"));

        if (String.IsNullOrEmpty(name)) name = value;
        if (String.IsNullOrEmpty(value)) value = name;
        if (!String.IsNullOrEmpty(name)) dic.Add(name, value);

        // Checking the version using >= enables forward compatibility.
        String CheckFor45PlusVersion(Int32 releaseKey)
        {
            if (releaseKey >= 533325)
                return "4.8.1";
            if (releaseKey >= 528040)
                return "4.8";
            if (releaseKey >= 461808)
                return "4.7.2";
            if (releaseKey >= 461308)
                return "4.7.1";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if (releaseKey >= 379893)
                return "4.5.2";
            if (releaseKey >= 378675)
                return "4.5.1";
            if (releaseKey >= 378389)
                return "4.5";
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "";
        }
    }

    private static void GetNetCore(IDictionary<String, String> dic)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (String.IsNullOrEmpty(dir)) return;

        dir += "\\dotnet\\shared";
        if (!Directory.Exists(dir)) return;

        var di = new DirectoryInfo(dir);
        foreach (var item in di.GetDirectories())
        {
            foreach (var elm in item.GetDirectories())
            {
                dic[elm.Name] = elm.Name;
            }
        }
    }
}