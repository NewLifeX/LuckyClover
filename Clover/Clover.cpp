#include <stdio.h>

//#define CPPHTTPLIB_OPENSSL_SUPPORT
#include "httplib.h"

#include <windows.h>

void main()
{
    OSVERSIONINFO osvi;
    BOOL bIsWindowsXPorLater;

    ZeroMemory(&osvi, sizeof(OSVERSIONINFO));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&osvi);

    printf("osver: %d.%d.%d\r\n", osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
    printf("PlatformId: %d\r\n", osvi.dwPlatformId);

    //// HTTP
    //httplib::Client cli("http://star.newlifex.com:6600");

    //if (auto res = cli.Get("/api")) {
    //    if (res->status == 200) {
    //        printf("%s", res->body);
    //    }
    //}

    // HTTP
    httplib::Client cli("http://x.newlifex.com");

    auto res = cli.Get("/dotnet/dotNetFx40_Full_x86_x64.exe",
        [&](const char* data, size_t data_length) {
            //body.append(data, data_length);
            printf("%d", data_length);
            return true;
        });

    //// HTTP
    //httplib::Client cli("http://x.newlifex.com");

    //auto res = cli.Get("/dotnet/dotNetFx40_Full_x86_x64.exe");
    //res->status;
    //res->body;
}