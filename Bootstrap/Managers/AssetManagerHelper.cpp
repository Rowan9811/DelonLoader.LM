#include "AssetManagerHelper.h"
#include "../Utils/Assertion.h"
#include "../Core.h"

AAssetManager* AssetManagerHelper::Instance = nullptr;

bool AssetManagerHelper::Initialize()
{
    auto env = Core::GetEnv();

    jclass unityClass = env->FindClass("com/unity3d/player/UnityPlayer");
    jfieldID currentActivityId = env->GetStaticFieldID(unityClass, "currentActivity", "Landroid/app/Activity;");
    jobject currentActvityObj = env->GetStaticObjectField(unityClass, currentActivityId);
    jclass activityClass = env->FindClass("android/app/Activity");
    jmethodID getAssetId = env->GetMethodID(activityClass, "getAssets", "()Landroid/content/res/AssetManager;");
    jobject assetManagerObj = env->CallObjectMethod(currentActvityObj, getAssetId);
    
    Instance = AAssetManager_fromJava(env, assetManagerObj);
    if (Instance == NULL)
    {
        Assertion::ThrowInternalFailure("Failed to create AssetManager instance");
        return false;
    }

    return true;
}
