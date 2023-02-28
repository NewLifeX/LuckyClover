#include "BaseInclude.h"
#include "CDownloadFile.h"
#include "CGetOSVersion.h"
#include "CRegistryVisit.h"

using namespace std;

bool CheckRuntime();
void InstallNet20();
void InstallNet40();
void InstallNet45();
void InstallNet48();

bool _silent = false;

int main(int argc, char* argv[])
{
    //检测OS版本状态
    CGetOSVersion cOSVer;
    int iOSMainVerNum = 0;
    int iOSSubVerNum = 0;
    bool bIsServer = false;

    cout << "幸运草 v1.0.2023.215 NewLife" << endl;
    cout << "检测并安装主流.NET运行时。" << endl;
    cout << "操作系统: " << cOSVer.GetOSVersionDesc(iOSMainVerNum, iOSSubVerNum, bIsServer) << "\n";
    cout << "用法：clover.exe [net2|net4|net45|net48] -silent" << endl;
    cout << "本地server.txt文件设置下载文件的服务器地址。" << endl;

    CheckRuntime();

    // 读取静默安装标记
    for (int i = 0; i < argc; ++i)
    {
        string strArg = argv[i];
        if (strArg == "-silent") _silent = true;
    }

    string ver = "";
    if (argc >= 2)
        ver.assign(argv[1]);
    else if (iOSMainVerNum <= 5)
        ver = "net2";
    else
        ver = "net4";

    if (ver == "net2" || ver == "net20") {
        InstallNet20();
    }
    else if (ver == "net4" || ver == "net40") {
        InstallNet40();
    }
    else if (ver == "net45") {
        InstallNet45();
    }
    else if (ver == "net48") {
        InstallNet48();
    }

    return 0;
}

bool CheckRuntime()
{
    //检测OS是否安装 .net Framework客户端版本
    CRegistryVisit cRegVisit;

    cout << endl;
    cout << "已安装版本：" << endl;
    bool bNet20 = cRegVisit.CheckIsInstallNet(_T("2.0"));
    if (bNet20) {
        cout << "v2.0" << endl;
    }

    bool bNet30 = cRegVisit.CheckIsInstallNet(_T("3.0"));
    if (bNet30) {
        cout << "v3.0" << endl;
    }

    bool bNet35 = cRegVisit.CheckIsInstallNet(_T("3.5"));
    if (bNet35) {
        cout << "v3.5" << endl;
    }

    bool bNet40 = cRegVisit.CheckIsInstallNet(_T("4.0"));
    if (bNet40) {
        cout << "v4.0" << endl;
    }

    bool bNet45 = cRegVisit.CheckIsInstallNet(_T("4.5"));
    if (bNet45) {
        cout << "v4.5" << endl;
    }

    bool bNet48 = cRegVisit.CheckIsInstallNet(_T("4.8"));
    if (bNet48) {
        cout << "v4.8" << endl;
    }

    //if (ver == "net2" || ver == "net20") return bNet20;
    //if (ver == "net3" || ver == "net30") return bNet30;
    //if (ver == "net35") return bNet35;
    //if (ver == "net4" || ver == "net40") return bNet40;
    //if (ver == "net45") return bNet45;
    //if (ver == "net48") return bNet48;

    return true;
}

const string GetFile(const string& ver)
{
    if (ver == "net2" || ver == "net20") return "NetFx20SP2_x86.exe";
    if (ver == "net3" || ver == "net30" || ver == "net35") return "NetFx20SP2_x86.exe";
    if (ver == "net4" || ver == "net40") return "dotNetFx40_Full_x86_x64.exe";
    if (ver == "net45") return "NDP452-KB2901907-x86-x64-AllOS-ENU.exe";
    if (ver == "net48") return "ndp48-x86-x64-allos-enu.exe";

    //构造本地文件目录绝对路径 -- 获取当前工作目录
    TCHAR szPath[MAX_PATH] = { 0 };
    CString strFilePath;
    if (GetModuleFileName(NULL, szPath, MAX_PATH))
    {
        CString strTemp(szPath);
        CString strPathTemp;
        strPathTemp = strTemp.Left(strTemp.ReverseFind('\\'));
        strFilePath = strPathTemp + _T("\\dotNetFx40_Full_x86_x64.exe");
    }

    return "";
}

bool Download(const string& localFile, const string& remoteFile)
{
    string server = "http://x.newlifex.com";

    // 从配置文件读取服务器地址
    char szPath[MAX_PATH] = { 0 };
    if (GetModuleFileName(NULL, szPath, MAX_PATH))
    {
        CString strTemp(szPath);
        CString strCurrentDir = strTemp.Left(strTemp.ReverseFind('\\'));
        CString strConfigFilePath = strCurrentDir + _T("\\server.txt");
        if (::PathFileExists(strConfigFilePath)) {
            ifstream infile;
            infile.open(strConfigFilePath);
            if (infile) {
                char line[64];
                if (infile.getline(line, sizeof(line))) {
                    string str(line);
                    if (!str.empty()) {
                        server = str;
                    }
                }
            }

            infile.close();
        }
    }

    CDownloadFile cDownFile;
    string remote = "/dotnet/" + remoteFile;

    cout << "下载：" << server << remote << endl;
    cout << "保存：" << localFile << endl;

    //执行下载操作
    int iTryCount = 0;
    while (++iTryCount < 15)
    {
        if (cDownFile.Download(localFile.c_str(), server.c_str(), 80, remote.c_str())) return true;

        //沉默1秒后重试
        Sleep(1 * 1000);
    }

    return false;
}

bool Install(const string& fileName, const string& baseUrl, const string& arg)
{
    cout << "安装：" << fileName << endl;

    //构造本地文件目录绝对路径 -- 获取当前工作目录
    TCHAR szPath[MAX_PATH] = { 0 };
    if (!GetModuleFileName(NULL, szPath, MAX_PATH)) return false;

    CString strTemp(szPath);
    CString strPathTemp = strTemp.Left(strTemp.ReverseFind('\\'));

    string file(strPathTemp);
    file += "\\" + fileName;
    string remoteFile = fileName;
    if (baseUrl.empty()) remoteFile = baseUrl + fileName;

    // 不存在则下载
    if (!::PathFileExists(file.c_str())) {
        if (!Download(file, remoteFile)) {
            cout << "下载失败！" << endl;
            return false;
        }
    }

    // 仍然不存在，则退出
    if (!::PathFileExists(file.c_str())) return false;

    cout << "开始安装 " << fileName << " ......" << endl;

    // 执行.net Framework安装文件(exe),带参数
    // system 执行命令并等待完成，ShellExecute 不等返回
    //ShellExecute(NULL, _T("open"), file.c_str(), _T("/passive /promptrestart"), NULL, SW_HIDE);
    if (!_silent)
        system(file.c_str());
    else if (arg.empty())
        system(("\"" + file + "\" /passive /promptrestart").c_str());
    else
        system(("\"" + file + "\" " + arg).c_str());
}

bool Install(const string& fileName)
{
    return Install(fileName, "", "");
}

void InstallNet20()
{
    CRegistryVisit cRegVisit;

    bool bNet = cRegVisit.CheckIsInstallNet(_T("2.0"));
    if (bNet) {
        cout << "已安装.NET2.0" << endl;
        return;
    }

    Install("NetFx20SP2_x86.exe");
}

void InstallNet40()
{
    CRegistryVisit cRegVisit;

    bool bNet = cRegVisit.CheckIsInstallNet(_T("4.0"));
    if (bNet) {
        cout << "已安装.NET4.0" << endl;
        return;
    }

    Install("dotNetFx40_Full_x86_x64.exe");
}

void InstallNet45()
{
    CRegistryVisit cRegVisit;

    bool bNet = cRegVisit.CheckIsInstallNet(_T("4.5"));
    if (bNet) {
        cout << "已安装.NET4.5" << endl;
        return;
    }

    Install("NDP452-KB2901907-x86-x64-AllOS-ENU.exe");
    Install("NDP452-KB2901907-x86-x64-AllOS-CHS.exe");
}

void InstallNet48()
{
    CRegistryVisit cRegVisit;

    bool bNet = cRegVisit.CheckIsInstallNet(_T("4.8"));
    if (bNet) {
        cout << "已安装.NET4.8" << endl;
        return;
    }

    CGetOSVersion cOSVer;
    int iOSMainVerNum = 0;
    int iOSSubVerNum = 0;
    bool bIsServer = false;

    cOSVer.GetOSVersionDesc(iOSMainVerNum, iOSSubVerNum, bIsServer);

    bool isWin7 = iOSMainVerNum == 6 && iOSSubVerNum == 1;
    if (isWin7)
        Install("Windows6.1-KB3063858-x64.msu", "win7/", "/quiet /norestart");

    // win10/win11 中安装 .NET4.8.1
    if (iOSMainVerNum >= 10)
    {
        Install("ndp481-x86-x64-allos-enu.exe", "", "/passive /promptrestart /showfinalerror");
        Install("ndp481-x86-x64-allos-chs.exe", "", "/passive /promptrestart /showfinalerror");
    }
    else
    {
        Install("ndp48-x86-x64-allos-enu.exe", "", "/passive /promptrestart /showfinalerror");
        Install("ndp48-x86-x64-allos-chs.exe", "", "/passive /promptrestart /showfinalerror");
    }
}