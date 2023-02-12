#pragma once
#include "BaseInclude.h"

class CGetOSVersion
{
public:
	CGetOSVersion();
	~CGetOSVersion();
//Functions
public:
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
	/// <param name="iSubVer">操作系统小版本号，一般描述为如：SP1</param>
	/// <returns>描述操作系统的版本简称，如 "windows XP SP1" </returns>
	std::string GetOSVersionDesc(int& iOSVerNum, int& iSubVer,bool& bIsServer);
private:
	/// <summary>
	/// 内部调用接口，检测本地操作系统版本信息
	/// </summary>
	/// <returns>bool,值为真则表示检测成功</returns>
	bool CheckOSVer();
//Attributes
private:
	int m_iOSMainNum;
	int m_iOSSubNum;
	bool m_bIsServer;
	std::string m_strOSDesc;
};

