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
	/// �����ṩ�ӿ�,����������в���ϵͳ�İ汾��Ϣ
	/// </summary>
	/// <param name="iOSVerNum">����ϵͳ��汾��
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
	/// <param name="iSubVer">����ϵͳС�汾�ţ�һ������Ϊ�磺SP1</param>
	/// <returns>��������ϵͳ�İ汾��ƣ��� "windows XP SP1" </returns>
	std::string GetOSVersionDesc(int& iOSVerNum, int& iSubVer,bool& bIsServer);
private:
	/// <summary>
	/// �ڲ����ýӿڣ���Ȿ�ز���ϵͳ�汾��Ϣ
	/// </summary>
	/// <returns>bool,ֵΪ�����ʾ���ɹ�</returns>
	bool CheckOSVer();
//Attributes
private:
	int m_iOSMainNum;
	int m_iOSSubNum;
	bool m_bIsServer;
	std::string m_strOSDesc;
};

