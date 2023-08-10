#ifdef _WIN32
#include <Windows.h>
#endif

#ifdef __ANDROID__
#include "AndroidData.h"
#endif

#include "Hook.h"
#include "../Utils/Console/Logger.h"
#include "../Utils/Console/Debug.h"


#ifdef _WIN64
#include "../Base/MSDetours/detours_x64.h"
#elif _WIN32
#include "../Base/MSDetours/detours_x86.h"
#endif

#ifdef _WIN32
void Hook::Attach(void** target, void* detour)
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(target, detour);
	DetourTransactionCommit();
}

void Hook::Detach(void** target, void* detour)
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourDetach(target, detour);
	DetourTransactionCommit();
}
#endif

#ifdef __ANDROID__
std::unordered_map<void*, Hook::FunchookDef*> Hook::FuncHookMap;
std::unordered_map<void*, Hook::HookDef*> Hook::DobbyHookMap;

void Hook::Attach(void** target, void* detour)
{
    if (DobbyHookMap.find(detour) == DobbyHookMap.end()) {
        Hook::HookDef* handle = nullptr;
        DobbyHookMap[detour] = handle = (Hook::HookDef*)malloc(sizeof(Hook::HookDef));
        handle->backup = *target;

        void* org = nullptr;
        int dobby = DobbyHook(*target, (dobby_dummy_func_t)detour, (dobby_dummy_func_t*)&org);
        if (dobby != 0)
        {
            std::string dobbyOutput = "Dobby hook failed: code " + std::to_string(dobby);
            Logger::QuickLog(dobbyOutput.c_str(), LogType::Error);
            return;
        }

        if (org == nullptr)
        {
            Logger::QuickLog("Dobby hook failed: null origin", LogType::Error);
            return;
        }

        *target = org;
#ifdef DEBUG
        Logger::QuickLog("Dobby hook successful", LogType::Debug);
#endif
    }
    else
        Logger::QuickLog("trying to hook already hooked detour i think?", LogType::Warning);
}

void Hook::Detach(void** target, void* detour)
{
    if (DobbyHookMap.find(detour) == DobbyHookMap.end()) {
        Logger::QuickLog("Hook does not exist, can't unhook", LogType::Error);
    }
    else {
        void* stub = DobbyHookMap[detour]->backup;
        int dobby = DobbyDestroy(stub);

        if (dobby != 0)
        {
            std::string dobbyOutput = "Dobby unhook failed: code " + std::to_string(dobby);
            Logger::QuickLog(dobbyOutput.c_str(), LogType::Error);
            return;
        }

        stub = nullptr;
        free(DobbyHookMap[detour]);

#ifdef DEBUG
        Logger::QuickLog("Dobby unhook successful", LogType::Debug);
#endif
    }
}

void Hook::AttachFH(void** target, void* detour)
{
    //Debug::Msg("attaching");

    int rv;
    void* trueTarget = detour;
    Hook::FunchookDef* handle = nullptr;

    if (FuncHookMap.find(trueTarget) == FuncHookMap.end())
    {
        FuncHookMap[trueTarget] = handle = (Hook::FunchookDef*)malloc(sizeof(Hook::FunchookDef));
        handle->original = *target;
        handle->handle = funchook_create();
        rv = funchook_prepare(handle->handle, target, detour);
        if (rv != 0)
        {
            Logger::QuickLog("Failed to prepare hook", LogType::Error);
            return;
        }
    } else
        handle = FuncHookMap[trueTarget];

    rv = funchook_install(handle->handle, 0);
    if (rv != 0)
    {
        Logger::QuickLogf("Failed to install hook (%d, %s)", LogType::Error, rv, funchook_error_message(handle->handle));
        return;
    }

    return;
}

void Hook::DetachFH(void** target, void* detour)
{
    //Debug::Msg("detaching");

    int rv;

    void* trueTarget = detour;
    Hook::FunchookDef* handle = nullptr;

    if (FuncHookMap.find(trueTarget) == FuncHookMap.end())
    {
        Logger::QuickLog("Hook doesn't exist", LogType::Error);
        return;
    } else
        handle = FuncHookMap[trueTarget];

    void* reset = handle->original;

    rv = funchook_uninstall(handle->handle, 0);
    if (rv != 0)
    {
        Logger::QuickLog("Failed to uninstall hook", LogType::Error);
        return;
    }

    *target = reset;

    return;
}
#endif

#ifdef __ANDROID__
bool Hook::Setup() {
    funchook_set_debug_file((std::string(AndroidData::DataDir) + "/funchook.log").c_str());
    dobby_enable_near_branch_trampoline();
    return true;
}
#endif