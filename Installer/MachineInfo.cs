using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using NewLife.Log;

namespace NewLife;

/// <summary>机器信息</summary>
/// <remarks>
/// 文档 https://newlifex.com/core/machine_info
/// 
/// 刷新信息成本较高，建议采用单例模式
/// </remarks>
public class MachineInfo
{
    #region 属性
    /// <summary>系统名称</summary>
    [DisplayName("系统名称")]
    public String OSName { get; set; }

    /// <summary>系统版本</summary>
    [DisplayName("系统版本")]
    public String OSVersion { get; set; }

    /// <summary>产品名称。制造商</summary>
    [DisplayName("产品名称")]
    public String Product { get; set; }

    /// <summary>处理器型号</summary>
    [DisplayName("处理器型号")]
    public String Processor { get; set; }

    ///// <summary>处理器序列号。PC处理器序列号绝大部分重复，实际存储处理器的其它信息</summary>
    //public String CpuID { get; set; }

    /// <summary>硬件唯一标识。取主板编码，部分品牌存在重复</summary>
    [DisplayName("硬件唯一标识")]
    public String UUID { get; set; }

    /// <summary>软件唯一标识。系统标识，操作系统重装后更新，Linux系统的machine_id，Android的android_id，Ghost系统存在重复</summary>
    [DisplayName("软件唯一标识")]
    public String Guid { get; set; }

    /// <summary>计算机序列号。适用于品牌机，跟笔记本标签显示一致</summary>
    [DisplayName("计算机序列号")]
    public String Serial { get; set; }

    /// <summary>主板。序列号或家族信息</summary>
    [DisplayName("主板")]
    public String Board { get; set; }

    /// <summary>磁盘序列号</summary>
    [DisplayName("磁盘序列号")]
    public String DiskID { get; set; }

    /// <summary>内存总量。单位Byte</summary>
    [DisplayName("内存总量")]
    public UInt64 Memory { get; set; }

    /// <summary>可用内存。单位Byte</summary>
    [DisplayName("可用内存")]
    public UInt64 AvailableMemory { get; set; }

    /// <summary>CPU占用率</summary>
    [DisplayName("CPU占用率")]
    public Single CpuRate { get; set; }

    ///// <summary>网络上行速度。字节每秒，初始化后首次读取为0</summary>
    //[DisplayName("网络上行速度")]
    //public UInt64 UplinkSpeed { get; set; }

    ///// <summary>网络下行速度。字节每秒，初始化后首次读取为0</summary>
    //[DisplayName("网络下行速度")]
    //public UInt64 DownlinkSpeed { get; set; }

    ///// <summary>温度。单位度</summary>
    //[DisplayName("温度")]
    //public Double Temperature { get; set; }

    ///// <summary>电池剩余。小于1的小数，常用百分比表示</summary>
    //[DisplayName("电池剩余")]
    //public Double Battery { get; set; }
    #endregion

    #region 构造
    /// <summary>当前机器信息。默认null，在RegisterAsync后才能使用</summary>
    public static MachineInfo Current { get; set; } = new MachineInfo();
    #endregion

    #region 方法
    /// <summary>刷新</summary>
    public void Init()
    {
        var osv = Environment.OSVersion;
        if (OSVersion.IsNullOrEmpty()) OSVersion = osv.Version + "";
        if (OSName.IsNullOrEmpty()) OSName = (osv + "").TrimStart("Microsoft").TrimEnd(OSVersion).Trim();
        if (Guid.IsNullOrEmpty()) Guid = "";

        try
        {
            LoadWindowsInfo();
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }

        // window+netcore 不方便读取注册表，随机生成一个guid，借助文件缓存确保其不变
        if (Guid.IsNullOrEmpty()) Guid = "0-" + System.Guid.NewGuid().ToString();
        if (UUID.IsNullOrEmpty()) UUID = "0-" + System.Guid.NewGuid().ToString();

        try
        {
            Refresh();
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
        }
    }

    private void LoadWindowsInfo()
    {
        var machine_guid = "";

        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        if (reg != null) machine_guid = reg.GetValue("MachineGuid") + "";
        if (machine_guid.IsNullOrEmpty())
        {
            //reg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            //if (reg != null) machine_guid = reg.GetValue("MachineGuid") + "";
        }

        var ci = new ComputerInfo();
        try
        {
            Memory = ci.TotalPhysicalMemory;

            // 系统名取WMI可能出错
            OSName = ci.OSFullName.TrimStart("Microsoft").Trim();
            OSVersion = ci.OSVersion;
        }
        catch
        {
            try
            {
                var reg2 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (reg2 != null)
                {
                    OSName = reg2.GetValue("ProductName") + "";
                    OSVersion = reg2.GetValue("ReleaseId") + "";
                }
            }
            catch (Exception ex)
            {
                if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteException(ex);
            }
        }

        Processor = GetInfo("Win32_Processor", "Name");
        //CpuID = GetInfo("Win32_Processor", "ProcessorId");
        var uuid = GetInfo("Win32_ComputerSystemProduct", "UUID");
        Product = GetInfo("Win32_ComputerSystemProduct", "Name");
        DiskID = GetInfo("Win32_DiskDrive", "SerialNumber");

        var sn = GetInfo("Win32_BIOS", "SerialNumber");
        if (!sn.IsNullOrEmpty() && !sn.EqualIgnoreCase("System Serial Number")) Serial = sn;
        Board = GetInfo("Win32_BaseBoard", "SerialNumber");

        // UUID取不到时返回 FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF
        if (!uuid.IsNullOrEmpty() && !uuid.EqualIgnoreCase("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")) UUID = uuid;

        if (!machine_guid.IsNullOrEmpty()) Guid = machine_guid;
    }

    /// <summary>获取实时数据，如CPU、内存、温度</summary>
    public void Refresh()
    {
        RefreshWindows();
    }

    private void RefreshWindows()
    {
        MEMORYSTATUSEX ms = default;
        ms.Init();
        if (GlobalMemoryStatusEx(ref ms))
        {
            Memory = ms.ullTotalPhys;
            AvailableMemory = ms.ullAvailPhys;
        }

        GetSystemTimes(out var idleTime, out var kernelTime, out var userTime);

        var current = new SystemTime
        {
            IdleTime = idleTime.ToLong(),
            TotalTime = kernelTime.ToLong() + userTime.ToLong(),
        };

        var idle = current.IdleTime - (_systemTime?.IdleTime ?? 0);
        var total = current.TotalTime - (_systemTime?.TotalTime ?? 0);
        _systemTime = current;

        CpuRate = total == 0 ? 0 : (Single)Math.Round((Single)(total - idle) / total, 4);
    }
    #endregion

    #region 内存
    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [SecurityCritical]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern Boolean GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    internal struct MEMORYSTATUSEX
    {
        internal UInt32 dwLength;

        internal UInt32 dwMemoryLoad;

        internal UInt64 ullTotalPhys;

        internal UInt64 ullAvailPhys;

        internal UInt64 ullTotalPageFile;

        internal UInt64 ullAvailPageFile;

        internal UInt64 ullTotalVirtual;

        internal UInt64 ullAvailVirtual;

        internal UInt64 ullAvailExtendedVirtual;

        internal void Init() => dwLength = checked((UInt32)Marshal.SizeOf(typeof(MEMORYSTATUSEX)));
    }
    #endregion

    #region 磁盘
    /// <summary>获取指定目录所在盘可用空间，默认当前目录</summary>
    /// <param name="path"></param>
    /// <returns>返回可用空间，字节，获取失败返回-1</returns>
    public static Int64 GetFreeSpace(String path = null)
    {
        if (path.IsNullOrEmpty()) path = ".";

        var driveInfo = new DriveInfo(Path.GetPathRoot(path.GetFullPath()));
        if (driveInfo == null || !driveInfo.IsReady) return -1;

        try
        {
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>获取指定目录下文件名，支持去掉后缀的去重，主要用于Linux</summary>
    /// <param name="path"></param>
    /// <param name="trimSuffix"></param>
    /// <returns></returns>
    public static ICollection<String> GetFiles(String path, Boolean trimSuffix = false)
    {
        var list = new List<String>();
        if (path.IsNullOrEmpty()) return list;

        var di = path.AsDirectory();
        if (!di.Exists) return list;

        var list2 = di.GetFiles().Select(e => e.Name).ToList();
        foreach (var item in list2)
        {
            var line = item?.Trim();
            if (!line.IsNullOrEmpty())
            {
                if (trimSuffix)
                {
                    if (!list2.Any(e => e != line && line.StartsWith(e))) list.Add(line);
                }
                else
                {
                    list.Add(line);
                }
            }
        }

        return list;
    }
    #endregion

    #region Windows辅助
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern Boolean GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    private struct FILETIME
    {
        public UInt32 Low;

        public UInt32 High;

        public FILETIME(Int64 time)
        {
            Low = (UInt32)time;
            High = (UInt32)(time >> 32);
        }

        public Int64 ToLong() => (Int64)(((UInt64)High << 32) | Low);
    }

    private class SystemTime
    {
        public Int64 IdleTime;
        public Int64 TotalTime;
    }

    private SystemTime _systemTime;

    /// <summary>获取WMI信息</summary>
    /// <param name="path"></param>
    /// <param name="property"></param>
    /// <param name="nameSpace"></param>
    /// <returns></returns>
    public static String GetInfo(String path, String property, String nameSpace = null)
    {
        // Linux Mono不支持WMI
        if (Runtime.Mono) return "";

        var bbs = new List<String>();
        try
        {
            var wql = $"Select {property} From {path}";
            var cimobject = new ManagementObjectSearcher(nameSpace, wql);
            var moc = cimobject.Get();
            foreach (var mo in moc)
            {
                var val = mo?.Properties?[property]?.Value;
                if (val != null) bbs.Add(val.ToString().Trim());
            }
        }
        catch (Exception ex)
        {
            if (XTrace.Log.Level <= LogLevel.Debug) XTrace.WriteLine("WMI.GetInfo({0})失败！{1}", path, ex.Message);
            return "";
        }

        bbs.Sort();

        return bbs.Distinct().Join();
    }
    #endregion
}