using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
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
        var flag = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("http"))
            {
                var url = args[i];
                if (url[url.Length - 1] == '/') url = url.Substring(0, url.Length - 1);
                _baseUrl = url;
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
                    _baseUrl = url;
                    flag = true;
                }
            }
        }

        // 根据操作系统，自动选择安装NET版本
        _menus["net"] = AutoInstallNet;

        _menus["net40"] = InstallNet40;
        _menus["net45"] = InstallNet45;
        _menus["net48"] = InstallNet48;

        _menus["net6"] = e => InstallNet6(e, null);
        _menus["net7"] = e => InstallNet7(e, null);
        _menus["net6-desktop"] = e => InstallNet6(e, "desktop");
        _menus["net7-desktop"] = e => InstallNet7(e, "desktop");
        _menus["net6-aspnet"] = e => InstallNet6(e, "aspnet");
        _menus["net7-aspnet"] = e => InstallNet7(e, "aspnet");

        _menus["md5"] = ShowMd5;

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

    private static Boolean Install(String fileName, String baseUrl, String arg = null, String hash = null)
    {
        Console.WriteLine("下载 {0}", fileName);

        // 检查已存在文件的MD5哈希，不正确则重新下载
        var fi = new FileInfo(fileName);
        if (fi.Exists && !String.IsNullOrEmpty(hash) && GetMD5(fileName) != hash)
        {
            fi.Delete();
            fi = null;
        }
        if (fi == null || !fi.Exists)
        {
            var url = $"{baseUrl}/{fileName}";
            Console.WriteLine("正在下载：{0}", url);
            var http = new WebClient();
            http.DownloadFile(url, fileName);
            Console.WriteLine("MD5: {0}", GetMD5(fileName));
        }

        if (String.IsNullOrEmpty(arg)) arg = "/passive /promptrestart";

        Console.WriteLine("正在安装：{0} {1}", fileName, arg);
        var p = Process.Start(fileName, arg);
        if (p.WaitForExit(600_000))
        {
            Console.WriteLine("安装完成！");
            Environment.ExitCode = 0;
            return true;
        }
        else
        {
            Console.WriteLine("安装超时！");
            Environment.ExitCode = 123;
            return false;
        }
    }

    static Version GetLast(IList<VerInfo> vers, String prefix)
    {
        var ver = new Version();
        if (vers.Count > 0)
        {
            Console.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                if (String.IsNullOrEmpty(prefix) || item.Name.StartsWith(prefix))
                {
                    var str = item.Name.Trim('v');
                    var p = str.IndexOf('-');
                    if (p > 0) str = str.Substring(0, p);
                    var v = new Version(str);
                    if (v > ver) ver = v;
                }

                Console.WriteLine(item.Name);
            }
            Console.WriteLine("");
        }

        return ver;
    }

    private static void InstallNet40(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本
        var target = new Version("4.0");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        Install("dotNetFx40_Full_x86_x64.exe", _baseUrl, null, "251743DFD3FDA414570524BAC9E55381");
    }

    private static void InstallNet45(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本
        var target = new Version("4.5");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        Install("NDP452-KB2901907-x86-x64-AllOS-ENU.exe", _baseUrl, null, "EE01FC4110C73A8E5EFC7CABDA0F5FF7");
        Install("NDP452-KB2901907-x86-x64-AllOS-CHS.exe", _baseUrl, null, "F0DE04B58842C30B8BE2D52F1DACF353");
    }

    private static void InstallNet48(String[] args)
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本。win10起支持4.8.1
        var osVer = Environment.OSVersion.Version;
        var target = osVer.Major >= 10 ? new Version("4.8.1") : new Version("4.8");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7)
            Install("Windows6.1-KB3063858-x64.msu", _baseUrl + "/win7", "/quiet /norestart", "6235547A9AC3D931843FE931C15F8E51");

        // win10/win11 中安装 .NET4.8.1
        if (osVer.Major >= 10)
        {
            Install("ndp481-x86-x64-allos-enu.exe", _baseUrl, "/passive /promptrestart /showfinalerror", "175C14084CEF7AE4DAC70BDDE804212F");
            Install("ndp481-x86-x64-allos-chs.exe", _baseUrl, "/passive /promptrestart /showfinalerror", "56EE95B0E0520A792099F7F3810FCCF4");
        }
        else
        {
            Install("ndp48-x86-x64-allos-enu.exe", _baseUrl, "/passive /promptrestart /showfinalerror", "AEBCB9FCAFA2BECF8BB30458A7E1F0A2");
            Install("ndp48-x86-x64-allos-chs.exe", _baseUrl, "/passive /promptrestart /showfinalerror", "048308B019CBB9C741354DC7FEA928B9");
        }
    }

    private static void InstallNet6(String[] args, String kind)
    {
        var vers = GetNetCore();

        var ver = GetLast(vers, "v6.0");

        // 目标版本
        var target = new Version("6.0.13");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

#if NET20
        var is64 = IntPtr.Size == 8;
#else
        var is64 = Environment.Is64BitOperatingSystem;
#endif

        // win7需要vc2019运行时
        var osVer = Environment.OSVersion.Version;
        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7)
        {
            if (is64)
            {
                Install("Windows6.1-KB3063858-x64.msu", _baseUrl + "/win7", "/quiet /norestart", "6235547A9AC3D931843FE931C15F8E51");
                Install("VC_redist.x64.exe", _baseUrl + "/vc2019", "/passive", "35431D059197B67227CD12F841733539");
            }
            else
            {
                Install("Windows6.1-KB3063858-x86.msu", _baseUrl + "/win7", "/quiet /norestart", "6D2B63B73E20DA5128490632995C4E65");
                Install("VC_redist.x86.exe", _baseUrl + "/vc2019", "/passive", "DD0232EE751164EAAD2FE0DE7158D77D");
            }
        }

        if (is64)
        {
            switch (kind)
            {
                case "aspnet":
                    Install("dotnet-hosting-6.0.13-win.exe", _baseUrl, null, "F583DE1A3597F4B6AAE8FCEF60801531");
                    break;
                case "desktop":
                    Install("windowsdesktop-runtime-6.0.13-win-x64.exe", _baseUrl, null, "7C37E8A464A8248889DADC710CC7585D");
                    break;
                default:
                    Install("dotnet-runtime-6.0.13-win-x64.exe", _baseUrl, null, "7CBDCB7E0AD6C186B7129497CF32D70B");
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                case "aspnet":
                    Install("dotnet-hosting-6.0.13-win.exe", _baseUrl, null, "F583DE1A3597F4B6AAE8FCEF60801531");
                    break;
                case "desktop":
                    Install("windowsdesktop-runtime-6.0.13-win-x86.exe", _baseUrl, null, "27E8E8FD587E5C3A3789105DD78D554E");
                    break;
                default:
                    Install("dotnet-runtime-6.0.13-win-x86.exe", _baseUrl, null, "6817C54EAB15B9ECD02A79FEC46FB09C");
                    break;
            }
        }
    }

    private static void InstallNet7(String[] args, String kind = null)
    {
        var vers = GetNetCore();

        var ver = GetLast(vers, null);

        // 目标版本
        var target = new Version("7.0.1");
        if (ver >= target)
        {
            Console.WriteLine("已安装最新版 v{0}", ver);
            return;
        }

#if NET20
        var is64 = IntPtr.Size == 8;
#else
        var is64 = Environment.Is64BitOperatingSystem;
#endif

        // win7需要vc2019运行时
        var osVer = Environment.OSVersion.Version;
        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7)
        {
            if (is64)
            {
                Install("Windows6.1-KB3063858-x64.msu", _baseUrl + "/win7", "/quiet /norestart", "6235547A9AC3D931843FE931C15F8E51");
                Install("VC_redist.x64.exe", _baseUrl + "/vc2019", "/passive", "35431D059197B67227CD12F841733539");
            }
            else
            {
                Install("Windows6.1-KB3063858-x86.msu", _baseUrl + "/win7", "/quiet /norestart", "6D2B63B73E20DA5128490632995C4E65");
                Install("VC_redist.x86.exe", _baseUrl + "/vc2019", "/passive", "DD0232EE751164EAAD2FE0DE7158D77D");
            }
        }

        if (is64)
        {
            switch (kind)
            {
                case "aspnet":
                    //Install("dotnet-runtime-7.0.1-win-x64.exe", _baseUrl, null, "A2C4819E0D689B84A3291C3D391402F8");
                    //Install("aspnetcore-runtime-7.0.1-win-x64.exe", _baseUrl, null, "C6F6A84EA2F306C9DA8BBA9B85522BAD");
                    Install("dotnet-hosting-7.0.1-win.exe", _baseUrl, null, "3809855004F80E0AD58335E9122B29FF");
                    break;
                case "desktop":
                    Install("windowsdesktop-runtime-7.0.1-win-x64.exe", _baseUrl, null, "28CB0F04EE3DE71E5ED1E6B2A3DB89B8");
                    break;
                default:
                    Install("dotnet-runtime-7.0.1-win-x64.exe", _baseUrl, null, "A2C4819E0D689B84A3291C3D391402F8");
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                case "aspnet":
                    //Install("dotnet-runtime-7.0.1-win-x86.exe", _baseUrl, null, "CF2F21C5374A1B87532474F3900EFFF5");
                    //Install("aspnetcore-runtime-7.0.1-win-x86.exe", _baseUrl, null, "0DB188A73A6D9BA6116C0D80791A7E4A");
                    Install("dotnet-hosting-7.0.1-win.exe", _baseUrl, null, "3809855004F80E0AD58335E9122B29FF");
                    break;
                case "desktop":
                    Install("windowsdesktop-runtime-7.0.1-win-x86.exe", _baseUrl, null, "3D111CD0C48A72953788E06E3084D937");
                    break;
                default:
                    Install("dotnet-runtime-7.0.1-win-x86.exe", _baseUrl, null, "CF2F21C5374A1B87532474F3900EFFF5");
                    break;
            }
        }
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

        var dir = "";
        if (Environment.OSVersion.Platform <= PlatformID.WinCE)
        {
            dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (String.IsNullOrEmpty(dir)) return null;
            dir += "\\dotnet\\shared";
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            dir = "/usr/share/dotnet/shared";

        var dic = new SortedDictionary<String, VerInfo>();
        var di = new DirectoryInfo(dir);
        if (di.Exists)
        {
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
        }

        foreach (var item in dic)
        {
            list.Add(item.Value);
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

    private static String GetMD5(String fileName)
    {
        var fi = new FileInfo(fileName);
        var md5 = MD5.Create();
        using var fs = fi.OpenRead();
        var buf = md5.ComputeHash(fs);
        var hex = BitConverter.ToString(buf).Replace("-", null);

        return hex;
    }

    private static void ShowMd5(String[] args)
    {
        var pt = "*.*";
        if (args != null && args.Length >= 2) pt = args[1];
        //Console.WriteLine("pt={0}", pt);

        var di = new DirectoryInfo("./");
        foreach (var fi in di.GetFiles(pt))
        {
            Console.WriteLine("{0}\t{1}", fi.Name, GetMD5(fi.FullName));
        }
    }
}