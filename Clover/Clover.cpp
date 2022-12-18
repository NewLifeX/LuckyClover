//#include <windows.h>
#include <stdio.h>

//#define CPPHTTPLIB_OPENSSL_SUPPORT
#include "httplib.h"

void main()
{
    //OSVERSIONINFO osvi;
    //BOOL bIsWindowsXPorLater;

    //ZeroMemory(&osvi, sizeof(OSVERSIONINFO));
    //osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    //GetVersionEx(&osvi);

    //printf("osver: %d.%d.%d\r\n", osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
    //printf("PlatformId: %d\r\n", osvi.dwPlatformId);

    // HTTP
    httplib::Client cli("http://star.newlifex.com:6600");

    if (auto res = cli.Get("/api")) {
        if (res->status == 200) {
            printf("%s", res->body);
        }
    }

    //// HTTP
    //httplib::Client cli("http://x.newlifex.com");

    //auto res = cli.Get("/dotnet/dotNetFx40_Full_x86_x64.exe");
    //res->status;
    //res->body;
}