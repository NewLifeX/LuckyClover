#pragma once
#include "httplib.h"
#include <Windows.h>
#include <atlstr.h>
#include <iostream>
#include <stdio.h>
#include <stdlib.h>
#include <string>

#pragma comment(lib,"atls.lib")

template<class... T>
std::string format(const char* fmt, const T&...t)
{
    const auto len = snprintf(nullptr, 0, fmt, t...);
    std::string r;
    r.resize(static_cast<size_t>(len) + 1);
    snprintf(&r.front(), len + 1, fmt, t...);  // Bad boy
    r.resize(static_cast<size_t>(len));

    return r;
}