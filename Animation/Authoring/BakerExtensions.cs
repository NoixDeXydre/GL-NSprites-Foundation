using CRL.BlobHashMaps;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites.Authoring
{
    public static partial class BakerExtensions
    {
        public static void BakeAnimation<T>(this Baker<T> baker, in Entity entity, SpriteAnimationSet animationSet, int initialAnimationIndex = 0)
            where T : Component
        {
            if(baker == null)
            {
                Debug.LogError(new NSpritesException("Passed Baker is null"));
                return;
            }
            if (animationSet == null)
            {
                Debug.LogError(new NSpritesException("Passed AnimationSet is null"));
                return;
            }

            baker.DependsOn(animationSet);

            if (animationSet == null)
                return;

            if (initialAnimationIndex >= animationSet.Animations.Count || initialAnimationIndex < 0)
            {
                Debug.LogError(new NSpritesException($"Initial animation index {initialAnimationIndex} can't be less than 0 or great/equal to animation count {animationSet.Animations.Count}"));
                return;
            }
            
            #region création blob et map animation
            var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use `using` keyword because there is extension which use this + ref
            ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
            var animations = animationSet.Animations;
            var animationArray = blobBuilder.Allocate(ref root, animations.Count);

            var animationMap = new NativeHashMap<FixedString64Bytes, int>(128, Allocator.Temp);
            var animIndex = 0;
            foreach (var anim in animations)
            {

                var uv = NSpritesUtils.GetTextureST(anim.SpriteSheet);
                var gridsize = anim.FrameCount;
                animationArray[animIndex] = new SpriteAnimationBlobData
                {

                    GridSize = gridsize,
                    FrameRange = anim.FrameRange.IsDefault
                        ? new int2(0, anim.FrameCount.x * anim.FrameCount.y)
                        : anim.FrameRange,
                    UVAtlas = uv,
                    frameSize = new float2(new float2(uv.x, uv.y) / gridsize),
                    FramesDuration = anim.FramesDuration,
                    AnimationDuration = anim.FramesDuration * anim.FrameCount.x * anim.FrameCount.y,

                    playback = anim.typeAnimation,
                    loop = anim.animationABoucler,
                    pause = anim.animationEnPause
                };

                animationMap.Add(anim.nomAnimation, animIndex++);
            }

            var blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<SpriteAnimationBlobData>>(Allocator.Persistent);
            baker.AddBlobAsset(ref blobAssetReference, out _);
            blobBuilder.Dispose();

            var builderMapAnimations = new BlobBuilder(Allocator.Temp);
            ref var rootMapAnimations = ref builderMapAnimations.ConstructRoot<BlobHashMap<FixedString64Bytes, int>>();
            builderMapAnimations.ConstructHashMap(ref rootMapAnimations, ref animationMap);

            var blobMapAnimationsReference = builderMapAnimations.CreateBlobAssetReference<BlobHashMap<FixedString64Bytes, int>>(Allocator.Persistent);
            baker.AddBlobAsset(ref blobMapAnimationsReference, out _);
            builderMapAnimations.Dispose();

            #endregion

            #region ajout des composants

            ref var initialAnim = ref blobAssetReference.Value[initialAnimationIndex];

            baker.AddComponent(entity, new AnimationSetLink { value = blobAssetReference });

            baker.AddComponent(entity, new AnimationState // Valeurs par défaut
            {
                currentFramesDuration = initialAnim.FramesDuration,
                currentAnimationDuration = initialAnim.AnimationDuration,
                playback = initialAnim.playback,
                loop = initialAnim.loop, 
                pause = initialAnim.pause,
                animationIndex = initialAnimationIndex,
                time = initialAnim.FramesDuration
            });

            baker.AddComponent(entity, new IndexedAnimationsName
            {
                indexedAnimationsNameCollection = blobMapAnimationsReference
            });
            
            baker.AddComponent(entity, new MainTexSTInitial { value = initialAnim.UVAtlas });

            #endregion
        }
    }
}