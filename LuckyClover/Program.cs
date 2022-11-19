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
    private static String _baseUrl = "http://x.newlifex.com/dotnet";

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

        // 读取命令行
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("http"))
            {
                _baseUrl = args[i];
                if (_baseUrl[_baseUrl.Length - 1] == '/') _baseUrl = _baseUrl.Substring(0, _baseUrl.Length - 1);

                break;
            }
        }

        // 根据操作系统，自动选择安装NET版本
        _menus["net"] = AutoInstallNet;

        _menus["net40"] = InstallNet40;
        _menus["net45"] = InstallNet45;
        _menus["net48"] = InstallNet48;

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
        var vers = new List<VerInfo>();
        if (Environment.OSVersion.Platform <= PlatformID.WinCE)
        {
            vers.AddRange(Get1To45VersionFromRegistry());
            vers.AddRange(Get45PlusFromRegistry());
        }
        vers.AddRange(GetNetCore());

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

    private static void AutoInstallNet(String[] args)
    {
        var osVer = Environment.OSVersion.Version;

        // WinXP
        if (osVer.Major <= 5)
            InstallNet40(args);
        // Vista
        else if (osVer.Major == 6 && osVer.Minor == 0)
            InstallNet45(args);
        else if (osVer.Major == 6 && osVer.Minor == 1)
        {
            // Win7
            if (osVer.Revision <= 7600)
                InstallNet45(args);
            else
            // Win7Sp1
            {
                InstallNet48(args);
                InstallNet7(args);
            }
        }
        // Win10/Win11
        else if (osVer.Major >= 10)
        {
            InstallNet7(args);
        }
        else
        {
            InstallNet48(args);
            InstallNet7(args);
        }
    }

    private static Boolean Install(String fileName, String baseUrl, String arg = null)
    {
        Console.WriteLine("下载 {0}", fileName);

        if (!File.Exists(fileName))
        {
            var url = $"{baseUrl}/{fileName}";
            Console.WriteLine("正在下载：{0}", url);
            var http = new WebClient();
            http.DownloadFile(url, fileName);
        }

        if (String.IsNullOrEmpty(arg)) arg = "/passive /promptrestart";

        Console.WriteLine("正在安装：{0} {1}", fileName, arg);
        var p = Process.Start(fileName, arg);
        if (p.WaitForExit(600_000))
        {
            Console.WriteLine("安装完成！");
            return true;
        }
        else
        {
            Console.WriteLine("安装超时！");
            return true;
        }
    }

    private static void InstallNet40(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Version);
                if (v > ver) ver = v;

                Console.WriteLine(item.Name);
            }
            Console.WriteLine("");
        }

        // 目标版本
        var target = new Version("4.0");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        Install("dotNetFx40_Full_x86_x64.exe", _baseUrl);
    }

    private static void InstallNet45(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Version);
                if (v > ver) ver = v;

                Console.WriteLine(item.Name);
            }
            Console.WriteLine("");
        }

        // 目标版本
        var target = new Version("4.5");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        Install("NDP452-KB2901907-x86-x64-AllOS-ENU.exe", _baseUrl);
        Install("NDP452-KB2901907-x86-x64-AllOS-CHS.exe", _baseUrl);
    }

    private static void InstallNet48(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Version);
                if (v > ver) ver = v;

                Console.WriteLine(item.Name);
            }
            Console.WriteLine("");
        }

        // 目标版本。win10起支持4.8.1
        var osVer = Environment.OSVersion.Version;
        var target = osVer.Major >= 10 ? new Version("4.8.1") : new Version("4.8");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;

        if (osVer.Major >= 10)
        {
            if (isWin7)
                Install("Windows6.1-KB3063858-x64.msu", _baseUrl + "/win7", "/quiet /promptrestart");
            Install("ndp481-x86-x64-allos-enu.exe", _baseUrl, "/passive /promptrestart /showfinalerror");
            Install("ndp481-x86-x64-allos-chs.exe", _baseUrl, "/passive /promptrestart /showfinalerror");
        }
        else
        {
            if (isWin7)
                Install("Windows6.1-KB3063858-x64.msu", _baseUrl + "/win7", "/quiet /promptrestart");
            Install("ndp48-x86-x64-allos-enu.exe", _baseUrl, "/passive /promptrestart /showfinalerror");
            Install("ndp48-x86-x64-allos-chs.exe", _baseUrl, "/passive /promptrestart /showfinalerror");
        }
    }

    private static void InstallNet6(String[] args)
    {
        var vers = GetNetCore();

        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Version);
                if (v > ver) ver = v;

                Console.WriteLine(item.Name);
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

        Install("dotnet-runtime-6.0.10-win-x64.exe", _baseUrl);
    }

    private static void InstallNet7(String[] args)
    {
        var vers = GetNetCore();

        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                var v = new Version(item.Version);
                if (v > ver) ver = v;

                Console.WriteLine(item.Name);
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

        Install("dotnet-runtime-7.0.0-win-x64.exe", _baseUrl);
    }

    private static IList<VerInfo> Get1To45VersionFromRegistry()
    {
        var list = new List<VerInfo>();
#if !NETCOREAPP3_1
        // 注册表查找 .NET Framework
        using var ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");

        foreach (var versionKeyName in ndpKey.GetSubKeyNames())
        {
            // 跳过 .NET Framework 4.5
            if (versionKeyName == "v4") continue;
            if (!versionKeyName.StartsWith("v")) continue;

            var versionKey = ndpKey.OpenSubKey(versionKeyName);
            // 获取 .NET Framework 版本
            var ver = (String)versionKey.GetValue("Version", "");
            // 获取SP数字
            var sp = versionKey.GetValue("SP", "").ToString();

            if (!String.IsNullOrEmpty(ver))
            {
                // 获取 installation flag, or an empty string if there is none.
                var install = versionKey.GetValue("Install", "").ToString();
                if (String.IsNullOrEmpty(install)) // No install info; it must be in a child subkey.
                    list.Add(new VerInfo { Name = versionKeyName, Version = ver, Sp = sp });
                else if (!String.IsNullOrEmpty(sp) && install == "1")
                    list.Add(new VerInfo { Name = versionKeyName, Version = ver, Sp = sp });
            }
            else
            {
                foreach (var subKeyName in versionKey.GetSubKeyNames())
                {
                    var subKey = versionKey.OpenSubKey(subKeyName);
                    ver = (String)subKey.GetValue("Version", "");
                    if (!String.IsNullOrEmpty(ver))
                    {
                        var name = ver;
                        while (name.Length > 3 && name.Substring(name.Length - 2) == ".0")
                            name = name.Substring(0, name.Length - 2);
                        if (name[0] != 'v') name = 'v' + name;
                        sp = subKey.GetValue("SP", "").ToString();

                        var install = subKey.GetValue("Install", "").ToString();
                        if (String.IsNullOrEmpty(install)) //No install info; it must be later.
                            list.Add(new VerInfo { Name = name, Version = ver, Sp = sp });
                        else if (!String.IsNullOrEmpty(sp) && install == "1")
                            list.Add(new VerInfo { Name = name, Version = ver, Sp = sp });
                        else if (install == "1")
                            list.Add(new VerInfo { Name = name, Version = ver, Sp = sp });
                    }
                }
            }
        }
#endif

        return list;
    }

    private static IList<VerInfo> Get45PlusFromRegistry()
    {
        var list = new List<VerInfo>();
#if !NETCOREAPP3_1
        const String subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        using var ndpKey = Registry.LocalMachine.OpenSubKey(subkey);

        if (ndpKey == null) return list;

        //First check if there's an specific version indicated
        var name = "";
        var value = "";
        var ver = ndpKey.GetValue("Version");
        if (ver != null) name = ver.ToString();
        var release = ndpKey.GetValue("Release");
        if (release != null)
            value = CheckFor45PlusVersion((Int32)ndpKey.GetValue("Release"));

        if (String.IsNullOrEmpty(name)) name = value;
        if (String.IsNullOrEmpty(value)) value = name;
        if (!String.IsNullOrEmpty(name)) list.Add(new VerInfo { Name = "v" + value, Version = name });

        // Checking the version using >= enables forward compatibility.
        static String CheckFor45PlusVersion(Int32 releaseKey) => releaseKey switch
        {
            >= 533325 => "4.8.1",
            >= 528040 => "4.8",
            >= 461808 => "4.7.2",
            >= 461308 => "4.7.1",
            >= 460798 => "4.7",
            >= 394802 => "4.6.2",
            >= 394254 => "4.6.1",
            >= 393295 => "4.6",
            >= 379893 => "4.5.2",
            >= 378675 => "4.5.1",
            >= 378389 => "4.5",
            _ => ""
        };
#endif

        return list;
    }

    private static IList<VerInfo> GetNetCore()
    {
        var list = new List<VerInfo>();

        if (Environment.OSVersion.Platform <= PlatformID.WinCE)
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (String.IsNullOrEmpty(dir)) return list;

            dir += "\\dotnet\\shared";
            if (!Directory.Exists(dir)) return list;

            var dic = new SortedDictionary<String, VerInfo>();
            var di = new DirectoryInfo(dir);
            foreach (var item in di.GetDirectories())
            {
                foreach (var elm in item.GetDirectories())
                {
                    var name = "v" + elm.Name;
                    if (!dic.ContainsKey(name))
                    {
                        dic.Add(name, new VerInfo { Name = name, Version = elm.Name });
                    }
                }
            }

            foreach (var item in dic)
            {
                list.Add(item.Value);
            }
        }

        // 通用处理
        {
            var infs = Execute("dotnet", "--list-runtimes")?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (infs != null)
            {
                foreach (var line in infs)
                {
                    var ss = line.Split(' ');
                    if (ss.Length >= 2)
                    {
                        var name = "v" + ss[1];
                        var ver = $"{ss[0]} {ss[1]}";

                        VerInfo vi = null;
                        foreach (var item in list)
                        {
                            if (item.Name == name)
                            {
                                vi = item;
                                break;
                            }
                        }
                        if (vi == null)
                        {
                            vi = new VerInfo { Name = name, Version = ver };
                            list.Add(vi);
                        }

                        if (vi.Version.Length < ver.Length) vi.Version = ver;
                    }
                }
            }
        }

        return list;
    }

    private static String Execute(String cmd, String arguments = null)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, arguments)
            {
                // UseShellExecute 必须 false，以便于后续重定向输出流
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                //RedirectStandardError = true,
            };
            var process = Process.Start(psi);
            if (!process.WaitForExit(3_000))
            {
                process.Kill();
                return null;
            }

            return process.StandardOutput.ReadToEnd();
        }
        catch { return null; }
    }
}