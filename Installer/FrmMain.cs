using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using BuildTool;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Threading;
using NewLife.Web;
using Timer = System.Threading.Timer;
#if !NET40
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Installer;

#if NET40
#else
//delegate void Func();
delegate void Action();
#endif

public partial class FrmMain : Form
{
    private MachineInfo _info;
    private Boolean _x64;
    private String _installPath;
    private String _serviceName = "StarAgent";
    private ITracer _tracer;

    public FrmMain()
    {
        InitializeComponent();

#if NET45
        _tracer = DefaultTracer.Instance;
#endif
    }

    private void FrmMain_Load(Object sender, EventArgs e)
    {
        rtbLog.UseWinFormControl();

        using var span = _tracer?.NewSpan(nameof(FrmMain_Load));

        var asm = AssemblyX.Create(Assembly.GetExecutingAssembly());
        Text = String.Format("{2} v{0} {1:HH:mm:ss}", asm.FileVersion, asm.Compile, Text);

        var mi = new MachineInfo();
        mi.Init();
        span?.SetTag(mi);
        XTrace.WriteLine(mi.ToJson(true));
        _info = mi;

        txtMachince.Text = Environment.MachineName;
        txtUser.Text = Environment.UserName;

        txtOS.Text = mi.OSName;
        txtVersion.Text = mi.OSVersion;

        txtProduct.Text = mi.Product;
        txtSerial.Text = mi.Serial;

        _x64 = IntPtr.Size == 8;

        var f = "server.txt";
        if (File.Exists(f))
        {
            var url = File.ReadAllText(f)?.Trim();
            if (!url.IsNullOrWhiteSpace()) txtServer.Text = url;
        }

        _timer = new Timer(Detect, null, 0, 30_000);

        ThreadPoolX.QueueUserWorkItem(DetectTarget);
    }

    #region 运行时环境
    Timer _timer;
    private Boolean _firstDetect;
    private void Detect(Object state)
    {
        using var span = _tracer?.NewSpan(nameof(Detect));

        var net = new NetRuntime();
        var vers = net.GetVers();
        span?.SetTag(vers);

        if (!_firstDetect)
        {
            _firstDetect = true;

            XTrace.WriteLine("已安装NET运行时版本：");
            foreach (var item in vers)
            {
                if (String.IsNullOrEmpty(item.Sp))
                    XTrace.WriteLine("{0,-10} {1}", item.Name, item.Version);
                else
                    XTrace.WriteLine("{0,-10} {1} Sp{2}", item.Name, item.Version, item.Sp);
            }
        }

        this.Invoke(new Action(() =>
        {
            if (vers.Any(e => e.Name.StartsWith("v2.0")))
                cbNET2.Checked = true;

            if (vers.Any(e => e.Name.StartsWith("v4.0")))
                cbNET4.Checked = true;
            else
                btnNET4.Enabled = true;

            if (vers.Any(e => e.Name.StartsWith("v4.8")))
                cbNET48.Checked = true;
            else
                btnNET48.Enabled = true;

            if (vers.Any(e => e.Name.StartsWith("v6.")))
                cbNET6.Checked = true;
            else
                btnNET6.Enabled = true;

            if (vers.Any(e => e.Name.StartsWith("v8.")))
                cbNET8.Checked = true;
            else
                btnNET8.Enabled = true;
        }));

        var svc = new WindowsService();
        if (svc.IsInstalled(_serviceName))
        {
            this.Invoke(new Action(() =>
            {
                btnUninstall.Enabled = true;
            }));

            var bRunning = svc.IsRunning(_serviceName);
            XTrace.WriteLine("软件{0}已安装！{1}", _serviceName, bRunning ? "正在运行！" : "未运行！");
        }
        else
        {
            XTrace.WriteLine("软件{0}未安装！", _serviceName);
        }
    }

    NetRuntime GetNetRuntime(Boolean isApp, Boolean silent)
    {
        var net = new NetRuntime
        {
            BaseUrl = txtServer.Text.EnsureEnd("/") + (isApp ? "star" : "dotnet"),
            Silent = silent,
            InstallPath = _installPath,
            Hashs = NetRuntime.LoadMD5s(),
        };
#if NET40
        net.Tracer = DefaultTracer.Instance;
#endif

        return net;
    }

    void ShowResult(Boolean rs)
    {
        if (rs)
            MessageBox.Show("安装成功！", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
            MessageBox.Show("安装失败！", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void btnNET4_Click(Object sender, EventArgs e)
    {
        var net = GetNetRuntime(false, false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            var rs = net.InstallNet40();

            _timer.Change(0, 30_000);

            ShowResult(rs);
        });
    }

    private void btnNET48_Click(Object sender, EventArgs e)
    {
        var net = GetNetRuntime(false, false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            var rs = net.InstallNet48();

            _timer.Change(0, 30_000);

            ShowResult(rs);
        });
    }

    private void btnNET6_Click(Object sender, EventArgs e)
    {
        var net = GetNetRuntime(false, false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            var rs = net.InstallNet6("6.0.28", (txtOS.Text + "").Contains("Server") ? "host" : "desktop");

            _timer.Change(0, 30_000);

            ShowResult(rs);
        });
    }

    private void btnNET8_Click(Object sender, EventArgs e)
    {
        var net = GetNetRuntime(false, false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            var rs = net.InstallNet8("8.0.3", (txtOS.Text + "").Contains("Server") ? "host" : "desktop");

            _timer.Change(0, 30_000);

            ShowResult(rs);
        });
    }

    private void btnFinal_Click(Object sender, EventArgs e)
    {
        var rs = MessageBox.Show("实在安装不上NET4时的终极大招，可能对系统文件有损坏！\r\n修复过程中可能自动重启系统，是否确定继续？", "危险提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (rs == DialogResult.Cancel) return;

        var net = GetNetRuntime(false, false);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            net.Install("netfxrepairtool.exe", null, "/repair /passive");

            _timer.Change(0, 30_000);

            MessageBox.Show("专家模式修复完成，请重新安装.NET运行时！");
        });
    }
    #endregion

    #region 服务通信
    private void btnNetwork_Click(Object sender, EventArgs e)
    {
        var url = txtServer.Text;
        if (url.IsNullOrEmpty()) return;

        using var span = _tracer?.NewSpan(nameof(btnNetwork_Click));
        try
        {
            //url = url.EnsureEnd("/") + "api";
            url = url.EnsureEnd("/");
            var client = new WebClientX();
            var html = client.GetHtml(url);
            if (html.IsNullOrEmpty()) return;

            XTrace.WriteLine(html);

            MessageBox.Show("测试通过");
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    private void btnDownloadAll_Click(Object sender, EventArgs e)
    {
        var net = GetNetRuntime(false, false);
        XTrace.WriteLine("目标目录：{0}", net.CachePath);

        // 检测缓存目录所有文件，如果哈希不对，直接删除
        foreach (var item in net.CachePath.AsDirectory().GetFiles("*.*", SearchOption.AllDirectories))
        {
            if (net.Hashs.TryGetValue(item.Name, out var hash))
            {
                var md5 = NetRuntime.GetMD5(item.FullName);
                if (hash != md5)
                {
                    XTrace.WriteLine("文件MD5校验失败，删除：{0}", item.FullName);
                    File.Delete(item.FullName);
                }
            }
        }

        TaskEx.Run(async () =>
        {
            var helper = new DownloadHelper { CachePath = net.CachePath };
            await helper.DownloadAllAsync();

            helper.BaseUrl = net.BaseUrl;
            await helper.DownloadAppsAsync();
        });
    }
    #endregion

    #region 自动安装软件
    void DetectTarget()
    {
        using var span = _tracer?.NewSpan(nameof(DetectTarget));

        var osVer = Environment.OSVersion.Version;
        var str = "";

        //A方案，NET8
        //B方案，NET6
        //C方案，NET8
        //D方案，NET45

        Invoke(new Action(() =>
        {
            var btns = new[] { btnInstallA, btnInstallB, btnInstallC, btnInstallD };
            foreach (var btn in btns)
            {
                btn.Enabled = false;
            }

            // WinXP
            if (osVer.Major <= 5)
            {
                //btnInstallC.Enabled = true;
                btnInstallD.Enabled = true;
                str = "WinXP";
            }
            // Vista
            else if (osVer.Major == 6 && osVer.Minor == 0)
            {
                //btnInstallC.Enabled = true;
                btnInstallD.Enabled = true;
                str = "Vista";
            }
            else if (osVer.Major == 6 && osVer.Minor == 1)
            {
                // Win7
                if (osVer.Revision <= 7600)
                {
                    //btnInstallC.Enabled = true;
                    btnInstallD.Enabled = true;
                    str = "Win7";
                }
                else
                // Win7Sp1
                {
                    btnInstallB.Enabled = true;
                    //btnInstallC.Enabled = true;
                    btnInstallD.Enabled = true;
                    str = "Win7Sp1";
                }
            }
            // Win10/Win11
            else if (osVer.Major >= 10)
            {
                btnInstallA.Enabled = true;
                btnInstallB.Enabled = true;
                btnInstallC.Enabled = true;
                btnInstallD.Enabled = true;
                str = "Win10/Win11";
            }
            else
            {
                btnInstallA.Enabled = true;
                btnInstallB.Enabled = true;
                btnInstallC.Enabled = true;
                btnInstallD.Enabled = true;
                str = txtOS.Text;
            }

            // 在.NET4.0中，禁止安装D方案（.NET2.0）
#if NET40
            //btnInstallD.Enabled = false;
#endif

            if (_x64)
                str += "（64位）";
            else
                str += "（32位）";
            txtTarget.Text = str;

            span?.SetTag(str);
        }));

        // 检测磁盘，决定安装目录
        var dis = DriveInfo.GetDrives();
        if (dis.Any(e => e.Name == @"D:\" && e.DriveType == DriveType.Fixed))
            _installPath = @"D:\agent\";
        else if (dis.Any(e => e.Name == @"C:\" && e.DriveType == DriveType.Fixed))
            _installPath = @"C:\agent\";
        else
            _installPath = dis[0].Name.CombinePath("agent");

        XTrace.WriteLine("安装路径：{0}", _installPath);
    }

    /// <summary>安装服务。启动进程</summary>
    /// <param name="svc"></param>
    /// <returns></returns>
    Boolean InstallService(WindowsService svc, String url)
    {
        var file = _installPath.CombinePath("StarAgent.exe");
        if (!File.Exists(file))
        {
            XTrace.WriteLine("找不到客户端文件");
            return false;
        }

        // 强制使用外部url地址控制内部文件
        var txt = Path.GetDirectoryName(file).CombinePath("server.txt");
        //if (!File.Exists(txt) && !url.IsNullOrEmpty())
        if (!url.IsNullOrEmpty())
        {
            File.WriteAllText(txt, url);
        }

        var args = "-install";
        var set = Setting.Current;
        if (!set.Server.IsNullOrEmpty()) args += " -server " + set.Server;

        var si = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,

            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,

            UseShellExecute = true,
            //RedirectStandardOutput = true,
            //RedirectStandardError = true,
        };

        var p = new Process { StartInfo = si };
        //p.OutputDataReceived += (s, e) => { if (e.Data != null) XTrace.WriteLine(e.Data); };
        //p.ErrorDataReceived += (s, e) => { if (e.Data != null) XTrace.Log.Error(e.Data); };

        p.Start();
        //p.BeginOutputReadLine();
        //p.BeginErrorReadLine();

        if (!p.WaitForExit(15_000)) return false;

        return true;
    }

    private void btnInstallA_Click(Object sender, EventArgs e)
    {
        var svc = new WindowsService();
        if (!svc.IsAdministrator())
        {
            MessageBox.Show("需要管理员权限运行本软件！");
            return;
        }

        groupBox3.Enabled = false;
        var url = txtServer.Text;

        var server = txtServer.Text;
        var net = GetNetRuntime(false, true);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            net.InstallNet8("8.0.3", (txtOS.Text + "").Contains("Server") ? "host" : "desktop");
            _timer.Change(1000, 30_000);

            try
            {
                if (svc.IsRunning(_serviceName)) svc.Stop(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            net = GetNetRuntime(true, true);
            var rs = net.Install("staragent80.zip", null);
            if (rs) rs = InstallService(svc, url);
            _timer.Change(1000, 30_000);

            PostLog(server, nameof(btnInstallA_Click));

            if (!rs)
                MessageBox.Show("安装失败！");
            else
                Invoke(new Action(() =>
                {
                    groupBox3.Enabled = true;

                    MessageBox.Show("安装完成！");
                }));
        });
    }

    private void btnInstallB_Click(Object sender, EventArgs e)
    {
        var svc = new WindowsService();
        if (!svc.IsAdministrator())
        {
            MessageBox.Show("需要管理员权限运行本软件！");
            return;
        }

        groupBox3.Enabled = false;
        var url = txtServer.Text;

        var server = txtServer.Text;
        var net = GetNetRuntime(false, true);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            net.InstallNet6("6.0.28", (txtOS.Text + "").Contains("Server") ? "host" : "desktop");
            _timer.Change(1000, 30_000);

            try
            {
                if (svc.IsRunning(_serviceName)) svc.Stop(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            net = GetNetRuntime(true, true);
            var rs = net.Install("staragent60.zip", null);
            if (rs) rs = InstallService(svc, url);
            _timer.Change(1000, 30_000);

            PostLog(server, nameof(btnInstallB_Click));

            if (!rs)
                MessageBox.Show("安装失败！");
            else
                Invoke(new Action(() =>
                {
                    groupBox3.Enabled = true;

                    MessageBox.Show("安装完成！");
                }));
        });
    }

    private void btnInstallC_Click(Object sender, EventArgs e)
    {
        var svc = new WindowsService();
        if (!svc.IsAdministrator())
        {
            MessageBox.Show("需要管理员权限运行本软件！");
            return;
        }

        groupBox3.Enabled = false;
        var url = txtServer.Text;

        var server = txtServer.Text;
        var net = GetNetRuntime(true, true);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            try
            {
                if (svc.IsRunning(_serviceName)) svc.Stop(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            var rs = net.Install("staragent80.zip", null);
            if (rs) rs = InstallService(svc, url);
            _timer.Change(1000, 30_000);

            PostLog(server, nameof(btnInstallC_Click));

            if (!rs)
                MessageBox.Show("安装失败！");
            else
                Invoke(new Action(() =>
                {
                    groupBox3.Enabled = true;

                    MessageBox.Show("安装完成！");
                }));
        });
    }

    private void btnInstallD_Click(Object sender, EventArgs e)
    {
        var svc = new WindowsService();
        if (!svc.IsAdministrator())
        {
            MessageBox.Show("需要管理员权限运行本软件！");
            return;
        }

        groupBox3.Enabled = false;
        var url = txtServer.Text;

        var server = txtServer.Text;
        var net = GetNetRuntime(true, true);
        ThreadPoolX.QueueUserWorkItem(() =>
        {
            try
            {
                if (svc.IsRunning(_serviceName)) svc.Stop(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            var rs = net.Install("staragent45.zip", null);
            if (rs) rs = InstallService(svc, url);
            _timer.Change(1000, 30_000);

            PostLog(server, nameof(btnInstallD_Click));

            if (!rs)
                MessageBox.Show("安装失败！");
            else
                Invoke(new Action(() =>
                {
                    groupBox3.Enabled = true;

                    MessageBox.Show("安装完成！");
                }));
        });
    }

    private void btnUninstall_Click(Object sender, EventArgs e)
    {
        var svc = new WindowsService();
        if (!svc.IsAdministrator())
        {
            MessageBox.Show("需要管理员权限运行本软件！");
            return;
        }

        var server = txtServer.Text;
        using var span = _tracer?.NewSpan(nameof(btnUninstall_Click));
        try
        {
            try
            {
                svc.Stop(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
            try
            {
                svc.Remove(_serviceName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }

            _timer.Change(1000, 30_000);

            PostLog(server, nameof(btnUninstall_Click));
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region 辅助
    private void btnReport_Click(Object sender, EventArgs e)
    {
        PostLog(txtServer.Text, nameof(btnReport_Click));
    }

    void PostLog(String server, String action)
    {
        //// 上传客户端日志
        //var di = _installPath.CombinePath("Log").AsDirectory();
        //if (di.Exists)
        //{
        //    var fi = di.GetFiles("*.log").OrderByDescending(e => e.LastWriteTime).FirstOrDefault();
        //    if (fi != null) PostLog(server, fi, action);
        //}

        //// 上传自己的日志
        //di = @".\Log".AsDirectory();
        //if (di.Exists)
        //{
        //    var fi = di.GetFiles("*.log").OrderByDescending(e => e.LastWriteTime).FirstOrDefault();
        //    if (fi != null) PostLog(server, fi, action);
        //}
    }

    void PostLog(String server, FileInfo fi, String action)
    {
        using var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // 取最后100K
        var retain = fs.Length - 100 * 1024;
        if (retain > 0) fs.Seek(retain, SeekOrigin.Begin);

        XTrace.WriteLine("发现日志：{0}", fi.FullName);
        XTrace.WriteLine("日志大小：{0}", fs.Length);

        var buf = new Byte[fs.Length - fs.Position];
        var count = fs.Read(buf, 0, buf.Length);

        buf = buf.Compress();
        XTrace.WriteLine("压缩大小：{0}", buf.Length);

#if NET40_OR_GREATER
        var http = new HttpClient { BaseAddress = new Uri(server) };
        var headers = http.DefaultRequestHeaders;
#else
        var http = new WebClientX { BaseAddress = server };
        var headers = http.Headers;
#endif
        headers.Add("X-Action", action);
        headers.Add("X-ClientId", _info.UUID);
        headers.Add("X-MachineName", Environment.MachineName);
        headers.Add("X-UserName", Environment.UserName);
        headers.Add("X-IP", NetHelper.MyIP() + "");

#if NET40_OR_GREATER
        var content = new ByteArrayContent(buf);
        var rs = http.PostAsync("/log", content).Result;
        rs.EnsureSuccessStatusCode();
        var html = rs.Content.ReadAsStringAsync().Result;
#else
        var rs = http.UploadData("/log", buf);
        var html = rs.ToStr();
#endif

        XTrace.WriteLine("上传完成！{0}", html);
    }
    #endregion
}