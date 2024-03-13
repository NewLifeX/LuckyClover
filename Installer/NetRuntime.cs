using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceProcess;
using Microsoft.Win32;
using NewLife;
using NewLife.Log;

namespace Installer;

public class NetRuntime
{
    #region 属性
    public String BaseUrl { get; set; }

    public Boolean Silent { get; set; }

    /// <summary>应用安装目录</summary>
    public String InstallPath { get; set; }

    /// <summary>缓存目录</summary>
    public String CachePath { get; set; } = "../Cache";

    /// <summary>是否强制。如果true，则已安装版本存在也强制安装。默认false</summary>
    public Boolean Force { get; set; }

    public IDictionary<String, String> Hashs { get; set; }

    public ITracer Tracer { get; set; }
    #endregion

    #region 核心方法
    public Boolean Install(String fileName, String baseUrl = null, String arg = null)
    {
        using var span = Tracer?.NewSpan($"Install-{Path.GetFileNameWithoutExtension(fileName)}", new { fileName, baseUrl });

        XTrace.WriteLine("下载 {0}", fileName);

        var fullFile = fileName;
        if (!String.IsNullOrEmpty(CachePath)) fullFile = Path.Combine(CachePath, fileName);

        var hash = "";
        if (Hashs != null && !Hashs.TryGetValue(fileName, out hash)) hash = null;

        // 检查已存在文件的MD5哈希，不正确则重新下载
        var fi = new FileInfo(fullFile);
        if (fi.Exists && fi.Length < 1024 && !String.IsNullOrEmpty(hash) && GetMD5(fullFile) != hash)
        {
            fi.Delete();
            fi = null;
        }
        if (fi == null || !fi.Exists)
        {
            if (String.IsNullOrEmpty(baseUrl))
                baseUrl = BaseUrl;
            else
                baseUrl = BaseUrl + baseUrl;

            var url = $"{baseUrl}/{fileName}";
            XTrace.WriteLine("正在下载：{0}", url);

            fullFile.EnsureDirectory(true);

            var http = new WebClient();
            http.DownloadFile(url, fullFile);
            XTrace.WriteLine("MD5: {0}", GetMD5(fullFile));
        }

        // 解压缩
        if (fileName.EndsWithIgnoreCase(".zip"))
        {
            XTrace.WriteLine("正在解压缩到：{0}", InstallPath);

            fullFile.AsFile().Extract(InstallPath.EnsureDirectory(false), true);

            return true;
        }

        // 执行安装包
        if (String.IsNullOrEmpty(arg)) arg = "/passive /promptrestart";
        if (!Silent) arg = null;

        XTrace.WriteLine("正在安装：{0} {1}", fullFile, arg);
        var p = Process.Start(fullFile, arg);
        if (p.WaitForExit(600_000))
        {
            if (p.ExitCode == 0)
                XTrace.WriteLine("安装完成！");
            else
                XTrace.WriteLine("安装失败！ExitCode={0}", p.ExitCode);
            Environment.ExitCode = p.ExitCode;
            return p.ExitCode == 0;
        }
        else
        {
            XTrace.WriteLine("安装超时！");
            Environment.ExitCode = 400;
            return false;
        }
    }

    static Version GetLast(IList<VerInfo> vers, String prefix = null, String suffix = null)
    {
        var ver = new Version();
        if (vers.Count > 0)
        {
            //XTrace.WriteLine("已安装版本：");
            foreach (var item in vers)
            {
                if ((String.IsNullOrEmpty(prefix) || item.Name.StartsWith(prefix)) &&
                    (String.IsNullOrEmpty(suffix) || item.Name.EndsWith(suffix)))
                {
                    var str = item.Name.Trim('v');
                    var p = str.IndexOf('-');
                    if (p > 0) str = str.Substring(0, p);

                    var v = new Version(str);
                    if (v > ver) ver = v;
                }

                //XTrace.WriteLine(item.Name);
            }
            //XTrace.WriteLine("");
        }

        return ver;
    }

    public Boolean InstallNet40()
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet40), null);

        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本
        var target = new Version("4.0");
        if (ver >= target)
        {
            XTrace.WriteLine("已安装最新版 v{0}", ver);
            return true;
        }


        var rs = Install("dotNetFx40_Full_x86_x64.exe", null);
        if (!rs)
        {
            XTrace.WriteLine("安装NET4失败，准备清理环境后重新安装！");

            CleanForNet();

            rs = Install("dotNetFx40_Full_x86_x64.exe", null);
        }

        return rs;
    }

    public Boolean InstallNet45()
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet45), null);

        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本
        var target = new Version("4.5");
        if (ver >= target)
        {
            XTrace.WriteLine("已安装最新版 v{0}", ver);
            return false;
        }

        var rs = Install("NDP452-KB2901907-x86-x64-AllOS-ENU.exe");
        if (rs) Install("NDP452-KB2901907-x86-x64-AllOS-CHS.exe");

        if (!rs)
        {
            XTrace.WriteLine("安装NET45失败，准备清理环境后重新安装！");

            CleanForNet();

            rs = Install("NDP452-KB2901907-x86-x64-AllOS-ENU.exe");
            Install("NDP452-KB2901907-x86-x64-AllOS-CHS.exe");
        }

        return rs;
    }

    public Boolean InstallNet48()
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet48), null);

        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());

        var ver = GetLast(vers, null);

        // 目标版本。win10起支持4.8.1
        var osVer = Environment.OSVersion.Version;
        var target = osVer.Major >= 10 ? new Version("4.8.1") : new Version("4.8");
        if (ver >= target)
        {
            XTrace.WriteLine("已安装最新版 v{0}", ver);
            return true;
        }

        var is64 = IntPtr.Size == 8;

        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7)
        {
            //if (is64)
            //{
            //    Install("Windows6.1-KB3063858-x64.msu", "/win7", "/quiet /norestart");
            //}
            //else
            //{
            //    Install("Windows6.1-KB3063858-x86.msu", "/win7", "/quiet /norestart");
            //}
            InstallCert();
        }

        // win10/win11 中安装 .NET4.8.1
        var rs = false;
        if (osVer.Major >= 10)
        {
            rs = Install("ndp481-x86-x64-allos-enu.exe", null, "/passive /promptrestart /showfinalerror");
            if (rs) Install("ndp481-x86-x64-allos-chs.exe", null, "/passive /promptrestart /showfinalerror");
        }
        else
        {
            rs = Install("ndp48-x86-x64-allos-enu.exe", null, "/passive /promptrestart /showfinalerror");
            if (rs) Install("ndp48-x86-x64-allos-chs.exe", null, "/passive /promptrestart /showfinalerror");
        }

        return rs;
    }

    /// <summary>安装.NET6.0</summary>
    /// <param name="target">目标版本。包括子版本，如6.0.15</param>
    /// <param name="kind">安装类型。如aspnet/desktop/host</param>
    public Boolean InstallNet6(String target, String? kind = null)
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet6), null);

        var vers = GetNetCore();

        var suffix = "";
        if (!String.IsNullOrEmpty(kind)) suffix = "-" + kind;
        var ver = GetLast(vers, "v6.0", suffix);

        // 目标版本
        var targetVer = new Version(target);
        if (!Force && ver >= targetVer)
        {
            WriteLog("已安装最新版 v{0}", ver);
            return false;
        }

#if NET20
        var is64 = IntPtr.Size == 8;
#else
        var is64 = Environment.Is64BitOperatingSystem;
#endif

        // win7需要vc2019运行时
        var osVer = Environment.OSVersion.Version;
        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7 && ver.Major < 6)
        {
            if (is64)
            {
                Install("Windows6.1-KB3063858-x64.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x64.exe", "/vc2019", "/passive");
            }
            else
            {
                Install("Windows6.1-KB3063858-x86.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x86.exe", "/vc2019", "/passive");
            }
        }

        var rs = false;
        if (is64)
        {
            switch (kind)
            {
                case "aspnet":
                    rs = Install($"dotnet-runtime-{target}-win-x64.exe");
                    rs = Install($"aspnetcore-runtime-{target}-win-x64.exe");
                    break;
                case "desktop":
                    rs = Install($"windowsdesktop-runtime-{target}-win-x64.exe");
                    break;
                case "host":
                    rs = Install($"dotnet-hosting-{target}-win.exe");
                    break;
                default:
                    rs = Install($"dotnet-runtime-{target}-win-x64.exe");
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                case "aspnet":
                    rs = Install($"dotnet-runtime-{target}-win-x86.exe");
                    rs = Install($"aspnetcore-runtime-{target}-win-x86.exe");
                    break;
                case "desktop":
                    rs = Install($"windowsdesktop-runtime-{target}-win-x86.exe");
                    break;
                case "host":
                    rs = Install($"dotnet-hosting-{target}-win.exe");
                    break;
                default:
                    rs = Install($"dotnet-runtime-{target}-win-x86.exe");
                    break;
            }
        }

        return rs;
    }

    public Boolean InstallNet7(String kind = null)
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet7), null);

        var vers = GetNetCore();

        var suffix = "";
        if (!String.IsNullOrEmpty(kind)) suffix = "-" + kind;
        var ver = GetLast(vers, "v7.0", suffix);

        // 目标版本
        var target = new Version("7.0");
        if (ver >= target)
        {
            XTrace.WriteLine("已安装最新版 v{0}", ver);
            return false;
        }

        var is64 = IntPtr.Size == 8;

        // win7需要vc2019运行时
        var osVer = Environment.OSVersion.Version;
        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7)
        {
            if (is64)
            {
                Install("Windows6.1-KB3063858-x64.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x64.exe", "/vc2019", "/passive");
            }
            else
            {
                Install("Windows6.1-KB3063858-x86.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x86.exe", "/vc2019", "/passive");
            }
        }

        var rs = false;
        if (is64)
        {
            switch (kind)
            {
                case "aspnet":
                    rs = Install("dotnet-runtime-7.0.9-win-x64.exe");
                    rs = Install("aspnetcore-runtime-7.0.9-win-x64.exe");
                    break;
                case "desktop":
                    rs = Install("windowsdesktop-runtime-7.0.9-win-x64.exe");
                    break;
                case "host":
                    rs = Install("dotnet-hosting-7.0.9-win.exe");
                    break;
                default:
                    rs = Install("dotnet-runtime-7.0.9-win-x64.exe");
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                case "aspnet":
                    rs = Install("dotnet-runtime-7.0.9-win-x86.exe");
                    rs = Install("aspnetcore-runtime-7.0.9-win-x86.exe");
                    break;
                case "desktop":
                    rs = Install("windowsdesktop-runtime-7.0.9-win-x86.exe");
                    break;
                case "host":
                    rs = Install("dotnet-hosting-7.0.9-win.exe");
                    break;
                default:
                    rs = Install("dotnet-runtime-7.0.9-win-x86.exe");
                    break;
            }
        }

        return rs;
    }

    /// <summary>安装.NET8.0</summary>
    /// <param name="target">目标版本。包括子版本，如6.0.15</param>
    /// <param name="kind">安装类型。如aspnet/desktop/host</param>
    public Boolean InstallNet8(String target, String? kind = null)
    {
        using var span = Tracer?.NewSpan(nameof(InstallNet7), null);

        var vers = GetNetCore();

        var suffix = "";
        if (!String.IsNullOrEmpty(kind)) suffix = "-" + kind;
        var ver = GetLast(vers, "v8.0", suffix);

        // 目标版本
        var targetVer = new Version(target);
        if (!Force && ver >= targetVer)
        {
            WriteLog("已安装最新版 v{0}", ver);
            return false;
        }

#if NET20
        var is64 = IntPtr.Size == 8;
#else
        var is64 = Environment.Is64BitOperatingSystem;
#endif

        // win7需要vc2019运行时
        var osVer = Environment.OSVersion.Version;
        var isWin7 = osVer.Major == 6 && osVer.Minor == 1;
        if (isWin7 && ver.Major < 6)
        {
            if (is64)
            {
                Install("Windows6.1-KB3063858-x64.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x64.exe", "/vc2019", "/passive");
            }
            else
            {
                Install("Windows6.1-KB3063858-x86.msu", "/win7", "/quiet /norestart");
                Install("VC_redist.x86.exe", "/vc2019", "/passive");
            }
        }

        if (is64)
        {
            switch (kind)
            {
                case "aspnet":
                    Install($"dotnet-runtime-{target}-win-x64.exe");
                    Install($"aspnetcore-runtime-{target}-win-x64.exe");
                    break;
                case "desktop":
                    Install($"windowsdesktop-runtime-{target}-win-x64.exe");
                    break;
                case "host":
                    Install($"dotnet-hosting-{target}-win.exe");
                    break;
                default:
                    Install($"dotnet-runtime-{target}-win-x64.exe");
                    break;
            }
        }
        else
        {
            switch (kind)
            {
                case "aspnet":
                    Install($"dotnet-runtime-{target}-win-x86.exe");
                    Install($"aspnetcore-runtime-{target}-win-x86.exe");
                    break;
                case "desktop":
                    Install($"windowsdesktop-runtime-{target}-win-x86.exe");
                    break;
                case "host":
                    Install($"dotnet-hosting-{target}-win.exe");
                    break;
                default:
                    Install($"dotnet-runtime-{target}-win-x86.exe");
                    break;
            }
        }

        return true;
    }

    /// <summary>获取所有已安装版本</summary>
    /// <returns></returns>
    public IList<VerInfo> GetVers()
    {
        var vers = new List<VerInfo>();
        vers.AddRange(Get1To45VersionFromRegistry());
        vers.AddRange(Get45PlusFromRegistry());
        vers.AddRange(GetNetCore());

        return vers;
    }

    public static IList<VerInfo> Get1To45VersionFromRegistry()
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

    public static IList<VerInfo> Get45PlusFromRegistry()
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

    public static IList<VerInfo> GetNetCore()
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
                    if (item.Name.Contains("AspNet"))
                        name += "-aspnet";
                    else if (item.Name.Contains("Desktop"))
                        name += "-desktop";
                    if (!dic.ContainsKey(name))
                    {
                        dic.Add(name, new VerInfo { Name = name, Version = item.Name + " " + elm.Name });
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
                        if (ver.Contains("AspNet"))
                            name += "-aspnet";
                        else if (ver.Contains("Desktop"))
                            name += "-desktop";

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
    #endregion

    #region 辅助
    public static String GetMD5(String fileName)
    {
        var fi = new FileInfo(fileName);
        var md5 = MD5.Create();
        using var fs = fi.OpenRead();
        var buf = md5.ComputeHash(fs);
        var hex = BitConverter.ToString(buf).Replace("-", null);

        return hex;
    }

    /// <summary>加载内嵌的文件MD5信息</summary>
    /// <returns></returns>
    public static IDictionary<String, String> LoadMD5s()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ms = asm.GetManifestResourceStream(typeof(NetRuntime).Namespace + ".res.md5.txt");

        var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(ms);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (String.IsNullOrEmpty(line)) continue;

            var ss = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length >= 2)
            {
                dic[ss[0]] = ss[1];
            }
        }

        return dic;
    }

    /// <summary>为安装NET清理环境</summary>
    public static void CleanForNet()
    {
        {
            // 解决“一般信任关系失败”问题

            Process.Start("regsvr32", "/s Softpub.dll").WaitForExit(15_000);
            Process.Start("regsvr32", "/s Wintrust.dll").WaitForExit(15_000);
            Process.Start("regsvr32", "/s Initpki.dll").WaitForExit(15_000);
            Process.Start("regsvr32", "/s Mssip32.dll").WaitForExit(15_000);
        }

        {
#if !NETCOREAPP3_1
            using var reg = Registry.CurrentUser.OpenSubKey(@"\Software\Microsoft\Windows\CurrentVersion\WinTrust\Trust Providers\Software Publishing", true);
            if (reg != null)
            {
                var v = (Int32)reg.GetValue("State");
                if (v != 0x23c00) reg.SetValue("State", 0x23c00);
            }
#endif
        }

        {
            // https://support.microsoft.com/zh-cn/sbs/windows/%E4%BF%AE%E5%A4%8D-windows-%E6%9B%B4%E6%96%B0%E9%94%99%E8%AF%AF-18b693b5-7818-5825-8a7e-2a4a37d6d787
            Process.Start("net", "stop bits").WaitForExit(15_000);
            Process.Start("net", "stop wuauserv").WaitForExit(15_000);

            var dirs = new[] {
                @"C:\Windows\SoftwareDistribution",
                @"C:\Windows\System32\catroot2",
            };
            foreach (var dir in dirs)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Move(dir, dir + "_bak");
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine(ex.Message);
                    }
                }
            }

            Process.Start("net", "start bits").WaitForExit(15_000);
            Process.Start("net", "start wuauserv").WaitForExit(15_000);
        }

        {
            //!!! 以下代码，能够破坏掉已经安装好的.NET4.0
            //var files = new[] {
            //    @"C:\Windows\System32\msvcr100_clr0400.dll",
            //    @"C:\Windows\SysWOW64\msvcr100_clr0400.dll",
            //    @"C:\Windows\System32\msvcr110_clr0400.dll",
            //    @"C:\Windows\SysWOW64\msvcr110_clr0400.dll",
            //    @"C:\Windows\System32\msvcp110_clr0400.dll",
            //    @"C:\Windows\SysWOW64\msvcp110_clr0400.dll",
            //};
            //foreach (var file in files)
            //{
            //    if (File.Exists(file))
            //    {
            //        try
            //        {
            //            XTrace.WriteLine("清理：{0}", file);
            //            File.Move(file, file + ".bak");
            //        }
            //        catch
            //        {
            //            XTrace.WriteLine("重命名文件失败，请手工重命名：{0}", file);
            //        }
            //    }
            //}
        }

        InstallCert();
    }

    public static Boolean InstallCert()
    {
        XTrace.WriteLine("准备安装微软根证书");

        // 释放文件
        var asm = Assembly.GetExecutingAssembly();
        var names = new[] { "CertMgr.Exe", "MicrosoftRootCertificateAuthority2011.cer" };
        foreach (var name in names)
        {
            var ms = asm.GetManifestResourceStream(typeof(NetRuntime).Namespace + ".res." + name);
            var buf = ms.ReadBytes(-1);

            File.WriteAllBytes(name.GetFullPath(), buf);
        }

        var exe = names[0];
        var cert = names[1];
        if (!File.Exists(exe) || !File.Exists(cert)) return false;

        // 执行
        try
        {
            var p = Process.Start(exe, $"-add \"{cert}\" -s -r localMachine AuthRoot");

            return p.WaitForExit(30_000) && p.ExitCode == 0;
        }
        catch (Exception ex)
        {
            XTrace.WriteLine(ex.Message);
            return false;
        }
        finally
        {
            if (File.Exists(cert)) File.Delete(cert);
            if (File.Exists(exe)) File.Delete(exe);
        }
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; } = Logger.Null;

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object?[] args) => Log?.Info($"[NetRuntime]{format}", args);
    #endregion
}