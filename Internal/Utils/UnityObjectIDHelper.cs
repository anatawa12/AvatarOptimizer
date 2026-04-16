using System;
using UnityEditor;
using UnityEngine;
namespace Anatawa12.AvatarOptimizer;

public static class UnityObjectIDHelper
{
    public static EntityId GetEntityIDCompatible(this UnityEngine.Object unityObject)
    {
#if UNITY_6000_4_OR_NEWER
        return unityObject.GetEntityId();
#else
       return new (unityObject.GetInstanceID());
#endif
    }

    public static UnityEngine.Object EntityIdToObject(EntityId entityId)
    {
#if UNITY_6000_4_OR_NEWER
        return EditorUtility.EntityIdToObject(entityId);
#else
        return EditorUtility.InstanceIDToObject(entityId.InstanceID);
#endif
    }

    public static EntityId ObjectReferenceEntityIdValue(this SerializedProperty unityObject)
    {
#if UNITY_6000_4_OR_NEWER
        return unityObject.objectReferenceEntityIdValue;
#else
        return new (unityObject.objectReferenceInstanceIDValue);
#endif
    }
}
#if !UNITY_6000_4_OR_NEWER
public struct EntityId : IEquatable<EntityId>
{
    public int InstanceID;

    public EntityId(int id)
    {
        InstanceID = id;
    }

    public bool Equals(EntityId other)
    {
        return InstanceID == other.InstanceID;
    }

    public override bool Equals(object obj)
    {
        return obj is EntityId eid && Equals(eid);
    }
    public override int GetHashCode() => InstanceID;
    public static EntityId None => default(EntityId);


    public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
    public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
}
#endif
