#include "JNIManagedInterface.h"
#include "../Core.h"
#include "jni.h"

jint JNIManagedInterface::JNI_CreateJavaVM(JavaVM **jvm, JNIEnv **env, void *args) {
    *jvm = Core::Bootstrap;
    *env = Core::GetEnv();
    return 0;
}

jint JNIManagedInterface::JNI_GetDefaultJavaVMInitArgs(void *) {
    return 0;
}

jint JNIManagedInterface::JNI_GetCreatedJavaVMs(JavaVM** jvm, jsize bufferLength, jsize* createdVMs) {
    *jvm = Core::Bootstrap;
    *createdVMs = 1;
    return 0;
}
