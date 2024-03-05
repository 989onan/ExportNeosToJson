using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExportResoniteToJson
{
    public class ExportResoniteToJson : ResoniteMod
    {
        public override string Name => "ExportResoniteToJson";
        public override string Author => "runtime";
        public override string Version => "2.0.0";
        public override string Link => "https://github.com/zkxs/ExportNeosToJson";


        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.michaelripley.ExportResoniteToJson");
            FieldInfo formatsField = AccessTools.DeclaredField(typeof(ModelExportable), "formats");
            if (formatsField == null)
            {
                Error("could not read ModelExportable.formats");
                return;
            }

            // inject addional formats
            List<string> modelFormats = new List<string>((string[])formatsField.GetValue(null));
            modelFormats.Add("JSON");
            modelFormats.Add("RawBSON");
            modelFormats.Add("BRSON");
            modelFormats.Add("7ZBSON");
            modelFormats.Add("LZ4BSON");
            formatsField.SetValue(null, modelFormats.ToArray());

            MethodInfo exportModelOriginal = AccessTools.DeclaredMethod(typeof(ModelExporter), nameof(ModelExporter.ExportModel), new Type[] {
                typeof(Slot),
                typeof(string),
                typeof(Predicate<Component>) });
            if (exportModelOriginal == null)
            {
                Error("Could not find ModelExporter.ExportModel(Slot, string, Predicate<Component>)");
                return;
            }
            MethodInfo exportModelPrefix = AccessTools.DeclaredMethod(typeof(ExportResoniteToJson), nameof(ExportModelPrefix));
            harmony.Patch(exportModelOriginal, prefix: new HarmonyMethod(exportModelPrefix));

            Msg("Hook installed successfully");
        }

        private static bool ExportModelPrefix(Slot slot, string targetFile, Predicate<Component> filter, ref Task<bool> __result)
        {
            string extension = Path.GetExtension(targetFile).Substring(1).ToUpper();
            SavedGraph graph;
            switch (extension)
            {
                case "JSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportJSON(graph, targetFile);
                    return false; // skip original function
                case "7ZBSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = Export7zbson(graph, targetFile);
                    return false; // skip original function
                case "RawBSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ToRawBSON(graph, targetFile);
                    return false; // skip original function
                case "LZ4BSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportLz4bson(graph, targetFile);
                    return false; // skip original function
                case "BRSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportBrson(graph, targetFile);
                    return false; // skip original function
                default:
                    return true; // call original function
            }
        }

        private static async Task<bool> ToRawBSON(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToRawBSON(graph.Root, fileStream);
            }
            Msg(string.Format("exported {0}", targetFile));
            return true; // call original function
        }

        private static async Task<bool> ExportJSON(SavedGraph graph, string targetFile)
        {

            await new ToBackground();

            //aaaaand stole some code from https://github.com/art0007i/LocalStorage/blob/master/LocalStorage/LocalStorage.cs AddItem.
            using (var fs = File.CreateText(targetFile))
            {
                var wr = new JsonTextWriter(fs);
                wr.Indentation = 2;
                wr.Formatting = Formatting.Indented;
                var writeFunc = AccessTools.Method(typeof(DataTreeConverter), "Write");
                writeFunc.Invoke(null, new object[] { graph, wr });
            }
            Msg(string.Format("exported {0}", targetFile));
            return true;
        }

        private static async Task<bool> ExportBrson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToBRSON(graph.Root, fileStream);
            }
            Msg(string.Format("exported {0}", targetFile));
            return true; // call original function
        }

        private static async Task<bool> ExportLz4bson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToLZ4BSON(graph.Root, fileStream);
            }
            Msg(string.Format("exported {0}", targetFile));
            return true;
        }

        private static async Task<bool> Export7zbson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.To7zBSON(graph.Root, fileStream);
            }
            Msg(string.Format("exported {0}", targetFile));
            return true;
        }
    }
}
