#include "AssetHelper.h"

#include <jni.h>
#include <string>
#include <fstream>

#include "../Managers/AssetManagerHelper.h"
#include "../Core.h"
#include "../Managers/AndroidData.h"

bool AssetHelper::CopyMelon() {
    try {
        std::string base = std::string(AndroidData::DataDir);
        // The + "/" is required for this to function. It breaks everything otherwise. Do not ask why, I do not know.
        CopyFileOrDir("melonloader", base + "/");
        CopyFileOrDir("bin/Data/Managed/etc", base + "/il2cpp/", "etc");
        return true;
    }
    catch (...) {
        return false;
    }
};

void AssetHelper::CopyFileOrDir(const std::string& path, const std::string& base) {
    JNIEnv *env = Core::GetEnv();

    jobject context = AndroidData::CurrentActivity;

    jclass contextClass = env->GetObjectClass(context);
    jmethodID getAssetsMethod = env->GetMethodID(contextClass, "getAssets", "()Landroid/content/res/AssetManager;");
    jobject assetManager = env->CallObjectMethod(context, getAssetsMethod);

    // Convert path and base to jstring
    jstring pathString = env->NewStringUTF(path.c_str());
    jstring baseString = env->NewStringUTF(base.c_str());

    // Access the list method of AssetManager to get assets array
    jclass assetManagerClass = env->GetObjectClass(assetManager);
    jmethodID listMethod = env->GetMethodID(assetManagerClass, "list", "(Ljava/lang/String;)[Ljava/lang/String;");
    jobjectArray assetsArray = (jobjectArray)env->CallObjectMethod(assetManager, listMethod, pathString);

    // Convert assets array length to C++ size_t
    jsize assetsLength = env->GetArrayLength(assetsArray);
    size_t assetsSize = static_cast<size_t>(assetsLength);

    if (assetsSize == 0) {
        CopyFile(path, base);
    } else {
        std::string fullPath = base + "/" + path;

        // Create the directory if it doesn't exist
        CreateDirectory(fullPath);

        // Iterate over assets and recursively call copyFileOrDir
        for (size_t i = 0; i < assetsSize; ++i) {
            jstring asset = (jstring)env->GetObjectArrayElement(assetsArray, i);
            const char* assetStr = env->GetStringUTFChars(asset, nullptr);
            std::string assetPath = path + "/" + assetStr;
            env->ReleaseStringUTFChars(asset, assetStr);

            CopyFileOrDir(assetPath, base);
        }
    }

    // Release local references
    env->DeleteLocalRef(assetManagerClass);
    env->DeleteLocalRef(contextClass);
    env->DeleteLocalRef(pathString);
    env->DeleteLocalRef(baseString);
}

void AssetHelper::CopyFileOrDir(const std::string& path, const std::string& base, const std::string& pathStart) {
    JNIEnv *env = Core::GetEnv();

    jobject context = AndroidData::CurrentActivity;

    jclass contextClass = env->GetObjectClass(context);
    jmethodID getAssetsMethod = env->GetMethodID(contextClass, "getAssets", "()Landroid/content/res/AssetManager;");
    jobject assetManager = env->CallObjectMethod(context, getAssetsMethod);

    // Convert path and base to jstring
    jstring pathString = env->NewStringUTF(path.c_str());
    jstring baseString = env->NewStringUTF(base.c_str());

    // Access the list method of AssetManager to get assets array
    jclass assetManagerClass = env->GetObjectClass(assetManager);
    jmethodID listMethod = env->GetMethodID(assetManagerClass, "list", "(Ljava/lang/String;)[Ljava/lang/String;");
    jobjectArray assetsArray = (jobjectArray)env->CallObjectMethod(assetManager, listMethod, pathString);

    // Convert assets array length to C++ size_t
    jsize assetsLength = env->GetArrayLength(assetsArray);
    size_t assetsSize = static_cast<size_t>(assetsLength);

    if (assetsSize == 0) {
        CopyFile(path, base);
    } else {
        std::string fullPath = base + "/" + pathStart;

        // Create the directory if it doesn't exist
        CreateDirectory(fullPath);

        // Iterate over assets and recursively call copyFileOrDir
        for (size_t i = 0; i < assetsSize; ++i) {
            jstring asset = (jstring)env->GetObjectArrayElement(assetsArray, i);
            const char* assetStr = env->GetStringUTFChars(asset, nullptr);
            std::string assetPath = path + "/" + assetStr;
            env->ReleaseStringUTFChars(asset, assetStr);

            CopyFileOrDir(assetPath, base, assetPath.substr(assetPath.find(pathStart) + pathStart.length()));
        }
    }

    // Release local references
    env->DeleteLocalRef(assetManagerClass);
    env->DeleteLocalRef(contextClass);
    env->DeleteLocalRef(pathString);
    env->DeleteLocalRef(baseString);
}

void AssetHelper::CopyFile(const std::string& filename, const std::string& base) {
    AAsset* asset = AAssetManager_open(AssetManagerHelper::Instance, filename.c_str(), AASSET_MODE_UNKNOWN);
    std::ofstream outputStream = std::ofstream(base + "/" + filename);

    const int bufferSize = 1024;
    void* buffer = malloc(bufferSize);

    int bytesRead;
    int totalBytesRead = 0;
    while ((bytesRead = AAsset_read(asset, buffer, 1024)) > 0) {
        outputStream.write(static_cast<char*>(buffer), bytesRead);
        totalBytesRead += bytesRead;
    }

    outputStream.close();
    free(buffer);
}

bool AssetHelper::CreateDirectory(const std::string& path) {
    if (!std::filesystem::exists(path)) {
        if (!std::filesystem::create_directory(path))
            return false;
    }

    return true;
}