#include "CDownloadFile.h"

CDownloadFile::CDownloadFile()
{

}

CDownloadFile::~CDownloadFile()
{

}



bool CDownloadFile::Download(CString strLocalFilePath, CString strNetIP, UINT nPort, CString strGetDataPath)
{
	bool bSuccess = false;
    std::string szError = "";

    USES_CONVERSION;
    std::string szNetIP = T2A(strNetIP.GetBuffer(0)); strNetIP.ReleaseBuffer();
    std::string szLocalFilePath = T2A(strLocalFilePath.GetBuffer(0)); strLocalFilePath.ReleaseBuffer();
    std::string szGetRemoteFilePath = T2A(strGetDataPath.GetBuffer(0)); strGetDataPath.ReleaseBuffer();
    if (szLocalFilePath.empty())
    {
        //szLocalFilePath = "dotNetFx40_Full_x86_x64.exe";
        return false;
    }

    httplib::Client cli1(szNetIP);
    if (auto res = cli1.Get(szGetRemoteFilePath)) {
        //szError = format("������ѷ��ؽ����ִ�д���:%d", res->status);
        //printf("%s", szError.c_str());
        std::cout << "HTTP: " << res->status << std::endl;
        if (200 == res->status)
        {
            std::ofstream out;
            out.open(szLocalFilePath, std::ios_base::binary | std::ios::out);
            //szError = format("׼�������ļ����洢����·��:%s", szLocalFilePath);
            //printf("%s", szError.c_str());
            //std::cout << "׼�������ļ����洢����·��:" << szLocalFilePath << std::endl;
            if (out.is_open())
            {
                out << res->body;
                out.flush();
                out.close();

                bSuccess = true;

                //szError = format("�ļ��������");
                //printf("%s",szError.c_str());
                std::cout << "�ļ��������" << std::endl;
            }
            else
            {
                //szError = format("�����ļ�[%s]��ʧ��.", szLocalFilePath);
                //printf("%s",szError.c_str());
                std::cout << "�����ļ�[" << szLocalFilePath << "]��ʧ��" << std::endl;
            }
        }
    }

    return bSuccess;
}