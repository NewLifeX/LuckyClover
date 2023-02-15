#include "CGetOSVersion.h"

CGetOSVersion::CGetOSVersion()
	: m_iOSMainNum(0)
    , m_iOSSubNum(0)
    , m_bIsServer(false)
{
    m_strOSDesc = "未知的系统版本";
}

CGetOSVersion::~CGetOSVersion()
{

}

/// <summary>
/// 对外提供接口,输出本地运行操作系统的版本信息
/// </summary>
/// <param name="iOSVerNum">操作系统大版本号
/// 1,windows 2000
/// 2,windows XP
/// 3,Windows XP 64
/// 4,windows vista
/// 5,windows 7
/// 6,windows 8
/// 7,windows 8
/// 8.windows 10
/// 9.windows 11
/// </param>
/// <param name="iSubVer">操作系统小版本号，一般描述为如：SP1,SP2 ... </param>
/// <returns>描述操作系统的版本简称，如 "windows XP SP1" </returns>
std::string CGetOSVersion::GetOSVersionDesc(int& iOSMainNum,int& iSubVer, bool& bIsServer)
{
    if (CheckOSVer())
    {
        iOSMainNum = m_iOSMainNum;
        iSubVer = m_iOSSubNum;
        bIsServer = m_bIsServer;
    }

    return m_strOSDesc;
}


/// <summary>
/// 内部调用接口，检测本地操作系统版本信息
/// </summary>
/// <returns>bool,值为真则表示检测成功</returns>
bool CGetOSVersion::CheckOSVer()
{
    bool bExcute = true;

    m_iOSMainNum = 0;
    m_iOSSubNum = 0;
    m_bIsServer = false;
    m_strOSDesc = "未知的系统版本";

    try
    {
/*
        if (IsWindowsXPOrGreater())
        {
            m_iOSMainNum = 2;
            m_iOSSubNum = 0;
            m_strOSDesc = "Windows XP";
        }

        if (IsWindowsXPSP1OrGreater())
        {
            m_iOSMainNum = 2;
            m_iOSSubNum = 1;
            m_strOSDesc = "Windows XP SP1";
        }

        if (IsWindowsXPSP2OrGreater())
        {
            m_iOSMainNum = 2;
            m_iOSSubNum = 2;
            m_strOSDesc = "Windows XP SP2";
        }

        if (IsWindowsXPSP3OrGreater())
        {
            m_iOSMainNum = 2;
            m_iOSSubNum = 3;
            m_strOSDesc = "Windows XP SP3";
        }

        if (IsWindowsVistaOrGreater())
        {
            m_iOSMainNum = 3;
            m_iOSSubNum = 0;
            m_strOSDesc = "Windows Vista";
        }

        if (IsWindowsVistaSP1OrGreater())
        {
            m_iOSMainNum = 3;
            m_iOSSubNum = 1;
            m_strOSDesc = "Windows Vista SP1";
        }

        if (IsWindowsVistaSP2OrGreater())
        {
            m_iOSMainNum = 3;
            m_iOSSubNum = 2;
            m_strOSDesc = "Windows Vista SP2";
        }

        if (IsWindows7OrGreater())
        {
            m_iOSMainNum = 4;
            m_iOSSubNum = 0;
            m_strOSDesc = "Windows 7";
        }

        if (IsWindows7SP1OrGreater())
        {
            m_iOSMainNum = 4;
            m_iOSSubNum = 1;
            m_strOSDesc = "Windows 7 SP1";
        }

        if (IsWindows8OrGreater())
        {
            m_iOSMainNum = 5;
            m_iOSSubNum = 0;
            m_strOSDesc = "Windows 8";
        }

        if (IsWindows8Point1OrGreater())
        {
            m_iOSMainNum = 5;
            m_iOSSubNum = 1;
            m_strOSDesc = "Windows 8.1";
        }

        if (IsWindows10OrGreater())
        {
            m_iOSMainNum = 6;
            m_iOSSubNum = 0;
            m_strOSDesc = "Windows 10";
        }

        if (IsWindowsServer())
        {
            m_bIsServer = true;
        }
*/

        typedef void(__stdcall* NTPROC)(DWORD*, DWORD*, DWORD*);
        //加载DLL
        HINSTANCE hinst = LoadLibrary(TEXT("ntdll.dll"));
        if (NULL == hinst)
            throw - 1;
        //获取函数地址
        NTPROC GetNtVersionNumbers = (NTPROC)GetProcAddress(hinst, "RtlGetNtVersionNumbers");

        bool bServer = false;
        DWORD dwMajor = 0;
        DWORD dwMinor = 0;
        DWORD dwBuildNumber = 0;
        if (NULL == GetNtVersionNumbers)
        {
            FreeLibrary(hinst);
            throw - 2;
        }
        //获取信息
        GetNtVersionNumbers(&dwMajor, &dwMinor, &dwBuildNumber);
        //释放句柄
        FreeLibrary(hinst);

        //printf("Windows版本 : %d.%d\n", dwMajor, dwMinor);

        //判断各个系统版本
        if (5 == dwMajor)
        {
            switch (dwMinor)
            {
            case 0:
                m_iOSMainNum = 1;
                m_iOSSubNum = 0;
                m_strOSDesc = "Windows 2000";
                break;
            case 1:
                m_iOSMainNum = 2;
                m_iOSSubNum = 0;
                m_strOSDesc = "Windows XP";
                break;
            case 2:
                m_iOSMainNum = 3;
                m_iOSSubNum = 0;
                if (bServer)
                {
                    m_strOSDesc = "Windows Server 2003";
                }
                else
                {
                    m_strOSDesc = "Windows XP 64";
                }
                
                break;
            }

        }

        if (6 == dwMajor)
        {
            switch (dwMinor)
            {
            case 0:
                m_iOSMainNum = 4;
                m_iOSSubNum = 0;
                m_strOSDesc = "Windows Vista";
                if (bServer)
                {
                    m_strOSDesc = "Windows Server 2008";
                }
                else
                {
                    m_strOSDesc = "Windows Vista";
                }
                break;
            case 1:
                m_iOSMainNum = 5;
                m_iOSSubNum = 0;
                if (bServer)
                {
                    m_strOSDesc = "Windows Server 2008 R2";
                }
                else
                {
                    m_strOSDesc = "Windows 7";
                }
                break;
            case 2:
                m_iOSMainNum = 6;
                m_iOSSubNum = 0;
                if (bServer)
                {
                    m_strOSDesc = "Windows Server 2012";
                }
                else
                {
                    m_strOSDesc = "Windows 8";
                }
                break;
            case 3:
                m_iOSMainNum = 7;
                m_iOSSubNum = 0;
                if (bServer)
                {
                    m_strOSDesc = "Windows Server 2012 R2";
                }
                else
                {
                    m_strOSDesc = "Windows 8.1";
                }
                break;
            }
        }

        if (dwMajor == 10 && dwMinor == 0)
        {
            m_iOSMainNum = 8;
            m_iOSSubNum = 0;
            if (bServer)
            {
                m_strOSDesc = "Windows Server 2016";
            }
            else
            {
                m_strOSDesc = "Windows 10";
            }
        }
    }
    catch (...)
    {
        bExcute = false;

        m_iOSMainNum = 0;
        m_iOSSubNum = 0;
        m_bIsServer = false;
        m_strOSDesc = "未知的系统版本";
    }
 
    return bExcute;
}