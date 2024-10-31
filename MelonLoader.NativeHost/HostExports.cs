#pragma warning disable CS0649 //Field is never assigned (native struct)
namespace MelonLoader.NativeHost
{
    unsafe struct HostExports
    {
        internal delegate* unmanaged<void**, void*, void> HookAttach;
        internal delegate* unmanaged<void**, void*, void> HookDetach;
        internal delegate* unmanaged<string, void> LogConsole;
        internal delegate* unmanaged<void**> GetJavaVM;
        internal delegate* unmanaged<string> GetPackageName;
    }
}
