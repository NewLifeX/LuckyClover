#pragma once
#include "BaseInclude.h"

class CDownloadFile
{
public:
	CDownloadFile();
	~CDownloadFile();
public:
	/// <summary>
	/// ��ָ���������ַ������ָ���ļ�
	/// </summary>
	/// <param name="strLocalFilePath">�����ڱ��ص��ļ�����·��</param>
	/// <param name="strHostName">ָ���������ַ</param>
	/// <param name="strGetDataPath">ָ��������·��</param>
	/// <returns></returns>
	bool Download(CString strLocalFilePath, CString strNetIP, CString strGetDataPath);
};

