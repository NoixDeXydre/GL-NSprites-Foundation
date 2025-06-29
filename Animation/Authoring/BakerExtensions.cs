using System.Linq;
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
            
            #region création blob liste animations
            var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use `using` keyword because there is extension which use this + ref
            ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
            var animations = animationSet.Animations;
            var animationArray = blobBuilder.Allocate(ref root, animations.Count);

            var animIndex = 0;
            foreach (var anim in animations)
            {

                var animData = anim.data;

                // Construction des transitions

                var blobBuilderTransitions = new BlobBuilder(Allocator.Temp);
                ref var rootTransitions = ref blobBuilderTransitions.ConstructRoot<BlobArray<SpriteAnimationTransitionBlobData>>();
                var transitionsArray = blobBuilderTransitions.Allocate(ref rootTransitions, animData.SpritesTransitions.Count);
                var transitionsIndex = 0;
                foreach (var transition in animData.SpritesTransitions)
                {

                    transitionsArray[transitionsIndex] = new SpriteAnimationTransitionBlobData
                    {
                        GridSize = transition.spriteAnimation.FrameCount,
                        FrameRange = transition.spriteAnimation.FrameRange,
                        UVAtlas = NSpritesUtils.GetTextureST(transition.spriteAnimation.SpriteSheet),
                        AnimationDuration = transition.spriteAnimation.FrameDurations.Sum(),
                        // FrameDuration - allocate lately

                        loop = transition.spriteAnimation.animationABoucler,
                        pause = transition.spriteAnimation.animationEnPause,
                        redo_animation = transition.retourAnimationRacine
                    };

                    var durationsTransition = blobBuilderTransitions.Allocate(ref transitionsArray[transitionsIndex].FrameDurations, transition.spriteAnimation.FrameDurations.Length);
                    for (int di = 0; di < durationsTransition.Length; di++)
                        durationsTransition[di] = animData.FrameDurations[di];

                    transitionsIndex++;
                }

                var blobAssetTransitionReference = blobBuilderTransitions.CreateBlobAssetReference<BlobArray<SpriteAnimationTransitionBlobData>>(Allocator.Persistent);
                baker.AddBlobAsset(ref blobAssetTransitionReference, out _);
                blobBuilderTransitions.Dispose();

                animationArray[animIndex] = new SpriteAnimationBlobData
                {
                    ID = Animator.StringToHash(anim.name),
                    GridSize = animData.FrameCount,
                    FrameRange = animData.FrameRange.IsDefault
                        ? new int2(0, animData.FrameCount.x * animData.FrameCount.y)
                        : animData.FrameRange,
                    UVAtlas = NSpritesUtils.GetTextureST(animData.SpriteSheet),
                    AnimationDuration = animData.FrameDurations.Sum(),
                    AnimationTransitions = blobAssetTransitionReference,
                    // FrameDuration - allocate lately

                    loop = animData.animationABoucler,
                    pause = animData.animationEnPause
                };

                var durations = blobBuilder.Allocate(ref animationArray[animIndex].FrameDurations, animData.FrameDurations.Length);
                for (int di = 0; di < durations.Length; di++)
                    durations[di] = animData.FrameDurations[di];

                animIndex++;
            }

            var blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<SpriteAnimationBlobData>>(Allocator.Persistent);
            baker.AddBlobAsset(ref blobAssetReference, out _);
            blobBuilder.Dispose();
            #endregion

            ref var initialAnim = ref blobAssetReference.Value[initialAnimationIndex];

            baker.AddComponent(entity, new AnimationSetLink { value = blobAssetReference });

            baker.AddComponent(entity, new AnimationIndex { value = initialAnimationIndex });
            baker.AddComponent(entity, new AnimationTimer { value = initialAnim.FrameDurations[0] });
            baker.AddComponent(entity, new TransitionIndex { value = 0 });

            baker.AddComponent(entity, new AnimationState // Valeurs par défaut
            {
                hasRootAnimationFinished = false,
                loop = initialAnim.loop, 
                pause = initialAnim.pause
            });

            baker.AddComponent<FrameIndex>(entity);
            
            baker.AddComponent(entity, new MainTexSTInitial { value = initialAnim.UVAtlas });
        }
    }
}