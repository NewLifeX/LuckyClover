// Clover.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include "BaseInclude.h"
#include "CDownloadFile.h"
#include "CGetOSVersion.h"
#include "CRegistryVisit.h"

int main()
{
    //检测OS版本状态
    CGetOSVersion cOSVer;
    int iOSMainVerNum = 0;
    int iOSSubVerNum = 0;
    bool bIsServer = false;

    std::cout << "幸运草 v1.0.2023.213" << std::endl;
    std::cout << "检测并安装主流.NET运行时。" << std::endl;
    std::cout << "操作系统: " << cOSVer.GetOSVersionDesc(iOSMainVerNum, iOSSubVerNum, bIsServer) << "\n";

    //检测OS是否安装 .net Framework客户端版本
    CRegistryVisit cRegVisit;

    std::cout << std::endl;
    std::cout << "已安装版本：" << std::endl;
    bool bNet20 = cRegVisit.CheckIsInstallNet(_T("2.0"));
    if (bNet20) {
        std::cout << "v2.0" << std::endl;
    }

    bool bNet30 = cRegVisit.CheckIsInstallNet(_T("3.0"));
    if (bNet30) {
        std::cout << "v3.0" << std::endl;
    }

    bool bNet35 = cRegVisit.CheckIsInstallNet(_T("3.5"));
    if (bNet35) {
        std::cout << "v3.5" << std::endl;
    }

    bool bNet40 = cRegVisit.CheckIsInstallNet(_T("4.0"));
    if (bNet40) {
        std::cout << "v4.0" << std::endl;
    }

    bool bNet45 = cRegVisit.CheckIsInstallNet(_T("4.5"));
    if (bNet45) {
        std::cout << "v4.5" << std::endl;
    }

    bool bNet48 = cRegVisit.CheckIsInstallNet(_T("4.8"));
    if (bNet48) {
        std::cout << "v4.8" << std::endl;
    }

    //如果未安装.net Framework,则判断本地指定目录是否存在.net Framework安装文件
    if (!bNet40)
    {
        bool bExitsFile = false;
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
        //判断是否存在该文件
        if (::PathFileExists(strFilePath))
        {
            bExitsFile = true;
        }
        //不存在，尝试从网络(指定URL)下载安装文件
        if (!bExitsFile)
        {
            CDownloadFile cDownFile;

            CString strLocalFilePath(strFilePath);
            CString strGetDataPath(_T("/dotnet/dotNetFx40_Full_x86_x64.exe"));
            CString strNetIP(_T("http://x.newlifex.com"));
            UINT nPort = 80;

            //执行下载操作
            int iTryCount = 0;
            while (++iTryCount < 4)
            {
                if (false == (bExitsFile = cDownFile.Download(strLocalFilePath, strNetIP, nPort, strGetDataPath)))
                {
                    //沉默10秒后重试
                    Sleep(10 * 1000);
                }
                else
                {
                    break;
                }
            }
        }
        //存在则尝试安装
        if (bExitsFile)
        {
            std::cout << "开始安装 .NET4.0 ……" << std::endl;

            //执行.net Framework安装文件(exe),带参数
            ShellExecute(NULL, _T("open"), strFilePath, _T("/passive /promptrestart"), NULL, SW_HIDE);
        }
    }

    //如果已安装.net Framework,流程结束

    std::cin.get();

    return 0;
}
