﻿using System;
using System.IO;
using System.Reflection;
using MelonLoader.MonoInternals.ResolveInternals;
using MelonLoader.Utils;

namespace MelonLoader.MonoInternals
{
    public static class MonoResolveManager
    {
        internal static bool Setup()
        {
            if (!AssemblyManager.Setup())
                return false;

            // Setup Search Directories
            string[] searchdirlist =
            {
                MelonEnvironment.UserLibsDirectory,
                MelonEnvironment.PluginsDirectory,
                MelonEnvironment.ModsDirectory,
                MelonEnvironment.MelonBaseDirectory,
                MelonEnvironment.GameRootDirectory,
                MelonEnvironment.OurRuntimeDirectory,
            };
            foreach (string path in searchdirlist)
                AddSearchDirectory(path);
            
            ForceResolveRuntime("Mono.Cecil.dll");
            ForceResolveRuntime("MonoMod.exe");
            ForceResolveRuntime("MonoMod.Utils.dll");
            ForceResolveRuntime("MonoMod.RuntimeDetour.dll");

            // Setup Redirections
            string[] assembly_list =
            {
                "MelonLoader",
                "MelonLoader.ModHandler",
            };
            Assembly base_assembly = typeof(MonoResolveManager).Assembly;
            foreach (string assemblyName in assembly_list)
                GetAssemblyResolveInfo(assemblyName).Override = base_assembly;

            MelonDebug.Msg("[MonoResolveManager] Setup Successful!");

            return true;
        }

        private static void ForceResolveRuntime(string fileName)
        {
            string filePath = Path.Combine(MelonEnvironment.OurRuntimeDirectory, fileName);
            if (!File.Exists(filePath))
                return;

            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(filePath); }
            catch { assembly = null; }
            
            if (assembly == null)
                return;

            GetAssemblyResolveInfo(Path.GetFileNameWithoutExtension(fileName)).Override = assembly;
        }

        // Search Directories
        public static void AddSearchDirectory(string path, int priority = 0)
            => SearchDirectoryManager.Add(path, priority);
        public static void RemoveSearchDirectory(string path)
            => SearchDirectoryManager.Remove(path);

        // Assembly
        public delegate void OnAssemblyLoadHandler(Assembly assembly);
        public static event OnAssemblyLoadHandler OnAssemblyLoad;
        internal static void SafeInvoke_OnAssemblyLoad(Assembly assembly)
            => OnAssemblyLoad?.Invoke(assembly);

        public delegate Assembly OnAssemblyResolveHandler(string name, Version version);
        public static event OnAssemblyResolveHandler OnAssemblyResolve;
        internal static Assembly SafeInvoke_OnAssemblyResolve(string name, Version version)
            => OnAssemblyResolve?.Invoke(name, version);

        public static AssemblyResolveInfo GetAssemblyResolveInfo(string name)
            => AssemblyManager.GetInfo(name);
        public static void LoadInfoFromAssembly(Assembly assembly)
            => AssemblyManager.LoadInfo(assembly);
    }
}
