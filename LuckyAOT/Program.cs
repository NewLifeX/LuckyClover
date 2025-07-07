using System;
using System.Reflection;

namespace LuckyClover;

#if NET20
delegate void Action();
#endif

internal class Program
{
    private static void Main(String[] args)
    {
        var cmd = "";
        if (args.Length >= 1) cmd = args[0];

        var asm = Assembly.GetEntryAssembly();
        var ver = asm.GetName().Version + "";
        var fver = ver;
        var vers = asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
        if (vers != null && vers.Length > 0)
        {
            var vatt = vers[0] as AssemblyFileVersionAttribute;
            if (!String.IsNullOrEmpty(vatt.Version)) fver = vatt.Version;
        }

        Console.WriteLine("幸运草 LuckyAOT v{0}", fver);
        //Console.WriteLine("无依赖编译为linux-arm/linux-x86/windows，用于自动安装主流.NET运行时");
#if NETFRAMEWORK
            var atts = asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            Console.WriteLine((atts[0] as AssemblyDescriptionAttribute).Description);
#else
        Console.WriteLine(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
#endif
        Console.WriteLine("{0}", Environment.OSVersion);
        Console.WriteLine("运行时：{0}", Environment.Version);
        Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", GetCompileTime(ver));
        Console.WriteLine();

        Console.ReadKey();
    }

    /// <summary>根据版本号计算得到编译时间</summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static DateTime GetCompileTime(String version)
    {
        var ss = version?.Split(['.']);
        if (ss == null || ss.Length < 4) return DateTime.MinValue;

        if (!Int32.TryParse(ss[2], out var d)) d = 0;
        if (!Int32.TryParse(ss[3], out var s)) s = 0;
        var y = DateTime.Today.Year;

        // 指定年月日的版本格式 1.0.yyyy.mmdd
        if (d <= y && d >= y - 10)
        {
            var dt = new DateTime(d, 1, 1);
            if (s > 0)
            {
                if (s >= 200) dt = dt.AddMonths(s / 100 - 1);
                s %= 100;
                if (s > 1) dt = dt.AddDays(s - 1);
            }

            return dt;
        }
        else
        {
            var dt = new DateTime(2000, 1, 1);
            dt = dt.AddDays(d).AddSeconds(s * 2);

            return dt;
        }
    }
}