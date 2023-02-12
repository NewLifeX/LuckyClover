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
	/// 在指定的计算机注册表内读取一个DWORD值
	/// </summary>
	/// <param name="hKeyRoot">注册表根值</param>
	/// <param name="lpPath">注册表路径</param>
	/// <param name="lpKey">键名</param>
	/// <param name="dwVal">获取到的键值</param>
	/// <returns></returns>
	BOOL ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, DWORD& dwVal);
	/// <summary>
	/// 在指定的计算机注册表内读取一个字符串(Unicode)
	/// </summary>
	/// <param name="hKeyRoot">注册表根值</param>
	/// <param name="lpPath">注册表路径</param>
	/// <param name="lpKey">键名</param>
	/// <param name="strVal">键值</param>
	/// <returns></returns>
	BOOL ReadKeyValue(HKEY hKeyRoot, LPCTSTR lpPath, LPCTSTR lpKey, CString& strVal);	
};

