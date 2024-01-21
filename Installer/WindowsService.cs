using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using NewLife;
using NewLife.Log;
using static Installer.Advapi32;

namespace Installer;

public class WindowsService
{
    #region 服务状态和控制
    /// <summary>服务是否已安装</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns></returns>
    public Boolean IsInstalled(String serviceName)
    {
        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
        if (manager == null || manager.IsInvalid) return false;

        using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_QUERY_CONFIG));
        return service != null && !service.IsInvalid;
    }

    /// <summary>服务是否已启动</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns></returns>
    public unsafe Boolean IsRunning(String serviceName)
    {
        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
        if (manager == null || manager.IsInvalid) return false;

        using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_QUERY_STATUS));
        if (service == null || service.IsInvalid) return false;

        SERVICE_STATUS status = default;
        return !QueryServiceStatus(service, &status)
            ? throw new Win32Exception(Marshal.GetLastWin32Error())
            : status.currentState == ServiceControllerStatus.Running;
    }

    /// <summary>安装服务</summary>
    /// <param name="serviceName">服务名</param>
    /// <param name="displayName"></param>
    /// <param name="binPath"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public Boolean Install(String serviceName, String displayName, String binPath, String description)
    {
        XTrace.WriteLine("{0}.Install {1}, {2}, {3}, {4}", GetType().Name, serviceName, displayName, binPath, description);

        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CREATE_SERVICE));
        if (manager.IsInvalid)
        {
            XTrace.WriteLine("安装Windows服务要求以管理员运行");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using var service = new SafeServiceHandle(CreateService(manager, serviceName, displayName, ServiceOptions.SERVICE_ALL_ACCESS, 0x10, 2, 1, binPath, null, 0, null, null, null));
        if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

        // 设置描述信息
        if (!description.IsNullOrEmpty())
        {
            SERVICE_DESCRIPTION sd;
            sd.Description = Marshal.StringToHGlobalUni(description);
            var lpInfo = Marshal.AllocHGlobal(Marshal.SizeOf(sd));

            try
            {
                Marshal.StructureToPtr(sd, lpInfo, false);

                const Int32 SERVICE_CONFIG_DESCRIPTION = 1;
                ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, lpInfo);
            }
            finally
            {
                Marshal.FreeHGlobal(lpInfo);
                Marshal.FreeHGlobal(sd.Description);
            }
        }

        return true;
    }

    /// <summary>卸载服务</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns></returns>
    public unsafe Boolean Remove(String serviceName)
    {
        XTrace.WriteLine("{0}.Remove {1}", GetType().Name, serviceName);

        if (!IsAdministrator()) return RunAsAdministrator("-uninstall");

        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_ALL));
        if (manager.IsInvalid)
        {
            XTrace.WriteLine("卸载Windows服务要求以管理员运行");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_STOP | ServiceOptions.STANDARD_RIGHTS_DELETE));
        if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

        SERVICE_STATUS status = default;
        ControlService(service, ControlOptions.Stop, &status);

        return DeleteService(service) == 0 ? throw new Win32Exception(Marshal.GetLastWin32Error()) : true;
    }

    /// <summary>启动服务</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns></returns>
    public Boolean Start(String serviceName)
    {
        XTrace.WriteLine("{0}.Start {1}", GetType().Name, serviceName);

        if (!IsAdministrator()) return RunAsAdministrator("-start");

        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_CONNECT));
        if (manager.IsInvalid)
        {
            XTrace.WriteLine("启动Windows服务要求以管理员运行");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_START));
        return service.IsInvalid
            ? throw new Win32Exception(Marshal.GetLastWin32Error())
            : !StartService(service, 0, IntPtr.Zero) ? throw new Win32Exception(Marshal.GetLastWin32Error()) : true;
    }

    /// <summary>停止服务</summary>
    /// <param name="serviceName">服务名</param>
    /// <returns></returns>
    public unsafe Boolean Stop(String serviceName)
    {
        XTrace.WriteLine("{0}.Stop {1}", GetType().Name, serviceName);

        if (!IsAdministrator()) return RunAsAdministrator("-stop");

        using var manager = new SafeServiceHandle(OpenSCManager(null, null, ServiceControllerOptions.SC_MANAGER_ALL));
        if (manager.IsInvalid)
        {
            XTrace.WriteLine("停止Windows服务要求以管理员运行");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        using var service = new SafeServiceHandle(OpenService(manager, serviceName, ServiceOptions.SERVICE_STOP));
        if (service.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

        SERVICE_STATUS status = default;
        return !ControlService(service, ControlOptions.Stop, &status) ? throw new Win32Exception(Marshal.GetLastWin32Error()) : true;
    }

    /// <summary>重启服务</summary>
    /// <param name="serviceName">服务名</param>
    public Boolean Restart(String serviceName)
    {
        XTrace.WriteLine("{0}.Restart {1}", GetType().Name, serviceName);

        if (!IsAdministrator()) return RunAsAdministrator("-restart");

        //if (InService)
        {
            var cmd = $"/c net stop {serviceName} & ping 127.0.0.1 -n 5 & net start {serviceName}";
            Process.Start("cmd.exe", cmd);
        }
        //else
        //{
        //    Process.Start(Service.GetExeName(), "-run -delay");
        //}

        //// 在临时目录生成重启服务的批处理文件
        //var filename = "重启.bat".GetFullPath();
        //if (File.Exists(filename)) File.Delete(filename);

        //File.AppendAllText(filename, "net stop " + serviceName);
        //File.AppendAllText(filename, Environment.NewLine);
        //File.AppendAllText(filename, "ping 127.0.0.1 -n 5 > nul ");
        //File.AppendAllText(filename, Environment.NewLine);
        //File.AppendAllText(filename, "net start " + serviceName);

        ////执行重启服务的批处理
        ////RunCmd(filename, false, false);
        //var p = new Process();
        //var si = new ProcessStartInfo
        //{
        //    FileName = filename,
        //    UseShellExecute = true,
        //    CreateNoWindow = true
        //};
        //p.StartInfo = si;

        //p.Start();

        ////if (File.Exists(filename)) File.Delete(filename);

        return true;
    }

    static Boolean RunAsAdministrator(String argument)
    {
        var exe = ExecutablePath;
        if (exe.IsNullOrEmpty()) return false;

        if (exe.EndsWithIgnoreCase(".dll"))
        {
            var exe2 = Path.ChangeExtension(exe, ".exe");
            if (File.Exists(exe2)) exe = exe2;
        }

        var startInfo = exe.EndsWithIgnoreCase(".dll") ?
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{Path.GetFileName(exe)} {argument}",
                WorkingDirectory = Path.GetDirectoryName(exe),
                Verb = "runas",
                UseShellExecute = true,
            } :
            new ProcessStartInfo
            {
                FileName = exe,
                Arguments = argument,
                Verb = "runas",
                UseShellExecute = true,
            };

        var p = Process.Start(startInfo);
        return !p.WaitForExit(5_000) || p.ExitCode == 0;
    }

    static String _executablePath;
    static String ExecutablePath
    {
        get
        {
            if (_executablePath == null)
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var codeBase = entryAssembly.CodeBase;
                    var uri = new Uri(codeBase);
                    _executablePath = uri.IsFile ? uri.LocalPath + Uri.UnescapeDataString(uri.Fragment) : uri.ToString();
                }
                else
                {
                    var moduleFileNameLongPath = GetModuleFileNameLongPath(new HandleRef(null, IntPtr.Zero));
                    _executablePath = moduleFileNameLongPath.ToString().GetFullPath();
                }
            }

            return _executablePath;
        }
    }

    static StringBuilder GetModuleFileNameLongPath(HandleRef hModule)
    {
        var sb = new StringBuilder(260);
        var num = 1;
        var num2 = 0;
        while ((num2 = GetModuleFileName(hModule, sb, sb.Capacity)) == sb.Capacity && Marshal.GetLastWin32Error() == 122 && sb.Capacity < 32767)
        {
            num += 2;
            var capacity = (num * 260 < 32767) ? (num * 260) : 32767;
            sb.EnsureCapacity(capacity);
        }
        sb.Length = num2;
        return sb;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern Int32 GetModuleFileName(HandleRef hModule, StringBuilder buffer, Int32 length);

    public Boolean IsAdministrator()
    {
        var current = WindowsIdentity.GetCurrent();
        var windowsPrincipal = new WindowsPrincipal(current);
        return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    #endregion
}
