using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using AsmResolver.Patching;
using AssetRipper.Primitives;
using Cpp2IL.Core;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.Extensions;
using Cpp2IL.Core.Logging;
using JNISharp.NativeInterface;
using LibCpp2IL.Logging;
using LibCpp2IL.Wasm;
using MelonLoader.InternalUtils;
using MelonLoader.Utils;
using Semver;

namespace MelonLoader.Il2CppAssemblyGenerator.Packages
{
    internal class Cpp2IL : Models.ExecutablePackage
    {
        internal static SemVersion NetCoreMinVersion = SemVersion.Parse("2022.1.0-pre-release.17");
        private static SemVersion NewExecutionMinVersion = SemVersion.Parse("2022.0.999");
        private SemVersion VersionSem;
        private string BaseFolder;

        private static string ReleaseName =>
            MelonUtils.IsWindows ? "Windows" : MelonUtils.IsUnix ? "Linux" : "OSX";
		
        internal Cpp2IL()
        {
            Version = "2022.1.0-pre-release.16";

            Name = nameof(Cpp2IL);
            Destination = Path.Combine(Core.BasePath, Name);
            OutputFolder = Path.Combine(Destination, "cpp2il_out");
        }

        internal override bool ShouldSetup() 
            => string.IsNullOrEmpty(Config.Values.DumperVersion) 
            || !Config.Values.DumperVersion.Equals(Version);

        internal override void Cleanup() { }

        internal override void Save()
            => Save(ref Config.Values.DumperVersion);

        internal override bool Execute()
        {
            // TODO: this could technically be done via download, but its a much bigger pain to do it that way

            Logger.InfoLog += (l, s) => Core.Logger.Msg($"[{s}] {l.TrimEnd('\n')}");
            Logger.WarningLog += (l, s) => Core.Logger.Warning($"[{s}] {l.TrimEnd('\n')}");
            Logger.ErrorLog += (l, s) => Core.Logger.Error($"[{s}] {l.TrimEnd('\n')}");
            Logger.VerboseLog += (l, s) => Core.Logger.Msg($"[{s}] {l.TrimEnd('\n')}");

            byte[] mdData = APKAssetManager.GetAssetBytes("bin/Data/Managed/Metadata/global-metadata.dat");
            string mdPath = Path.Combine(Core.BasePath, "global-metadata.dat");
            File.WriteAllBytes(mdPath, mdData);

            Cpp2IlApi.Init();
            Cpp2IlApi.ConfigureLib(false);
            var result = new Cpp2IlRuntimeArgs()
            {
                PathToAssembly = Core.GameAssemblyPath,
                PathToMetadata = mdPath,
                UnityVersion = UnityVersion.Parse(UnityInformationHandler.EngineVersion.ToString()), // they use different versions of the same library but under different names, thanks ds5678
                Valid = true,
                OutputRootDirectory = OutputFolder,
                OutputFormat = OutputFormatRegistry.GetFormat("dummydll"),
                ProcessingLayersToRun = [ProcessingLayerRegistry.GetById("attributeinjector")],
            };

            return RunCpp2IL(result);
        }

        // mostly copied from https://github.com/SamboyCoding/Cpp2IL/blob/development/Cpp2IL/Program.cs
        private bool RunCpp2IL(Cpp2IlRuntimeArgs runtimeArgs)
        {
            var executionStart = DateTime.Now;

            runtimeArgs.OutputFormat?.OnOutputFormatSelected();

            WasmFile.RemappedDynCallFunctions = null;

            Cpp2IlApi.InitializeLibCpp2Il(runtimeArgs.PathToAssembly, runtimeArgs.PathToMetadata, runtimeArgs.UnityVersion);

            foreach (var (key, value) in runtimeArgs.ProcessingLayerConfigurationOptions)
                Cpp2IlApi.CurrentAppContext.PutExtraData(key, value);

            //Pre-process processing layers, allowing them to stop others from running
            Core.Logger.Msg("Pre-processing processing layers...");
            var layers = runtimeArgs.ProcessingLayersToRun.Clone();
            RunProcessingLayers(runtimeArgs, processingLayer => processingLayer.PreProcess(Cpp2IlApi.CurrentAppContext, layers));
            runtimeArgs.ProcessingLayersToRun = layers;

            //Run processing layers
            Core.Logger.Msg("Invoking processing layers...");
            RunProcessingLayers(runtimeArgs, processingLayer => processingLayer.Process(Cpp2IlApi.CurrentAppContext));

            var outputStart = DateTime.Now;

            if (runtimeArgs.OutputFormat != null)
            {
                Core.Logger.Msg($"Outputting as {runtimeArgs.OutputFormat.OutputFormatName} to {runtimeArgs.OutputRootDirectory}...");
                runtimeArgs.OutputFormat.DoOutput(Cpp2IlApi.CurrentAppContext, runtimeArgs.OutputRootDirectory);
                Core.Logger.Msg($"Finished outputting in {(DateTime.Now - outputStart).TotalMilliseconds}ms");
            }
            else
            {
                Core.Logger.Warning("No output format requested, so not outputting anything. The il2cpp game loaded properly though! (Hint: You probably want to specify an output format, try --output-as)");
            }

            Cpp2IlPluginManager.CallOnFinish();

            File.Delete(runtimeArgs.PathToMetadata); // because we extracted it from the apk's assets folder; only purpose was this

            Core.Logger.Msg($"Done. Total execution time: {(DateTime.Now - executionStart).TotalMilliseconds}ms");
            return true;
        }

        private static void RunProcessingLayers(Cpp2IlRuntimeArgs runtimeArgs, Action<Cpp2IlProcessingLayer> run)
        {
            foreach (var processingLayer in runtimeArgs.ProcessingLayersToRun)
            {
                var processorStart = DateTime.Now;

                Core.Logger.Msg($"    {processingLayer.Name}...");

#if !DEBUG
                try
                {
#endif
                run(processingLayer);
#if !DEBUG
                }
                catch (Exception e)
                {
                    Logger.ErrorNewline($"Processing layer {processingLayer.Id} threw an exception: {e}");
                    Environment.Exit(1);
                }
#endif

                Core.Logger.Msg($"    {processingLayer.Name} finished in {(DateTime.Now - processorStart).TotalMilliseconds}ms");
            }
        }
    }
}
