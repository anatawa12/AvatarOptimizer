using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
//using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    internal class ObjectRefLookupCache
    {
        private readonly Dictionary<(string, long), Object> _cache = new Dictionary<(string, long), Object>();

        internal Object FindByGuidAndLocalId(string guid, long localId)
        {
            if (!_cache.TryGetValue((guid, localId), out var obj))
            {
                if (GlobalObjectId.TryParse($"GlobalObjectId_V1-{1}-{guid}-{localId}-{0}", out var goid))
                {
                    obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(goid);
                    if (obj) _cache[(guid, localId)] = obj;
                }
            }

            return obj;
        }
    }

    [Serializable]
    internal struct ObjectRef
    {
        [SerializeField] internal string guid;
        // 0 is null.
        [SerializeField] internal long localId;
        [SerializeField] internal string path, name;
        [SerializeField] internal string typeName;

        internal ObjectRef(Object obj)
        {
            if (obj == null)
            {
                this = default;
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(obj.name) ? "<???>" : obj.name;
                if (obj is Component c)
                {
                    this = new ObjectRef(
                        path: Utils.RelativePath(null, c.gameObject),
                        name: name,
                        typeName: obj.GetType().Name);
                }
                else if (obj is GameObject go)
                {
                    this = new ObjectRef(
                        path: Utils.RelativePath(null, go), 
                        name: name, 
                        typeName: obj.GetType().Name);
                }
                else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long id))
                {
                    this = new ObjectRef(guid, id, name, obj.GetType().Name);
                }
                else
                {
                    this = default; // fallback
                }
            }
        }

        private ObjectRef([NotNull] string path, [NotNull] string name, [NotNull] string typeName)
        {
            guid = null;
            localId = 0;
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        private ObjectRef([NotNull] string guid, long localId, [NotNull] string name, [NotNull] string typeName)
        {
            this.guid = guid ?? throw new ArgumentNullException(nameof(guid));
            if (localId == 0) throw new ArgumentOutOfRangeException(nameof(guid), "guid must not be zero");
            this.localId = localId;
            path = null;
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        internal Object Lookup(ObjectRefLookupCache cache)
        {
            if (path != null)
                return FindObject(path);
            if (guid != null && localId != 0)
                return cache.FindByGuidAndLocalId(guid, localId);
            return null;
        }

        private static GameObject FindObject(string path)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == path) return root;
                if (path.StartsWith(root.name + "/", StringComparison.Ordinal))
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
                return new ObjectRef(
                    path: original,
                    name: path.Substring(path.LastIndexOf('/') + 1),
                    typeName: typeName);
            }
            if (path != null && path.StartsWith(cloned + "/", StringComparison.Ordinal))
            {
                return new ObjectRef(
                    path: original + path.Substring(cloned.Length),
                    name: path.Substring(path.LastIndexOf('/') + 1),
                    typeName: typeName);
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

    [Serializable]
    public partial class ErrorLog
    {
        [SerializeField] internal List<ObjectRef> referencedObjects;
        [SerializeField] internal ReportLevel reportLevel;
        internal Assembly messageAssembly;
        [SerializeField] internal string messageAssemblyName;
        [SerializeField] internal string messageCode;
        [SerializeField] internal string[] substitutions;
        [SerializeField] internal string stacktrace;

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

        public ErrorLog(ReportLevel level, string code, string[] strings, Assembly callerAssembly)
        {
            reportLevel = level;
            messageAssembly = callerAssembly;
            messageAssemblyName = messageAssembly.GetName().Name;

            substitutions = strings.Select(s => $"{s}").ToArray();

            referencedObjects = BuildReport.CurrentReport.GetActiveReferences().ToList();

            messageCode = code;
            stacktrace = null;
        }

        public ErrorLog(ReportLevel level, string code, string[] strings)
            : this(level, code, strings, Assembly.GetCallingAssembly())
        {
        }

        public ErrorLog WithContext(params object[] args)
        {
            referencedObjects.InsertRange(0,
                args.Where(o => o is Component || o is GameObject)
                    .Select(o => new ObjectRef(o is Component c ? c.gameObject : (GameObject)o))
                    .ToList());
            return this;
        }

        internal ErrorLog(Exception e, string additionalStackTrace = "")
            : this(ReportLevel.InternalError,
                "ErrorReporter:error.internal_error",
                new[] { e.Message, e.TargetSite?.Name },
                typeof(ErrorLog).Assembly)
        {
            stacktrace = e.ToString() + additionalStackTrace;
        }

        public string ToString()
        {
            return "[" + reportLevel + "] " + messageCode + " " + "subst: " + string.Join(", ", substitutions);
        }

        public static ErrorLog Validation(string code, params string[] strings)
            => new ErrorLog(ReportLevel.Validation, code, strings, Assembly.GetCallingAssembly());

        public static ErrorLog Info(string code, params string[] strings)
            => new ErrorLog(ReportLevel.Info, code, strings, Assembly.GetCallingAssembly());

        public static ErrorLog Warning(string code, params string[] strings)
            => new ErrorLog(ReportLevel.Warning, code, strings, Assembly.GetCallingAssembly());

        public static ErrorLog Error(string code, params string[] strings)
            => new ErrorLog(ReportLevel.Error, code, strings, Assembly.GetCallingAssembly());
    }
}
