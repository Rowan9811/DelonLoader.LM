#pragma once

#include <jni.h>
#include <string>

class AndroidData
{
public:
	static char* BaseDataDir;
	static char* AppName;
	static char* DataDir;
    static jobject CurrentActivity;
	static bool Initialize();
private:
	static void GetBaseDataDir();
	static void GetAppName();
	static void GetDataDir();
    static std::string jstring2string(JNIEnv *env, jstring jStr);
};

