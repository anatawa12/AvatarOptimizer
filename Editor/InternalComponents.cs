using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

[AddComponentMenu("")]
[DisallowMultipleComponent]
internal class InternalAutoFreezeMeaninglessBlendShape : EditSkinnedMeshComponent
{
}

[AddComponentMenu("")]
[DisallowMultipleComponent]
internal class InternalAutoFreezeNonAnimatedBlendShapes : EditSkinnedMeshComponent
{
}

internal class InternalEvacuateUVChannel : EditSkinnedMeshComponent
{
    public List<Evacuation> evacuations = new List<Evacuation>();
        
    [Serializable]
    public struct Evacuation
    {
        public int originalChannel;
        public int savedChannel;
    }

    public int EvacuateIndex(int index)
    {
        foreach (var evacuation in evacuations)
            if (evacuation.originalChannel == index)
                index = evacuation.savedChannel;
        return index;
    }
}

internal class InternalRevertEvacuateUVChannel : EditSkinnedMeshComponent
{
    public InternalEvacuateUVChannel evacuate = null!; // initialized on add
}
