using System;
using System.Collections;
using System.Collections.Generic;
using Anatawa12.AvatarOptimizer.API;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer.APIInternal;

internal class UVUsageCompabilityAPIImpl : UVUsageCompabilityAPI.IUVUsageCompabilityAPIImpl
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        UVUsageCompabilityAPI.Impl = new UVUsageCompabilityAPIImpl();
    }

    private BitArray GetUsedChannels(SkinnedMeshRenderer renderer)
    {
        var usedChannels = new BitArray(8);

        var removeMeshByMask = renderer.GetComponent<RemoveMeshByMask>() as RemoveMeshByMask;
        var removeMeshByUVTile = renderer.GetComponent<RemoveMeshByUVTile>() as RemoveMeshByUVTile;
        var evacuate = renderer.GetComponent<InternalEvacuateUVChannel>() as InternalEvacuateUVChannel;

        if (removeMeshByMask != null)
        {
            usedChannels[0] = true;
        }

        if (removeMeshByUVTile != null)
        {
            foreach (var materialSlot in removeMeshByUVTile.materials)
            {
                if (materialSlot.RemoveAnyTile)
                {
                    usedChannels[(int)materialSlot.uvChannel] = true;
                }
            }
        }

        if (evacuate != null)
        {
            foreach (var evacuateEvacuation in evacuate.evacuations)
            {
                (usedChannels[evacuateEvacuation.savedChannel], usedChannels[evacuateEvacuation.originalChannel])
                    = (usedChannels[evacuateEvacuation.originalChannel], usedChannels[evacuateEvacuation.savedChannel]);
            }
        }

        return usedChannels;
    }

    public bool IsTexCoordUsed(SkinnedMeshRenderer renderer, int channel) => GetUsedChannels(renderer)[channel];

    public void RegisterTexCoordEvacuation(SkinnedMeshRenderer renderer, int originalChannel, int savedChannel)
    {
        var channels = GetUsedChannels(renderer);
        if (channels[savedChannel]) throw new InvalidOperationException("savedChannel is used by AAO");
        var evacuate = renderer.GetComponent<InternalEvacuateUVChannel>();
        if (evacuate == null)
        {
            evacuate = renderer.gameObject.AddComponent<InternalEvacuateUVChannel>();
            renderer.gameObject.AddComponent<InternalRevertEvacuateUVChannel>().evacuate = evacuate;
        }
        evacuate.evacuations.Add(new InternalEvacuateUVChannel.Evacuation
        {
            originalChannel = originalChannel,
            savedChannel = savedChannel,
        });
    }
}
