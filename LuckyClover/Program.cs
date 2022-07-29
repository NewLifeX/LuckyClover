using System;
using System.Collections.Generic;
using System.Linq;
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
        Console.WriteLine(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
        Console.WriteLine();

        _menus["net48"] = InstallNet48;
        _menus["net"] = InstallNet48;

        _menus["net6"] = InstallNet6;
        _menus["netcore"] = InstallNet6;

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
        Console.WriteLine("已安装版本：");
        foreach (var item in vers)
        {
            Console.WriteLine(item.Value);
        }
        Console.WriteLine("");

        Console.WriteLine("命令：clover");
        Console.WriteLine("运行时：{0}", Environment.Version);
        Console.WriteLine("");

        Console.WriteLine("可用命令：{0}", String.Join(", ", _menus.Keys));

        var line = Console.ReadLine()?.Trim();

        return line;
    }

    private static void InstallNet48(String[] args) =>
        //XTrace.WriteLine("InstallNet5 {0}", args);
        Console.WriteLine("InstallNet5 {0}", args?.FirstOrDefault());

    private static void InstallNet6(String[] args) =>
        // 检查是否已安装.NET运行时
        Console.WriteLine("InstallNet6 {0}", args?.FirstOrDefault());

    private static void WriteVersion(String version, String spLevel = "")
    {
        version = version.Trim();
        if (String.IsNullOrEmpty(version)) return;

        var spLevelString = "";
        if (!String.IsNullOrEmpty(spLevel))
            spLevelString = " Service Pack " + spLevel;

        Console.WriteLine($"{version}{spLevelString}");
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
        var ver = ndpKey.GetValue("Version");
        if (ver != null)
            name = ver.ToString();
        else
        {
            var release = ndpKey.GetValue("Release");
            if (release != null)
                name = CheckFor45PlusVersion((Int32)ndpKey.GetValue("Release"));
        }

        if (!String.IsNullOrEmpty(name)) dic.Add(name, name);

        // Checking the version using >= enables forward compatibility.
        String CheckFor45PlusVersion(Int32 releaseKey)
        {
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
}