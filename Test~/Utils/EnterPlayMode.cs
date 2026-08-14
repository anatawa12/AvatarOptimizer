using System;
using System.Collections;
using UnityEditor;
using UnityEngine.TestTools;

namespace Anatawa12.AvatarOptimizer.Test
{
    public class EnterPlayMode : IEditModeTestYieldInstruction
    {
        private UnityEngine.TestTools.EnterPlayMode _inner;

        public EnterPlayMode()
        {
            if (!EditorSettings.enterPlayModeOptionsEnabled
                || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0
                )
            {
                throw new Exception("EnterPlayMode requires 'Enter Play Mode Options' enabled and 'Disable Domain Reload' set.");
            }
            _inner = new UnityEngine.TestTools.EnterPlayMode(expectDomainReload: false);
        }

        public IEnumerator Perform() => _inner.Perform();

        public bool ExpectDomainReload => _inner.ExpectDomainReload;
        public bool ExpectedPlaymodeState => _inner.ExpectedPlaymodeState;
    }
}