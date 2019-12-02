﻿using UnityEngine;

namespace UnityExtensions
{
    [AddComponentMenu("Miscellaneous/Platform Excluder")]
    public class PlatformExcluder : ScriptableComponent
    {
        [SerializeField, Flags]
        PlatformMask _includedPlatforms = default;


        void Awake()
        {
            if (!_includedPlatforms.Contains(Application.platform))
                Destroy(gameObject);
        }
    }
}