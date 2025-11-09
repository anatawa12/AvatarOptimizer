using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer;

public struct StreamingMipMapSettings
{
    public bool useStreamingMipMaps;
    public int streamingMipMapPriority;

    public static StreamingMipMapSettings MergeSettings(IEnumerable<StreamingMipMapSettings> settings)
    {
        var result = new StreamingMipMapSettings();
        foreach (var setting in settings)
        {
            result.useStreamingMipMaps |= setting.useStreamingMipMaps;
            result.streamingMipMapPriority = Math.Max(result.streamingMipMapPriority, setting.streamingMipMapPriority);
        }
        return result;
    }
}

public partial class Utils
{
    public static StreamingMipMapSettings GetStreamingMipMapSettings(this Texture2D texture)
    {
        using var serializedTexture = new SerializedObject(texture);
        var streamingMipmapsProperty = serializedTexture.FindProperty("m_StreamingMipmaps");
        var streamingMipmapsPriorityProperty = serializedTexture.FindProperty("m_StreamingMipmapsPriority");
        return new StreamingMipMapSettings()
        {
            useStreamingMipMaps = streamingMipmapsProperty.boolValue,
            streamingMipMapPriority = streamingMipmapsPriorityProperty.intValue,
        };
    }

    public static void SetStreamingMipMapSettings(this Texture2D texture, StreamingMipMapSettings settings)
    {
        using var serializedTexture = new SerializedObject(texture);
        var streamingMipmapsProperty = serializedTexture.FindProperty("m_StreamingMipmaps");
        var streamingMipmapsPriorityProperty = serializedTexture.FindProperty("m_StreamingMipmapsPriority");
        streamingMipmapsProperty.boolValue = settings.useStreamingMipMaps;
        streamingMipmapsPriorityProperty.intValue = settings.streamingMipMapPriority;
        serializedTexture.ApplyModifiedPropertiesWithoutUndo();
    }
}
