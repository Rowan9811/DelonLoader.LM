#pragma once

#include "iosfwd"

class AssemblyLoader {
public:
    static std::string AssemblyTempPath;

    static bool Initialize();
    static void* Open(char *libraryPath);
};
