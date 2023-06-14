#include "AssetManagerHelper.h"
#include "../Utils/Assertion.h"
#include "../Core.h"

AAssetManager* AssetManagerHelper::Instance = nullptr;

bool AssetManagerHelper::Initialize()
{
    auto env = Core::GetEnv();

    jclass unityClass = env->FindClass("com/unity3d/player/UnityPlayer");
    if (unityClass == NULL) {
        Assertion::ThrowInternalFailure("Failed to find class com/unity3d/player/UnityPlayer");
        return false;
    }

    jfieldID currentActivityId = env->GetStaticFieldID(unityClass, "currentActivity", "Landroid/app/Activity;");
    if (currentActivityId == NULL) {
        Assertion::ThrowInternalFailure("Failed to get field ID currentActivity");
        return false;
    }

    jobject currentActivityObj = env->GetStaticObjectField(unityClass, currentActivityId);
    if (currentActivityObj == NULL) {
        Assertion::ThrowInternalFailure("Failed to get static object field currentActivity");
        return false;
    }

    jclass activityClass = env->FindClass("android/app/Activity");
    if (activityClass == NULL) {
        Assertion::ThrowInternalFailure("Failed to find class android/app/Activity");
        return false;
    }

    jmethodID getAssetId = env->GetMethodID(activityClass, "getAssets", "()Landroid/content/res/AssetManager;");
    if (getAssetId == NULL) {
        Assertion::ThrowInternalFailure("Failed to get method ID getAssets");
        return false;
    }

    jobject assetManagerObj = env->CallObjectMethod(currentActivityObj, getAssetId);
    if (assetManagerObj == NULL) {
        Assertion::ThrowInternalFailure("Failed to invoke getAssets()");
        return false;
    }
    
    Instance = AAssetManager_fromJava(env, assetManagerObj);
    if (Instance == NULL)
    {
        Assertion::ThrowInternalFailure("Failed to create AssetManager instance");
        return false;
    }

    return true;
}
