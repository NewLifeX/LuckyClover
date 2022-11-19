using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace LuckyClover;

internal class Program
{
    private static readonly Dictionary<String, Action<String[]>> _menus = new(StringComparer.OrdinalIgnoreCase);
    private static readonly String _baseUrl = "http://x.newlifex.com/dotnet";

    private static void Main(String[] args)
    {
        //XTrace.UseConsole();

        var asm = Assembly.GetEntryAssembly();
        Console.WriteLine("幸运草 LuckyClover v{0}", asm.GetName().Version);
        //Console.WriteLine("无依赖编译为linux-arm/linux-x86/windows，用于自动安装主流.NET运行时");
        Console.WriteLine(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
        Console.WriteLine("{0}", Environment.OSVersion);
        Console.WriteLine();

        // 根据操作系统，自动选择安装NET版本
        _menus["net"] = AutoInstallNet;

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

        InstallNet7(args);
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

    private static IList<VerInfo> GetNetCore()
    {
        var list = new List<VerInfo>();

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