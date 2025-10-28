using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;

namespace Anatawa12.AvatarOptimizer.Processors.SkinnedMeshes
{
    internal class RenameBlendShapeProcessor : EditSkinnedMeshProcessor<RenameBlendShape>
    {
        public RenameBlendShapeProcessor(RenameBlendShape component) : base(component)
        {
        }

        public override EditSkinnedMeshProcessorOrder ProcessOrder => EditSkinnedMeshProcessorOrder.AfterFreezeBlendShape;

        public override void Process(BuildContext context, MeshInfo2 target)
        {
            var mapping = CollectBlendShapeSources(target.BlendShapes, Component.nameMap.GetAsMap());
            var weightMap = target.BlendShapes.ToDictionary(x => x.name, x => x.weight);
            WarnErrorConflicts(mapping);
            SetTemporalNameForEmpty(mapping);
            DoRenameBlendShapes(target, mapping);
        }

        private void SetTemporalNameForEmpty(List<(string name, float weight, string source)> mapping)
        {
            var usingNames = new HashSet<string>(mapping.Select(x => x.name));

            var index = 0;

            string NewName()
            {
                while (true)
                {
                    var name = $"AAO-EmptyName-Placeholder-{index}";
                    index++;
                    if (!usingNames.Contains(name))
                        return name;
                }
            }

            for (var i = 0; i < mapping.Count; i++)
            {
                var tuple = mapping[i];
                if (tuple.name == "")
                    mapping[i] = tuple with { name = NewName() };
            }
        }

        public static void DoRenameBlendShapes(MeshInfo2 target, List<(string name, float weight, string source)> mapping)
        {
            foreach (var vertexPerBuffer in target.Vertices.GroupBy(x => x.BlendShapeBuffer))
            {
                var buffer = vertexPerBuffer.Key;
                var newShapes = new Dictionary<string, BlendShapeShape>();

                foreach (var (newName, _, source) in mapping)
                {
                    if (buffer.Shapes.Remove(source, out var shapeShape))
                        newShapes.Add(newName, shapeShape);
                }

                // reinitialize buffer
                buffer.Shapes.Clear();
                foreach (var (name, shape) in newShapes)
                    buffer.Shapes.Add(name, shape);
            }

            target.BlendShapes.Clear();
            target.BlendShapes.AddRange(mapping.Select(x => (x.name, x.weight)));
        }

        private void WarnErrorConflicts(List<(string name, float weight, string sources)> mapping)
        {
            // check for weight / animation conflicts
            // weight conflicts are error; animation conflicts can be false positive so warning
            foreach (var (name, _, sources) in mapping)
            {
                if (name == "")
                {
                    BuildLog.LogError("RenameBlendShape:error:empty-name", string.Join(", ", sources));
                }
            }            
        }

        public static List<(string name, float weight, string sources)> CollectBlendShapeSources(
            IEnumerable<(string, float)> blendShapes, Dictionary<string, string?> mapping)
        {
            var newList = new List<(string, float, string source)>();

            var duplicatedNames = new HashSet<string>();

            foreach (var (name, weight) in blendShapes)
            {
                if (!mapping.TryGetValue(name, out var newName) || newName == null)
                    newName = name;

                var found = newList.Find(x => x.Item1 == newName);
                if (newName == "")
                {
                    // empty name should be kept as is and not merged
                    newList.Add((newName, weight, name));
                }
                else if (found.source == null)
                {
                    // new one
                    newList.Add((newName, weight, name));
                }
                else
                {
                    // merging to existing
                    duplicatedNames.Add(newName);
                }
            }

            foreach (var duplicatedName in duplicatedNames)
            {
                BuildLog.LogError("RenameBlendShape:error:after-name-conflict", duplicatedName);
            }

            return newList;
        }

        public override IMeshInfoComputer GetComputer(IMeshInfoComputer upstream) => new MeshInfoComputer(this, upstream);

        class MeshInfoComputer : AbstractMeshInfoComputer
        {
            private readonly RenameBlendShapeProcessor _processor;

            public MeshInfoComputer(RenameBlendShapeProcessor processor, IMeshInfoComputer upstream) : base(upstream)
                => _processor = processor;

            public override (string, float)[] BlendShapes()
            {
                return CollectBlendShapeSources(base.BlendShapes(), _processor.Component.nameMap.GetAsMap())
                    .Select(x => (x.name, x.weight))
                    .ToArray();
            }
        }
    }
}
