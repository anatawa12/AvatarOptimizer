using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Anatawa12.Merger
{
    [AddComponentMenu("Anatawa12/Merge PhysBone")]
    internal class MergePhysBone : MonoBehaviour
    {
        [FormerlySerializedAs("component")] public VRCPhysBone[] components;
    }
}
