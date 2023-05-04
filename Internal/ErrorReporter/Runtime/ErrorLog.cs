using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
//using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class ObjectRefLookupCache
    {
        private Dictionary<string, Dictionary<long, UnityEngine.Object>> _cache =
            new Dictionary<string, Dictionary<long, Object>>();

        internal UnityEngine.Object FindByGuidAndLocalId(string guid, long localId)
        {
            if (!_cache.TryGetValue(guid, out var fileContents))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                fileContents = new Dictionary<long, Object>(assets.Length);
                foreach (var asset in assets)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var _, out long detectedId))
                    {
                        fileContents[detectedId] = asset;
                    }
                }

                _cache[guid] = fileContents;
            }

            if (fileContents.TryGetValue(localId, out var obj))
            {
                return obj;
            }
            else
            {
                return null;
            }
        }
    }

    internal struct ObjectRef
    {
        /*[JsonProperty]*/ internal string guid;
        /*[JsonProperty]*/ internal long? localId;
        /*[JsonProperty]*/ internal string path, name;
        /*[JsonProperty]*/ internal string typeName;

        internal ObjectRef(Object obj)
        {
            this.guid = null;
            localId = null;

            if (obj == null)
            {
                this.guid = path = name = null;
                localId = null;
                typeName = null;
                return;
            }

            typeName = obj.GetType().Name;

            long id;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out id))
            {
                this.guid = guid;
                localId = id;
            }

            if (obj is Component c)
            {
                path = Utils.RelativePath(null, c.gameObject);
            }
            else if (obj is GameObject go)
            {
                path = Utils.RelativePath(null, go);
            }
            else
            {
                path = null;
            }

            name = string.IsNullOrWhiteSpace(obj.name) ? "<???>" : obj.name;
        }

        internal UnityEngine.Object Lookup(ObjectRefLookupCache cache)
        {
            if (path != null)
            {
                return FindObject(path);
            }
            else if (guid != null && localId.HasValue)
            {
                return cache.FindByGuidAndLocalId(guid, localId.Value);
            }
            else
            {
                return null;
            }
        }

        private static GameObject FindObject(string path)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == path) return root;
                if (path.StartsWith(root.name + "/"))
                {
                    return root.transform.Find(path.Substring(root.name.Length + 1))?.gameObject;
                }
            }

            return null;
        }

        public ObjectRef Remap(string original, string cloned)
        {
            if (path == cloned)
            {
                path = original;
                name = path.Substring(path.LastIndexOf('/') + 1);
            }
            else if (path != null && path.StartsWith(cloned + "/"))
            {
                path = original + path.Substring(cloned.Length);
                name = path.Substring(path.LastIndexOf('/') + 1);
            }

            return this;
        }
    }

    public enum ReportLevel
    {
        Validation,
        Info,
        Warning,
        Error,
        InternalError,
    }

    public partial class ErrorLog
    {
        /*[JsonProperty]*/ internal readonly List<ObjectRef> referencedObjects;
        /*[JsonProperty]*/ internal readonly ReportLevel reportLevel;
        internal Assembly messageAssembly;
        /*[JsonProperty]*/ internal readonly string messageAssemblyName;
        /*[JsonProperty]*/ internal readonly string messageCode;
        /*[JsonProperty]*/ internal readonly string[] substitutions;
        /*[JsonProperty]*/ internal readonly string stacktrace;

        [CanBeNull]
        internal Assembly MessageAssembly
        {
            get
            {
                if (messageAssembly == null)
                {
                    try
                    {
                        messageAssembly = Assembly.Load(messageAssemblyName);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                return messageAssembly;
            }
        }

        public ErrorLog(ReportLevel level, string code, string[] strings, object[] args, Assembly callerAssembly)
        {
            reportLevel = level;
            messageAssembly = callerAssembly;
            messageAssemblyName = messageAssembly.GetName().Name;

            substitutions = strings.Select(s => s.ToString()).ToArray();

            referencedObjects = args.Where(o => o is Component || o is GameObject)
                .Select(o => new ObjectRef(o is Component c ? c.gameObject : (GameObject) o))
                .ToList();
            referencedObjects.AddRange(Utils.GetCurrentReportActiveReferences());

            messageCode = code;
            stacktrace = null;
        }

        public ErrorLog(ReportLevel level, string code, string[] strings, params object[] args)
            : this(level, code, strings, args, Assembly.GetCallingAssembly())
        {
        }

        public ErrorLog(ReportLevel level, string code, params object[] args)
            : this(level, code, Array.Empty<string>(), args, Assembly.GetCallingAssembly())
        {
        }

        internal ErrorLog(Exception e, string additionalStackTrace = "")
            : this(ReportLevel.InternalError, 
                "ErrorReporter:error.internal_error", 
                new [] {e.Message, e.TargetSite?.Name}, 
                Array.Empty<object>(),
                typeof(ErrorLog).Assembly)
        {
            stacktrace = e.ToString() + additionalStackTrace;
        }

        public string ToString()
        {
            return "[" + reportLevel + "] " + messageCode + " " + "subst: " + string.Join(", ", substitutions);
        }
    }
}
