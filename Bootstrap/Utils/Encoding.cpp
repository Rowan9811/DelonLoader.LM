#ifndef PORT_DISABLE
#include "Encoding.h"
#include <Windows.h>

char* Encoding::OsToUtf8(const char* osStr)
{
	// Convert to UTF16
	int len = MultiByteToWideChar(CP_ACP, 0, osStr, -1, NULL, 0);
	wchar_t* wstr = new wchar_t[len + 1];
	memset(wstr, 0, len + 1);
	MultiByteToWideChar(CP_ACP, 0, osStr, -1, wstr, len);

	// Convert UTF16 to UTF8
	len = WideCharToMultiByte(CP_UTF8, 0, wstr, -1, NULL, 0, NULL, NULL);
	char* str = new char[len + 1];
	memset(str, 0, len + 1);
	WideCharToMultiByte(CP_UTF8, 0, wstr, -1, str, len, NULL, NULL);

	delete[] wstr;
	return str;

}

char* Encoding::Utf8ToOs(const char* utf8Str)
{
	// Convert to UTF16
	int len = MultiByteToWideChar(CP_UTF8, 0, utf8Str, -1, NULL, 0);
	wchar_t* wstr = new wchar_t[len + 1];
	memset(wstr, 0, len * 2 + 2);
	MultiByteToWideChar(CP_UTF8, 0, utf8Str, -1, wstr, len);

	// Convert to system default encoding
	len = WideCharToMultiByte(CP_ACP, 0, wstr, -1, NULL, 0, NULL, NULL);
	char* str = new char[len + 1];
	memset(str, 0, len + 1);
	WideCharToMultiByte(CP_ACP, 0, wstr, -1, str, len, NULL, NULL);

	delete[] wstr;
	return str;
}
#elif __ANDROID__
#include "Encoding.h"

char* Encoding::OsToUtf8(char* osStr)
{
    return osStr;

}

char* Encoding::Utf8ToOs(char* utf8Str)
{
    return utf8Str;
}
#endif

std::string Encoding::string_format(const char* format, va_list args1)
{
    va_list args2;
    va_copy(args2, args1);

    int size_s = vsnprintf(nullptr, 0, format, args1) + 1;

    if (size_s <= 0) { throw std::runtime_error("Error during formatting."); }
    auto size = static_cast<size_t>(size_s);
    std::unique_ptr<char[]> buf(new char[size]);

    vsnprintf(buf.get(), size, format, args2);
    std::string str{ buf.get(), buf.get() + size - 1 };

    va_end(args2);

    return str;
}

std::string Encoding::DirectoryConcat(std::string base, const char* path)
{
    if (base[base.size() - 1] != '/') // Missing a path character between the two?
        base += "/";

    base += path; // Then append the actual path
    return base;
}