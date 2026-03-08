using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ExternalPackages
{
    public static class ExternalPackageUtility
    {
        private static bool _updatePending;
        
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            ScheduleUpdateDefines();
        }
        
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished; 
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }
        
        private static void OnCompilationFinished(object obj) => ScheduleUpdateDefines();
        
        private static void ScheduleUpdateDefines()
        {
            if (_updatePending)
                return;
            
            _updatePending = true;
            EditorApplication.delayCall += UpdateDefines;
        }
        
        private static void UpdateDefines()
        {
            _updatePending = false;
            Debug.Log("[ExternalPackages] Updating defines");
            var loadedAssemblies = CompilationPipeline.GetAssemblies().Select(a => a.name).ToHashSet();
            var definitions = FindAllAssetsOfType<ExternalPackageDefinition>().ToList();

            foreach (var target in GetAllNamedBuildTargets())
            {
                if (target == NamedBuildTarget.Unknown)
                    continue;

                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                var definesList = defines.ToList();
                var addedDefines = new List<string>();
                var removedDefines = new List<string>();
                
                foreach (var definition in definitions)
                {
                    if (string.IsNullOrWhiteSpace(definition.Assembly) || string.IsNullOrWhiteSpace(definition.Define))
                        continue;
                    
                    var present = loadedAssemblies.Contains(definition.Assembly);
                    var hasDefine = definesList.Contains(definition.Define);

                    if (present && !hasDefine)
                    {
                        definesList.Add(definition.Define);
                        addedDefines.Add(definition.Define);
                    }

                    if (!present && hasDefine)
                    {
                        definesList.Remove(definition.Define);
                        removedDefines.Add(definition.Define);
                    }
                }

                if (addedDefines.Count == 0 && removedDefines.Count == 0) 
                    continue;
                
                Debug.Log($"[ExternalPackages] Updated defines for {target.TargetName} +[{string.Join(",", addedDefines)}] -[{string.Join(",", removedDefines)}]");
                PlayerSettings.SetScriptingDefineSymbols(target, definesList.Distinct().ToArray());
            }
        }
        
        private static IEnumerable<NamedBuildTarget> GetAllNamedBuildTargets()
        {
            var fields = typeof(NamedBuildTarget).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(NamedBuildTarget) || f.GetCustomAttribute<System.ObsoleteAttribute>() != null) 
                    continue;
                var value = (NamedBuildTarget)f.GetValue(null);
                yield return value;
            }
        }
        
        private static IEnumerable<T> FindAllAssetsOfType<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
            if (guids == null)
                yield break;
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T tAsset) 
                        yield return tAsset;
                }
            }
        }
    }
}