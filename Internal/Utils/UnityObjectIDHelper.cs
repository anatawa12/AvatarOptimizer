using System;
using UnityEditor;
using UnityEngine;
namespace Anatawa12.AvatarOptimizer;

public static class UnityObjectIDHelper
{

#if !UNITY_6000_2_OR_NEWER
    public static EntityId GetEntityId(this UnityEngine.Object unityObject)
    {
       return new (unityObject.GetInstanceID());
    }
#endif

    public static UnityEngine.Object EntityIdToObject(EntityId entityId)
    {
#if UNITY_6000_3_OR_NEWER
        return EditorUtility.EntityIdToObject(entityId);
#elif UNITY_6000_2_OR_NEWER
        return EditorUtility.InstanceIDToObject(entityId);
#else
        return EditorUtility.InstanceIDToObject(entityId.InstanceID);
#endif
    }

    public static EntityId ObjectReferenceEntityIdValue(this SerializedProperty unityObject)
    {
#if UNITY_6000_4_OR_NEWER
        return unityObject.objectReferenceEntityIdValue;
#elif UNITY_6000_2_OR_NEWER
        return unityObject.objectReferenceInstanceIDValue; // implicit cast
#else
        return new (unityObject.objectReferenceInstanceIDValue);
#endif
    }
}
#if !UNITY_6000_2_OR_NEWER
/// <summary>
/// The polyfill for EntityId introduced in Unity 6000.2.
/// </summary>
public readonly struct EntityId : IEquatable<EntityId>
{
    public readonly int InstanceID;

    public EntityId(int id)
    {
        InstanceID = id;
    }

    public bool Equals(EntityId other)
    {
        return InstanceID == other.InstanceID;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityId eid && Equals(eid);
    }
    public override int GetHashCode() => InstanceID;
    public static EntityId None => default(EntityId);

    public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
    public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

    public override string ToString() => InstanceID.ToString();
    public string ToString(string format) => InstanceID.ToString(format);
}
#endif
