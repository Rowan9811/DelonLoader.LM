using System.IO;

#if !NET6_0_OR_GREATER
using System.Diagnostics;
using MelonLoader;
#endif

namespace MelonLoader.Utils
{
    public static class MelonEnvironment
    {
        private const string OurRuntimeName =
#if !NET6_0_OR_GREATER
            "net35";
#else
            "net8";
#endif

        public static bool IsDotnetRuntime { get; } = OurRuntimeName == "net8";
        public static bool IsMonoRuntime { get; } = !IsDotnetRuntime;

        public static string MelonLoaderDirectory { get; internal set; }
        public static string GameRootDirectory { get; internal set; }
        public static string PackageName { get; internal set; }

#if NET6_0_OR_GREATER
        public static string GameExecutablePath => System.Environment.ProcessPath;
#else
        public static string GameExecutablePath => Process.GetCurrentProcess().MainModule!.FileName;
#endif
        public static string MelonBaseDirectory => Directory.GetParent(MelonLoaderDirectory)!.FullName;
        public static string DependenciesDirectory => Path.Combine(MelonLoaderDirectory, "Dependencies");
        public static string SupportModuleDirectory => Path.Combine(DependenciesDirectory, "SupportModules");
        public static string CompatibilityLayerDirectory => Path.Combine(DependenciesDirectory, "CompatibilityLayers");
        public static string Il2CppAssemblyGeneratorDirectory => Path.Combine(DependenciesDirectory, "Il2CppAssemblyGenerator");
        public static string ModsDirectory => Path.Combine(MelonBaseDirectory, "Mods");
        public static string PluginsDirectory => Path.Combine(MelonBaseDirectory, "Plugins");
        public static string UserLibsDirectory => Path.Combine(MelonBaseDirectory, "UserLibs");
        public static string UserDataDirectory => Path.Combine(MelonBaseDirectory, "UserData");
        public static string MelonLoaderLogsDirectory => Path.Combine(MelonLoaderDirectory, "Logs");
        public static string OurRuntimeDirectory => Path.Combine(MelonLoaderDirectory, OurRuntimeName);

        public static string GameExecutableName => Path.GetFileNameWithoutExtension(GameExecutablePath);
        public static string UnityGameDataDirectory => "bin/Data/"; // inside the APK's assets directory; we're interacting with it via the AssetManager API so this makes sense
        public static string Il2CppDataDirectory => Path.Combine(UnityGameDataDirectory, "il2cpp_data");
        public static string UnityPlayerPath => Path.Combine(GameRootDirectory, "UnityPlayer.dll");

        public static string MelonManagedDirectory => Path.Combine(DependenciesDirectory, "Mono");
        public static string Il2CppAssembliesDirectory => Path.Combine(MelonLoaderDirectory, "Il2CppAssemblies");

        internal static void PrintEnvironment()
        {
            //These must not be changed, lum needs them
            MelonLogger.MsgDirect($"Core::BasePath = {MelonBaseDirectory}");
            MelonLogger.MsgDirect($"Game::BasePath = {GameRootDirectory}");
            MelonLogger.MsgDirect($"Game::DataPath = {UnityGameDataDirectory}");
            MelonLogger.MsgDirect($"Game::ApplicationPath = {GameExecutablePath}");

            MelonLogger.MsgDirect($"Runtime Type: {OurRuntimeName}");
        }
    }
}