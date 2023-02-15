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

    //�ж��Ƿ�װ1.0�汾
    if (0 == strCheckVer.CompareNoCase(_T("1.0")))
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\.NETFramework\\Policy\\v1.0\\3705"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("1.1")))//�ж��Ƿ�װ1.1�汾
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v1.1.4322"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("2.0")))//�ж��Ƿ�װ2.0�汾
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("3.0")))//�ж��Ƿ�װ3.0�汾
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v3.0\\Setup"), _T("InstallSuccess"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("3.5")))//�ж��Ƿ�װ3.5�汾
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v3.5"), _T("Install"), dwValue);
    }
    else if (0 == strCheckVer.CompareNoCase(_T("4.0")))//�ж��Ƿ�װ4.0�����ͻ���
    {
        ReadKeyValue(HKEY_LOCAL_MACHINE, _T("Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Client"), _T("Install"), dwValue);
        if (0 == dwValue)
        {
            //�ж��Ƿ�װ4.0�����汾�ͻ���
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

//��ָ���ļ����ע����ڶ�ȡһ��DWORDֵ
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
//��ָ���ļ����ע����ڶ�ȡһ���ַ���(Unicode)
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
