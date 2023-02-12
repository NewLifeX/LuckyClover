#pragma once
#include "BaseInclude.h"

class CRegistryVisit
{
public:
	CRegistryVisit();
	~CRegistryVisit();
public:
	bool CheckIsInstallNet(CString strCheckVer);
private:
	/// <summary>
	/// ��ָ���ļ����ע����ڶ�ȡһ��DWORDֵ
	/// </summary>
	/// <param name="hKeyRoot">ע����ֵ</param>
	/// <param name="lpPath">ע���·��</param>
	/// <param name="lpKey">����</param>
	/// <param name="dwVal">��ȡ���ļ�ֵ</param>
	/// <returns></returns>
	BOOL ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, DWORD& dwVal);
	/// <summary>
	/// ��ָ���ļ����ע����ڶ�ȡһ���ַ���(Unicode)
	/// </summary>
	/// <param name="hKeyRoot">ע����ֵ</param>
	/// <param name="lpPath">ע���·��</param>
	/// <param name="lpKey">����</param>
	/// <param name="strVal">��ֵ</param>
	/// <returns></returns>
	BOOL ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, CString& strVal);	
};

