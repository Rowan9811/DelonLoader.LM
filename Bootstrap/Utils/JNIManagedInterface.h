#pragma ONCE
#include <jni.h>

class JNIManagedInterface {
public:
    static jint JNI_GetDefaultJavaVMInitArgs(void*);
    static jint JNI_CreateJavaVM(JavaVM**, JNIEnv**, void*);
    static jint JNI_GetCreatedJavaVMs(JavaVM**, jsize, jsize*);
};