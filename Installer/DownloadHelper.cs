using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Installer;
using NewLife;
using NewLife.Http;
using NewLife.Log;

namespace BuildTool;

public class DownloadHelper
{
    public String BaseUrl { get; set; } = "http://x.newlifex.com/dotnet/";

    public String CachePath { get; set; }

    public async Task DownloadAllAsync()
    {
        XTrace.WriteLine("开始检测下载所有组件");

        await Check($"dotnet-runtime-{NetRuntime.Version8}-win-x64.exe");
        await Check($"dotnet-runtime-{NetRuntime.Version8}-win-x86.exe");
        await Check($"dotnet-runtime-{NetRuntime.Version6}-win-x64.exe");
        await Check($"dotnet-runtime-{NetRuntime.Version6}-win-x86.exe");

        await Check("ndp481-x86-x64-allos-enu.exe");
        await Check("ndp481-x86-x64-allos-chs.exe");
        await Check("ndp48-x86-x64-allos-enu.exe");
        await Check("ndp48-x86-x64-allos-chs.exe");

        await Check("dotNetFx40_Full_x86_x64.exe");
        await Check("NetFx20SP2_x86.exe");

        await Check("vc2019/VC_redist.x64.exe");
        await Check("vc2019/VC_redist.x86.exe");
        await Check("win7/Windows6.1-KB3063858-x64.msu");
        await Check("win7/Windows6.1-KB3063858-x86.msu");
        await Check("win7/MsRootCert.zip");

        await Check("aspnetcore-runtime-8.0.5-linux-loongarch64.tar.gz");
        await Check($"aspnetcore-runtime-{NetRuntime.Version8}-linux-arm64.tar.gz");
        await Check($"aspnetcore-runtime-{NetRuntime.Version8}-linux-x64.tar.gz");
        await Check("dotnet-sdk-3.1.7-rc1-mips64el.deb");

        XTrace.WriteLine("所有组件下载完成！");
    }

    public async Task DownloadAppsAsync()
    {
        XTrace.WriteLine("开始检测下载所有应用");

        await Check("star/staragent80.zip");
        await Check("star/staragent60.zip");
        await Check("star/staragent45.zip");

        XTrace.WriteLine("所有应用下载完成！");
    }

    async Task Check(String file, String url = null)
    {
        XTrace.WriteLine("检查：{0}", file);

        var fi = CachePath.CombinePath(file);
        if (File.Exists(fi)) return;

        var p = Path.GetDirectoryName(fi);
        if (!Directory.Exists(p)) Directory.CreateDirectory(p);

        if (String.IsNullOrEmpty(url)) url = file;
        if (!url.StartsWith("http")) url = BaseUrl.EnsureEnd("/") + url;

        XTrace.WriteLine("下载：{0}", url);

#if NET40_OR_GREATER || NETCOREAPP
        using var client = new HttpClient();
        await client.DownloadFileAsync(url, fi);
#else
        using var client = new WebClient();
        client.DownloadFile(url, fi);
#endif

        XTrace.WriteLine("下载完成！");
    }
}
