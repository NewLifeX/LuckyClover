#include <windows.h>
#include <stdio.h>

void main()
{
    OSVERSIONINFO osvi;
    BOOL bIsWindowsXPorLater;

    ZeroMemory(&osvi, sizeof(OSVERSIONINFO));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);

    GetVersionEx(&osvi);

    printf("osver: %d.%d.%d\r\n", osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
    printf("PlatformId: %d\r\n", osvi.dwPlatformId);

    //bIsWindowsXPorLater =
    //    ((osvi.dwMajorVersion > 5) ||
    //        ((osvi.dwMajorVersion == 5) && (osvi.dwMinorVersion >= 1)));

    //if (bIsWindowsXPorLater)
    //    printf("The system meets the requirements.\n");
    //else 
    //    printf("The system does not meet the requirements.\n");
}