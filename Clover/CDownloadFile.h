#pragma once
#include "BaseInclude.h"

class CDownloadFile
{
public:
	CDownloadFile();
	~CDownloadFile();
public:
	/// <summary>
	/// 从指定的网络地址上下载指定文件
	/// </summary>
	/// <param name="strLocalFilePath">保存在本地的文件绝对路径</param>
	/// <param name="strHostName">指定的网络地址</param>
	/// <param name="strGetDataPath">指定的下载路径</param>
	/// <returns></returns>
	bool Download(CString strLocalFilePath, CString strNetIP, CString strGetDataPath);
};

