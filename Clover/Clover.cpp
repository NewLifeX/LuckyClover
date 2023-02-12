// Clover_re.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include "BaseInclude.h"
#include "CDownloadFile.h"
#include "CGetOSVersion.h"
#include "CRegistryVisit.h"

int main()
{
    std::cout << "Hello World!\n";
    //检测OS版本状态
    CGetOSVersion cOSVer;
    int iOSMainVerNum = 0;
    int iOSSubVerNum = 0;
    bool bIsServer = false;

    std::cout << "操作系统版本:%s" << cOSVer.GetOSVersionDesc(iOSMainVerNum, iOSSubVerNum, bIsServer) << "\n";

    //检测OS是否安装 .net Framework客户端版本
    CRegistryVisit cRegVisit;
    bool bInstall = cRegVisit.CheckIsInstallNet(_T("4.0"));
    
    //如果未安装.net Framework,则判断本地指定目录是否存在.net Framework安装文件
    if (!bInstall)
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
            while (++iTryCount<4)
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
            //执行.net Framework安装文件(exe),带参数
            ShellExecute(NULL, _T("open"), strFilePath, _T("/passive /promptrestart"), NULL, SW_HIDE);
        }
    }

    //如果已安装.net Framework,流程结束

    return 0;
}
