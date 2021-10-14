using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuckClover
{
    class Program
    {
        private static IDictionary<String, Action<String[]>> _menus = new Dictionary<String, Action<String[]>>(StringComparer.OrdinalIgnoreCase);

        static void Main(string[] args)
        {
            //XTrace.UseConsole();

            var asm = Assembly.GetEntryAssembly().GetName();
            Console.WriteLine("幸运草 LuckyClover v{0}", asm.Version);
            Console.WriteLine("无依赖编译为linux-arm/linux-x86/windows，用于自动安装主流.NET运行时");
            Console.WriteLine();

            _menus["net5"] = InstallNet5;

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
            Console.WriteLine("命令：LuckClover");
            Console.WriteLine("");

            var line = Console.ReadLine()?.Trim();

            return line;
        }

        private static void InstallNet5(String[] args)
        {
            //XTrace.WriteLine("InstallNet5 {0}", args);
            Console.WriteLine("InstallNet5 {0}", args);
        }
    }
}