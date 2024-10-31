namespace MelonLoader.NativeHost
{
    internal class MelonLoaderInvoker
    {
        internal static unsafe void Initialize()
        {
            BootstrapInterop.HookAttach = NativeEntryPoint.Exports.HookAttach;
            BootstrapInterop.HookDetach = NativeEntryPoint.Exports.HookDetach;
            BootstrapInterop.LogConsole = NativeEntryPoint.Exports.LogConsole;
            BootstrapInterop.GetJavaVM = NativeEntryPoint.Exports.GetJavaVM;
            BootstrapInterop.GetPackageName = NativeEntryPoint.Exports.GetPackageName;

            Core.Initialize();
        }

        internal static void PreStart() => Core.PreStart();
        internal static void Start() => Core.Start();
    }
}
