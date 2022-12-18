#include <stdio.h>

//#define CPPHTTPLIB_OPENSSL_SUPPORT
#include "httplib.h"

#include <windows.h>
#pragma comment (lib,"Advapi32.lib")

using namespace std;

void main()
{
    OSVERSIONINFO osvi;
    BOOL bIsWindowsXPorLater;

    ZeroMemory(&osvi, sizeof(OSVERSIONINFO));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&osvi);

    printf("osver: %d.%d.%d\r\n", osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
    printf("PlatformId: %d\r\n", osvi.dwPlatformId);

    string releaseId = "";
    HKEY hKey_return = NULL;
    char keyValue[256];
    DWORD keySzType;
    DWORD keySize;

    if (ERROR_SUCCESS != RegOpenKeyEx(HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\", 0, KEY_READ, &hKey_return)) {
        printf("RegOpenKeyEx failed.\n");;
    }
    else if (ERROR_SUCCESS != RegQueryValueEx(hKey_return, "Version", 0, &keySzType, (LPBYTE)&keyValue, &keySize)) {
        printf("RegQueryValueEx failed.\n");
    }
    else {
        printf("ReleaseId: %s\n", keyValue);
    }

    //// HTTP
    //httplib::Client cli("http://star.newlifex.com:6600");

    //if (auto res = cli.Get("/api")) {
    //    if (res->status == 200) {
    //        printf("%s", res->body);
    //    }
    //}

    // HTTP
    httplib::Client cli("http://x.newlifex.com");

    if (auto res = cli.Get("/dotnet/dotNetFx40_Full_x86_x64.exe")) {
        printf("status:%d\n", res->status);
        if (res->status == 200)
        {
            std::ofstream out;
            out.open("dotNetFx40_Full_x86_x64.exe", std::ios_base::binary | std::ios::out);
            printf("savefile!\n");
            if (out.is_open())
            {
                out << res->body;
                out.flush();
                out.close();
                printf("down load file finished!\n");
            }
            else
            {
                printf("open file error!\n");
            }
        }
    }

    //// HTTP
    //httplib::Client cli("http://x.newlifex.com");

    //auto res = cli.Get("/dotnet/dotNetFx40_Full_x86_x64.exe");
    //res->status;
    //res->body;
}