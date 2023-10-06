#pragma once
#include <string>

class Encoding
{
public:
    /// <summary>
    /// Convert system default encoding string to utf8
    /// </summary>
    /// <param name="osStr"></param>
    /// <returns></returns>
    static char* OsToUtf8(char* osStr);

    /// <summary>
    /// Convert utf8 to system default encoding string 
    /// </summary>
    /// <param name="utf8Str"></param>
    /// <returns></returns>
    static char* Utf8ToOs(char* utf8Str);

    /// <summary>
    /// Handles string formatting
    /// This isn't really encoding but close enough so it's here
    /// </summary>
    static std::string string_format(const char* format, va_list args1);

    /// <summary>
    /// Concatenates paths while handling /
    /// </summary>
    static std::string DirectoryConcat(std::string base, const char* path);
};

