#include "AndroidData.h"

#include <jni.h>
#include <unistd.h>
#include <android/log.h>
#include <stdio.h>
#include <string>
#include <sys/stat.h>
#include <fcntl.h>
#include <vector>

#include "../Utils/Console/Debug.h"
#include "../Utils/Assertion.h"
#include "../Core.h"

char* AndroidData::BaseDataDir = "/storage/emulated/0/Android/data";
char* AndroidData::AppName = NULL;
char* AndroidData::DataDir = NULL;
jobject AndroidData::CurrentActivity = nullptr;

bool AndroidData::Initialize()
{
    GetAppName();
    if (!Assertion::ShouldContinue) return Assertion::ShouldContinue;
    GetDataDir();
    return Assertion::ShouldContinue;
}

void AndroidData::GetAppName()
{
    char* buffer = (char*)malloc(sizeof(char) * 0x1000);

    int fd = open("/proc/self/cmdline", O_RDONLY);
    if (read(fd, buffer, 0x1000) == 0)
    {
        Assertion::ThrowInternalFailure("Cannot get App name");
        return;
    }

    int nbytesread = strlen(buffer);

    AppName = new char[nbytesread + 1];
    AppName = (char*)malloc(nbytesread + 1);
    memcpy(AppName, buffer, nbytesread);
    AppName[nbytesread] = '\0';

    free(buffer);
}

void AndroidData::GetDataDir()
{
    JNIEnv* env = Core::GetEnv();

    jclass unityClass = env->FindClass("com/unity3d/player/UnityPlayer");
    if (unityClass == NULL) {
        Assertion::ThrowInternalFailure("Failed to find class com/unity3d/player/UnityPlayer");
        DataDir = nullptr;
    }

    jfieldID currentActivityId = env->GetStaticFieldID(unityClass, "currentActivity", "Landroid/app/Activity;");
    if (currentActivityId == NULL) {
        Assertion::ThrowInternalFailure("Failed to get field ID currentActivity");
        DataDir = nullptr;
    }

    jobject currentActivityObj = env->GetStaticObjectField(unityClass, currentActivityId);
    if (currentActivityObj == NULL) {
        Assertion::ThrowInternalFailure("Failed to get static object field currentActivity");
        DataDir = nullptr;
    }

    CurrentActivity = currentActivityObj;

    jclass activityClass = env->FindClass("android/app/Activity");
    if (activityClass == NULL) {
        Assertion::ThrowInternalFailure("Failed to find class android/app/Activity");
        DataDir = nullptr;
    }

    jmethodID getExtFilesId = env->GetMethodID(activityClass, "getExternalFilesDir", "(Ljava/lang/String;)Ljava/io/File;");
    if (getExtFilesId == NULL) {
        Assertion::ThrowInternalFailure("Failed to get method ID getExternalFilesDir");
        DataDir = nullptr;
    }

    jobject extFileObj = env->CallObjectMethod(currentActivityObj, getExtFilesId, nullptr);
    if (extFileObj == NULL) {
        Assertion::ThrowInternalFailure("Failed to invoke getExternalFilesDir()");
        DataDir = nullptr;
    }

    jclass fileClass = env->FindClass("java/io/File");
    if (fileClass == NULL) {
        Assertion::ThrowInternalFailure("Failed to find class java/io/File");
        DataDir = nullptr;
    }

    jmethodID toStringId = env->GetMethodID(fileClass, "toString", "()Ljava/lang/String;");
    if (toStringId == NULL) {
        Assertion::ThrowInternalFailure("Failed to get method ID toString");
        DataDir = nullptr;
    }

    jstring fileString = (jstring)env->CallObjectMethod(extFileObj, toStringId);
    if (fileString == NULL) {
        Assertion::ThrowInternalFailure("Failed to invoke toString()");
        DataDir = nullptr;
    }

    std::string str = jstring2string(env, fileString);

    env->DeleteLocalRef(extFileObj);
    env->DeleteLocalRef(fileString);

    std::vector<char> dataVector(str.c_str(), str.c_str() + str.size() + 1);
    DataDir = (char *)(&dataVector[0]);
}

std::string AndroidData::jstring2string(JNIEnv *env, jstring jStr) {
    if (!jStr)
        return "";

    const jclass stringClass = env->GetObjectClass(jStr);
    const jmethodID getBytes = env->GetMethodID(stringClass, "getBytes", "(Ljava/lang/String;)[B");
    const jbyteArray stringJbytes = (jbyteArray) env->CallObjectMethod(jStr, getBytes, env->NewStringUTF("UTF-8"));

    size_t length = (size_t) env->GetArrayLength(stringJbytes);
    jbyte* pBytes = env->GetByteArrayElements(stringJbytes, NULL);

    std::string ret = std::string((char *)pBytes, length);
    env->ReleaseByteArrayElements(stringJbytes, pBytes, JNI_ABORT);

    env->DeleteLocalRef(stringJbytes);
    env->DeleteLocalRef(stringClass);
    return ret;
}