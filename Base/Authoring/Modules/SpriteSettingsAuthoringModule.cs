using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites.Authoring
{

    [Serializable]
    public class SpriteSettingsAuthoringModule
    {

        public float2 Pivot = new(.5f);
        public float2 Size = new(1f);
        [Tooltip("Prevents changing Size when Sprite changed")] public bool LockSize;
        public Color Color = Color.white;

        /// <summary>
        /// Bakes sprite default (for NSprites-Foundation package) such as
        /// <list type="bullet">
        /// <item><see cref="UVAtlas"/> and <see cref="UVTilingAndOffset"/></item>
        /// <item><see cref="Scale2D"/></item>
        /// </list>
        /// </summary>
        /// <param name="baker">baker bruh</param>
        /// <param name="authoring">authoring monobehaviour</param>
        /// <param name="nativeSize">The native size of a sprite being baked. Needs because sprite and it's params can come from arbitrary source, so need to be passed</param>
        /// <param name="uvAtlas">The same as <see cref="nativeSize"/> should be passed, because of external sprite</param>
        public void Bake<TAuthoring>(Baker<TAuthoring> baker, TAuthoring authoring, in float2 nativeSize, in float4 uvAtlas)
            where TAuthoring : Component
        {
            var authoringTransform = authoring.transform;
            var authoringScale = authoringTransform.lossyScale;
            
            baker.BakeSpriteRender
            (
                baker.GetEntity(TransformUsageFlags.Dynamic),
                authoring,
                uvAtlas,
                Pivot,
                Size * nativeSize * new float2(authoringScale.x, authoringScale.y),
                Color
            );
        }

        public void TrySetSize(in float2 value)
        {
            if (LockSize)
                Debug.LogWarning($"{nameof(SpriteSettingsAuthoringModule)}: can't change size because {nameof(LockSize)} enabled");
            else
                Size = value;
        }
    }
}