﻿using System.Text;
using System.Text.Json;
using Cpp2IL.Core.Api;
using Cpp2IL.Core.Logging;
using Cpp2IL.Core.Model.Contexts;
using Cpp2IL.Plugin.BuildReport.Model;
using LibCpp2IL;
using LibCpp2IL.Metadata;

namespace Cpp2IL.Plugin.BuildReport;

public class BuildReportOutputFormat : Cpp2IlOutputFormat
{
    public override string OutputFormatId => "buildreport";
    public override string OutputFormatName => "IL2CPP Build Report";

    public override void OnOutputFormatSelected()
    {
        //We want readable information for metadata sizes in the build report
        ClassReadingBinaryReader.EnableReadableSizeInformation = true;
    }

    public override void DoOutput(ApplicationAnalysisContext context, string outputRoot)
    {
        //This output format serves as a way to see what's taking up space (and thus, presumably, build time) in your game
        //This is one of the few output formats that is targeted not at reverse-engineers, but rather at the game developer
        //It's probably useless to any players or modders.

        //Things that can be logged
        // - Binary
        //      - size of il2cpp section, resources, etc
        //      - generic variants, most varied types, etc
        //      - approximate amount of code contained within each managed assembly/type, etc.
        // - Metadata
        //      - number of types, methods, etc
        //      - sizes of various metadata streams (string literals etc)


        //Data analysis

        var binarySize = context.Binary.RawLength;
        var metadataInBinarySize = context.Binary.InBinaryMetadataSize;

        //Number of variants per generic base type
        var genericMethodDataByBaseMd = new Dictionary<Il2CppMethodDefinition, GenericMethodData>();

        foreach (var mSpec in context.Binary.AllGenericMethodSpecs)
        {
            var md = mSpec.MethodDefinition!;

            if (!genericMethodDataByBaseMd.TryGetValue(md, out var gmd))
            {
                gmd = genericMethodDataByBaseMd[md] = new(context, md); //This populates generic method instruction size and variant pointers
            }

            gmd.NumVariants++;
        }

        var numberOfGenericVariants = genericMethodDataByBaseMd.Values.Sum(gmd => gmd.NumVariants);
        var numberOfGenericPointers = genericMethodDataByBaseMd.Values.Sum(gmd => gmd.CountedVariantAddresses.Count);
        var sizeOfAllGenerics = genericMethodDataByBaseMd.Sum(m => (long)m.Value.TotalSizeInstructions);

        var mostUsedGenericMethods = genericMethodDataByBaseMd.OrderByDescending(kvp => kvp.Value.NumVariants).Take(100).ToList();
        var mostSpaceConsumingGenericMethods = genericMethodDataByBaseMd.OrderByDescending(kvp => kvp.Value.TotalSizeInstructions).Take(100).ToList();

        var methodsBySize = context.AllTypes.SelectMany(t => t.Methods).Where(m => m is not InjectedMethodAnalysisContext).Where(m => m.UnderlyingPointer != 0).ToDictionary(m => m, m => m.RawBytes.Length);
        var numberOfNonGenerics = methodsBySize.Count;
        var sizeOfAllNonGenerics = methodsBySize.Sum(m => m.Value);
        var largestMethods = methodsBySize.OrderByDescending(kvp => kvp.Value).Take(100).ToList();

        var inBinaryMetadataObjectsBySize = (List<KeyValuePair<Type?, int>>)context.Binary.BytesReadPerClass.OrderByDescending(kvp => kvp.Value).ToList()!;
        var inBinaryMetadataUnspecifiedSize = context.Binary.InBinaryMetadataSize - inBinaryMetadataObjectsBySize.Sum(kvp => kvp.Value);
        inBinaryMetadataObjectsBySize.Add(new(null, inBinaryMetadataUnspecifiedSize));

        var metadataFileObjectsBySize = (List<KeyValuePair<Type?, int>>)context.Metadata.BytesReadPerClass.OrderByDescending(kvp => kvp.Value).ToList()!;
        var metadataFileUnspecifiedSize = context.Metadata.Length - metadataFileObjectsBySize.Sum(kvp => kvp.Value);
        metadataFileObjectsBySize.Add(new(null, (int)metadataFileUnspecifiedSize));

        var attributeGeneratorsBySize = context.Assemblies
            .Cast<HasCustomAttributes>()
            .Concat(context.AllTypes)
            .Concat(context.AllTypes.SelectMany(t => t.Methods))
            .Concat(context.AllTypes.SelectMany(t => t.Fields))
            .Concat(context.AllTypes.SelectMany(t => t.Events))
            .Concat(context.AllTypes.SelectMany(t => t.Properties))
            .Where(m => m.CaCacheGeneratorAnalysis != null)
            .Select(m => m.CaCacheGeneratorAnalysis!)
            .Where(a => a.UnderlyingPointer != 0)
            .ToDictionary(a => a, a => a.RawBytes.Length);
        var sizeOfAllCaGenerators = attributeGeneratorsBySize.Sum(a => a.Value);
        var largestCaGenerators = attributeGeneratorsBySize.OrderByDescending(kvp => kvp.Value).Take(100).ToList();

        var spaceUsedPerAssembly = new Dictionary<AssemblyAnalysisContext, int>();
        foreach (var assemblyAnalysisContext in context.Assemblies)
        {
            spaceUsedPerAssembly[assemblyAnalysisContext] = 0;

            //Methods
            spaceUsedPerAssembly[assemblyAnalysisContext] += assemblyAnalysisContext.Types.SelectMany(t => t.Methods).Sum(m => methodsBySize.GetOrDefault(m, 0));

            //Generics
            var asmDef = assemblyAnalysisContext.Definition.Image;
            foreach (var genericMethodData in genericMethodDataByBaseMd)
            {
                if (genericMethodData.Key.DeclaringType?.DeclaringAssembly != asmDef)
                    continue;

                spaceUsedPerAssembly[assemblyAnalysisContext] += genericMethodData.Value.TotalSizeInstructions;
            }
        }

        var sortedUsageByAssembly = spaceUsedPerAssembly.OrderByDescending(kvp => kvp.Value).ToList();

        var unclassifiedSize = binarySize - sizeOfAllGenerics - sizeOfAllNonGenerics - sizeOfAllCaGenerators - metadataInBinarySize;

        //Generate build report

        var ret = new StringBuilder();

        ret.AppendLine($"IL2CPP Binary report generated at {DateTime.Now:f}:");
        ret.AppendLine();
        ret.AppendLine($"Binary size: {binarySize} bytes ({binarySize / 1024f / 1024:f2}MB)");
        ret.AppendLine($"    Of which {numberOfGenericPointers} generic method bodies: {sizeOfAllGenerics} bytes ({sizeOfAllGenerics / 1024f / 1024:f2}MB, {(double)sizeOfAllGenerics / binarySize:p})");
        ret.AppendLine($"    Of which {numberOfNonGenerics} non-generic method bodies: {sizeOfAllNonGenerics} bytes ({sizeOfAllNonGenerics / 1024f / 1024:f2}MB, {(double)sizeOfAllNonGenerics / binarySize:p})");
        ret.AppendLine($"    Of which Custom Attribute generator bodies: {sizeOfAllCaGenerators} bytes ({sizeOfAllCaGenerators / 1024f / 1024:f2}MB, {(double)sizeOfAllCaGenerators / binarySize:p})");
        ret.AppendLine($"    Of which in-binary il2cpp metadata: {metadataInBinarySize} bytes ({metadataInBinarySize / 1024f / 1024:f2}MB, {(double)metadataInBinarySize / binarySize:p})");
        ret.AppendLine($"    Of which C++ functions (il2cpp api functions, etc) and misc data: {unclassifiedSize} bytes ({(unclassifiedSize) / 1024f / 1024:f2}MB, {(double)unclassifiedSize / binarySize:p})");
        ret.AppendLine();
        ret.AppendLine($"Total number of generic variants: {numberOfGenericVariants} ({numberOfGenericPointers} after generic merging)");
        ret.AppendLine("    Top 100 most varied generic methods:");
        ret.AppendLine();

        foreach (var gMethod in mostUsedGenericMethods)
            ret.AppendLine($"        {GetMethodName(gMethod.Key)}, with {gMethod.Value.NumVariants} variants, reduced to {gMethod.Value.CountedVariantAddresses.Count} variants by generic sharing, totalling {gMethod.Value.TotalSizeInstructions} bytes of instructions");

        ret.AppendLine();
        ret.AppendLine("    Top 100 most space-consuming generic methods:");

        foreach (var gMethod in mostSpaceConsumingGenericMethods)
            ret.AppendLine($"        {GetMethodName(gMethod.Key)}, with {gMethod.Value.NumVariants} variants, reduced to {gMethod.Value.CountedVariantAddresses.Count} variants by generic sharing, totalling {gMethod.Value.TotalSizeInstructions} bytes ({gMethod.Value.TotalSizeInstructions / 1024f / 1024:f2}MB) of instructions");

        ret.AppendLine();
        ret.AppendLine($"Total number of non-generic methods: {methodsBySize.Count}");
        ret.AppendLine("    Top 100 largest non-generic methods:");

        foreach (var method in largestMethods)
            ret.AppendLine($"        {GetMethodName(method.Key.Definition!)}, with {method.Value} bytes ({method.Value / 1024f / 1024:f2}MB)");

        ret.AppendLine();
        ret.AppendLine($"Total number of Custom Attribute generators: {attributeGeneratorsBySize.Count}");
        ret.AppendLine("    Top 100 largest CA generators:");

        foreach (var caGenerator in largestCaGenerators)
            ret.AppendLine($"        Generator for {caGenerator.Key.AssociatedMember}, with {caGenerator.Value} bytes ({caGenerator.Value / 1024f / 1024:f2}MB)");

        ret.AppendLine();
        ret.AppendLine("    Usage per assembly, sorted by space used (by method bodies only, no metadata):");

        foreach (var kvp in sortedUsageByAssembly)
            ret.AppendLine($"        {kvp.Key.Definition.AssemblyName.Name}: {kvp.Value / 1024f / 1024:f3}MB ({(double)kvp.Value / binarySize:P} of binary)");

        ret.AppendLine();
        ret.AppendLine("    In-Binary Metadata breakdown:");

        foreach (var kvp in inBinaryMetadataObjectsBySize)
            if (kvp.Value >= 0)
                ret.AppendLine($"        {kvp.Key?.Name ?? "Unspecified"}: {kvp.Value} bytes ({kvp.Value / 1024f / 1024:f2}MB)");

        ret.AppendLine();
        ret.AppendLine($"    Metadata file (global-metadata.dat) breakdown (total size {context.Metadata.Length} bytes, {context.Metadata.Length / 1024f / 1024:f2}MB):");

        foreach (var kvp in metadataFileObjectsBySize)
            if (kvp.Value >= 0)
                ret.AppendLine($"        {kvp.Key?.Name ?? "Unspecified"}: {kvp.Value} bytes ({kvp.Value / 1024f / 1024:f2}MB)");

        //Save output
        var outputPath = Path.Combine(outputRoot, "buildreport.txt");
        File.WriteAllText(outputPath, ret.ToString());

        Logger.InfoNewline($"Wrote human-readable build report to {outputPath}", "BuildReportOutputFormat");

        //Save JSON output
        var jsonOutputPath = Path.Combine(outputRoot, "buildreport.json");

        var outputData = new OutputData();

        outputData.Binary.RawReadableClasses = inBinaryMetadataObjectsBySize.Select(kvp => new OutputReadableClassData { BytesUsed = kvp.Value, TypeName = kvp.Key?.Name ?? "Unspecified", }).ToArray();

        outputData.GlobalMetadata.RawReadableClasses = metadataFileObjectsBySize.Select(kvp => new OutputReadableClassData { BytesUsed = kvp.Value, TypeName = kvp.Key?.Name ?? "Unspecified", }).ToArray();

        var jsonText = JsonSerializer.Serialize(outputData, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText(jsonOutputPath, jsonText);

        Logger.InfoNewline($"Wrote JSON build report to {jsonOutputPath}", "BuildReportOutputFormat");
    }

    private static string GetMethodName(Il2CppMethodDefinition md)
    {
        return $"{md.DeclaringType!.FullName}::{md.Name}({string.Join(", ", md.Parameters!.AsEnumerable())})";
    }

    private class GenericMethodData
    {
        public int NumVariants;
        public HashSet<ulong> CountedVariantAddresses = new();
        public HashSet<string> FullNames = new();
        public int TotalSizeInstructions;
        public List<OutputMethodData> OutputMethodData = new();

        public GenericMethodData(ApplicationAnalysisContext context, Il2CppMethodDefinition md)
        {
            if (!context.Binary.ConcreteGenericMethods.TryGetValue(md, out var concreteGenericMethods))
                return;

            foreach (var cpp2IlMethodRef in concreteGenericMethods)
            {
                if (CountedVariantAddresses.Contains(cpp2IlMethodRef.GenericVariantPtr))
                    continue;

                var outputMethod = new OutputMethodData() { FullName = cpp2IlMethodRef.ToString(), NonGenericVersionFullName = cpp2IlMethodRef.BaseMethod.HumanReadableSignature, IsRemovedByGenericSharing = true, };

                OutputMethodData.Add(outputMethod);

                if (!context.ConcreteGenericMethodsByRef.TryGetValue(cpp2IlMethodRef, out var concreteAnalysisContext))
                    continue; //Eliminated by generic sharing

                outputMethod.IsRemovedByGenericSharing = false;
                outputMethod.MachineCodeSizeBytes = concreteAnalysisContext.RawBytes.Length;

                CountedVariantAddresses.Add(cpp2IlMethodRef.GenericVariantPtr);
                FullNames.Add(cpp2IlMethodRef.ToString());
                TotalSizeInstructions += concreteAnalysisContext.RawBytes.Length;
            }
        }
    }
}
