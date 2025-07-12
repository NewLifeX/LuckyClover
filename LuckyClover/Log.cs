using System;

namespace LuckyClover;

public class Log
{
    /// <summary>Windows10以上支持ANSI颜色代码，其它平台全部支持</summary>
    private static Boolean _ansiColor = Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major >= 10;
    public static void WriteLine(String format, params Object[] args)
    {
        // Zip压缩 \e[31;1m{0}\e[0m 到 \e[31;1m{1}\e[0m
        if (!_ansiColor)
        {
            for (var i = 30; i < 35; i++)
            {
                format = format.Replace($"\e[{i};1m", "");
            }
            format = format.Replace("\e[0m", "");
        }

        Console.WriteLine(format, args);
    }
}
