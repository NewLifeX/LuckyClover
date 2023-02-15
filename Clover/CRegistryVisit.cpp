#include "CRegistryVisit.h"

CRegistryVisit::CRegistryVisit()
{
}

CRegistryVisit::~CRegistryVisit()
{

}

bool CRegistryVisit::CheckIsInstallNet(CString strCheckVer)
{
    bool bReturn = false;
    DWORD dwValue = 0;

    //判断是否安装1.0版本
    if (0 == strCheckVer.CompareNoCase(_T("1.0")))
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\.NETFramework\\Policy\\v1.0\\3705"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("1.1")))//判断是否安装1.1版本
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v1.1.4322"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("2.0")))//判断是否安装2.0版本
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("3.0")))//判断是否安装3.0版本
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v3.0\\Setup"), _T("InstallSuccess"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("3.5")))//判断是否安装3.5版本
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v3.5"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("4.0")))//判断是否安装4.0精简版客户端
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client"), _T("Install"), dwValue);
        if (0 == dwValue)
        {
            //判断是否安装4.0完整版本客户端
            ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full"), _T("Install"), dwValue);
        }
    }
    else if (0 == strCheckVer.CompareNoCase(_T("4.5")))
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full"), _T("Release"), dwValue);
        dwValue = dwValue >= 378389 ? dwValue : 0;
    }
    else if (0 == strCheckVer.CompareNoCase(_T("4.8")))
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full"), _T("Release"), dwValue);
        dwValue = dwValue >= 528040 ? dwValue : 0;
    }

    return dwValue > 0;
}

//在指定的计算机注册表内读取一个DWORD值
BOOL CRegistryVisit::ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, DWORD& dwVal)
{
    BOOL bResult = FALSE;
    HKEY hKey = NULL;
    HKEY hRemoteKey = NULL;
    CString strHostName = _T("");
    if (ERROR_SUCCESS == RegConnectRegistry(strHostName, hKeyRoot, &hRemoteKey))
    {
        if (ERROR_SUCCESS == RegOpenKeyEx(hRemoteKey, lpPath, 0L, KEY_READ, &hKey))
        {
            DWORD dwType = 0;
            DWORD dwSize = sizeof(DWORD);
            DWORD dwDest = 0;
            if (ERROR_SUCCESS == RegQueryValueEx(hKey,
                lpKey,
                NULL,
                &dwType,
                (LPBYTE)&dwDest,
                &dwSize))
            {
                dwVal = dwDest;
                bResult = TRUE;
            }
            RegCloseKey(hKey);
        }
        RegCloseKey(hRemoteKey);
    }

    return bResult;
}
//在指定的计算机注册表内读取一个字符串(Unicode)
BOOL CRegistryVisit::ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, CString& strVal)
{
    BOOL bResult = FALSE;
    HKEY hKey = NULL;
    HKEY hRemoteKey = NULL;
    CString strHostName = _T("");

    if (ERROR_SUCCESS == RegConnectRegistry(strHostName, hKeyRoot, &hRemoteKey))
    {
        if (ERROR_SUCCESS == RegOpenKeyEx(hRemoteKey, lpPath, 0L, KEY_READ, &hKey))
        {
            DWORD dwErrorCode = 0;
            DWORD dwType = 0;
            DWORD dwSize = 8192;
            TCHAR szVal[8192] = _T("");
            if (ERROR_SUCCESS == (dwErrorCode = RegQueryValueEx(hKey,
                lpKey,
                NULL,
                &dwType,
                (LPBYTE)szVal,
                &dwSize)))
            {
                strVal.Format(_T("%s"), szVal);
                bResult = TRUE;
            }
            RegCloseKey(hKey);
        }
        RegCloseKey(hRemoteKey);
    }

    return bResult;
}
