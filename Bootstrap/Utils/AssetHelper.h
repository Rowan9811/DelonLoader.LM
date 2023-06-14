#pragma once


#include <string>
#include <jni.h>
#include <android/asset_manager_jni.h>

class AssetHelper {
public:
    static bool CopyMelon();
    static void CopyFileOrDir(const std::string& path, const std::string& base);
    static void CopyFile(const std::string& filename, const std::string& base);
    static bool CreateDirectory(const std::string& path);
};