using CRL.BlobHashMaps;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites.Authoring
{
    public static partial class BakerExtensions
    {
        public static void BakeAnimation<T>(this Baker<T> baker, in Entity entity, SpriteAnimationsSets animationsSetData, string initialAnimationName)
            where T : Component
        {
            if(baker == null)
            {
                Debug.LogError(new NSpritesException("Passed Baker is null"));
                return;
            }
            if (animationsSetData == null)
            {
                Debug.LogError(new NSpritesException("Passed AnimationsSets is null"));
                return;
            }

            baker.DependsOn(animationsSetData);

            if (animationsSetData == null)
                return;
            
            #region création blob et map animation
            var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use `using` keyword because there is extension which use this + ref
            ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
            var animationsSet = animationsSetData.AnimationsSets;

            // TODO optimiser cette section en récupérant la longueur et le spritesheet.
            // Ici on calcule le nombre d'animations à alouer dans le tableau.
            var allocNumber = 0;
            foreach (var set in animationsSet)
                allocNumber += set.Animations.Length;

            var animationArray = blobBuilder.Allocate(ref root, allocNumber);

            var animationMap = new NativeHashMap<FixedString64Bytes, int>(128, Allocator.Temp);
            var animIndex = 0;
            foreach (var set in animationsSet)
            {

                foreach(var animation in set.Animations)
                {

                    var uv = NSpritesUtils.GetTextureST(animation.SpriteSheet);
                    var gridsize = animation.FrameCount;
                    animationArray[animIndex] = new SpriteAnimationBlobData
                    {

                        GridSize = gridsize,
                        FrameRange = animation.FrameRange.IsDefault
                            ? new int2(0, animation.FrameCount.x * animation.FrameCount.y)
                            : animation.FrameRange,
                        UVAtlas = uv,
                        frameSize = new float2(new float2(uv.x, uv.y) / gridsize),

                        FlipX = animation.flipX,

                        FramesDuration = animation.FramesDuration,
                        AnimationDuration = animation.FramesDuration * animation.FrameCount.x * animation.FrameCount.y,

                        Playback = animation.typeAnimation,
                        Loop = animation.animationABoucler,
                        Pause = animation.animationEnPause
                    };

                    animationMap.Add(animation.nomAnimation, animIndex++);
                }
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

            var initialAnimationIndex = blobMapAnimationsReference.Value[initialAnimationName];
            ref var initialAnim = ref blobAssetReference.Value[initialAnimationIndex];

            baker.AddComponent(entity, new AnimationSetLink { value = blobAssetReference });

            baker.AddComponent(entity, new AnimationState // Valeurs par défaut
            {
                CurrentFramesDuration = initialAnim.FramesDuration,
                CurrentAnimationDuration = initialAnim.AnimationDuration,
                Playback = initialAnim.Playback,
                Loop = initialAnim.Loop, 
                Pause = initialAnim.Pause,
                AnimationIndex = initialAnimationIndex,
                Time = initialAnim.FramesDuration
            });

            baker.AddComponent(entity, new IndexedAnimationsName
            {
                IndexedAnimationsNameCollection = blobMapAnimationsReference
            });
            
            baker.AddComponent(entity, new MainTexSTInitial { value = initialAnim.UVAtlas });

            #endregion
        }
    }
}