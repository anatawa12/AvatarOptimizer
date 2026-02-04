#if AAO_VRCSDK3_AVATARS

using System;
using VRC.SDK3.Avatars.Components;

namespace Anatawa12.AvatarOptimizer
{
    class AnimatorLayerMap<T>
    {
        private T[] _values = new T[(int)(VRCAvatarDescriptor.AnimLayerType.IKPose + 1)];

        public static bool IsValid(VRCAvatarDescriptor.AnimLayerType type)
        {
            switch (type)
            {
                case VRCAvatarDescriptor.AnimLayerType.Base:
                case VRCAvatarDescriptor.AnimLayerType.Additive:
                case VRCAvatarDescriptor.AnimLayerType.Gesture:
                case VRCAvatarDescriptor.AnimLayerType.Action:
                case VRCAvatarDescriptor.AnimLayerType.FX:
                case VRCAvatarDescriptor.AnimLayerType.Sitting:
                case VRCAvatarDescriptor.AnimLayerType.TPose:
                case VRCAvatarDescriptor.AnimLayerType.IKPose:
                    return true;
                case VRCAvatarDescriptor.AnimLayerType.Deprecated0:
                default:
                    return false;
            }
        }

        public static readonly VRCAvatarDescriptor.AnimLayerType[] ValidLayerTypes =
        {
            VRCAvatarDescriptor.AnimLayerType.Base,
            VRCAvatarDescriptor.AnimLayerType.Additive,
            VRCAvatarDescriptor.AnimLayerType.Gesture,
            VRCAvatarDescriptor.AnimLayerType.Action,
            VRCAvatarDescriptor.AnimLayerType.FX,
            VRCAvatarDescriptor.AnimLayerType.Sitting,
            VRCAvatarDescriptor.AnimLayerType.TPose,
            VRCAvatarDescriptor.AnimLayerType.IKPose,
        };

        public ref T this[VRCAvatarDescriptor.AnimLayerType type]
        {
            get
            {
                if (!IsValid(type))
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);

                return ref _values[(int)type];
            }
        }
    }
}

#endif
