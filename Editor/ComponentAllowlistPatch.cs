using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.Merger
{
    // https://github.com/bdunderscore/modular-avatar/blob/2bfefc7bacbbed2c9720b72b11aad3abcff0d564/Packages/nadena.dev.modular-avatar/Editor/ComponentAllowlistPatch.cs#L33-L131
    // Originally under MIT License
    // Copyright (c) 2022 bd_
    [InitializeOnLoad]
    internal static class ComponentAllowlistPatch
    {
        internal static readonly bool PATCH_OK;

        static ComponentAllowlistPatch()
        {
            try
            {
                PatchAllowlist();
                PATCH_OK = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PATCH_OK = false;
            }
        }

        static void PatchAllowlist()
        {
            // The below APIs are all public, but undocumented and likely to change in the future.
            // As such, we use reflection to access them (allowing us to catch exceptions instead of just breaking the
            // build - and allowing the user to manually bake as a workaround).

            // The basic idea is to retrieve the HashSet of whitelisted components, and add all components extending
            // from AvatarTagComponent to it. This HashSet is cached on first access, but the lists of allowed
            // components used to initially populate it are private. So, we'll start off by making a call that (as a
            // side-effect) causes the list to be initially cached. This call will throw a NPE because we're passing
            // a null GameObject, but that's okay.

            var avatarValidation = FindType("VRC.SDK3.Validation.AvatarValidation");
            var findIllegalComponents =
                avatarValidation?.GetMethod("FindIllegalComponents", BindingFlags.Public | BindingFlags.Static);

            if (findIllegalComponents == null)
            {
                Debug.LogError(
                    "[Merger] Unsupported VRCSDK version: Failed to find AvatarValidation.FindIllegalComponents");
                return;
            }

            try
            {
                findIllegalComponents.Invoke(null, new[] {(object) null});
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is NullReferenceException)
                {
                    // ok!
                }
                else
                {
                    System.Diagnostics.Debug.Assert(e.InnerException != null, "e.InnerException != null");
                    throw e.InnerException;
                }
            }

            // Now fetch the cached allowlist and add our components to it.
            var validationUtils = FindType("VRC.SDKBase.Validation.ValidationUtils");
            var whitelistedTypes = validationUtils?.GetMethod(
                "WhitelistedTypes",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] {typeof(string), typeof(IEnumerable<Type>)},
                null
            );

            if (whitelistedTypes == null)
            {
                Debug.LogError(
                    "[Merger] Unsupported VRCSDK version: Failed to find ValidationUtils.WhitelistedTypes");
                return;
            }

            var allowlist = whitelistedTypes.Invoke(null, new object[] {"avatar-sdk3", null}) as HashSet<Type>;
            if (allowlist == null)
            {
                Debug.LogError("[Merger] Unsupported VRCSDK version: Failed to retrieve component whitelist");
                return;
            }

            allowlist.Add(typeof(AvatarTagComponent));

            // We'll need to find all types which derive from AvatarTagComponent and inject them into the allowlist
            // as well.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var ty in assembly.GetTypes())
                {
                    if (typeof(AvatarTagComponent).IsAssignableFrom(ty))
                    {
                        allowlist.Add(ty);
                    }
                }
            }
        }
        
        public static Type FindType(string typeName)
        {
            Type avatarValidation = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                avatarValidation = assembly.GetType(typeName);
                if (avatarValidation != null)
                {
                    break;
                }
            }

            return avatarValidation;
        }
    }
}
