using System;
using System.Text;

namespace Installer;

public static class Helper
{
    /// <summary>去掉两头的0字节</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static String TrimZero(this String value) => value?.Trim().Trim('\0').Trim().Replace("\0", null);

    /// <summary>
    /// 获取可见字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static String GetInvisibleChar(this String value, Boolean isFirst = true)
    {
        if (String.IsNullOrEmpty(value)) return value;

        var builder = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i].IsInvisible())
                builder.Append(value[i]);
            else if (isFirst)
                break;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 是否可见字符串
    /// ASCII码中，第0～31号及第127号(共33个)是控制字符或通讯专用字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Boolean IsInvisible(this Char ch) => ch is > (Char)31 and not (Char)127;
}