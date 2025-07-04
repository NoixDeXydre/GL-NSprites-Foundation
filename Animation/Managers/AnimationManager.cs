using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static SpriteAnimation;

namespace NSprites
{

    /// <summary>
    /// Gère les animations d'une entité.
    /// </summary>
    public static class AnimationManager
    {

        public static bool IsCurrentAnimationWithIndex(ref AnimationState _animationState, int index)
        {
            return _animationState.animationIndex == index;
        }

        public static bool IsCurrentAnimationWithName(ref AnimationState _animationState, FixedString64Bytes animationName)
        {
            return _animationState.animationName.Equals(animationName);
        }
        
        public static void SetLoopState(ref AnimationState _animationState, bool isLoop)
        {
            _animationState.loop = isLoop;
        }

        public static void SetPauseState(ref AnimationState _animationState, bool isPause)
        {
            _animationState.pause = isPause;
        }

        public static void SetAnimation(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink,
            in IndexedAnimationsName _indexedAnimationsName,
            FixedString64Bytes animationName, double worldTime, bool keepFrameIndex = false)
        {

            // Change d'animation SEULEMENT SI ce n'est pas la même.
            // Il faut reset l'animation si voulez faire les deux.
            if (IsCurrentAnimationWithName(ref _animationState, animationName))
            {
                return;
            }

            ref var animSet = ref _animationSetLink.value.Value;
            bool foundAnimation = _indexedAnimationsName.indexedAnimationsNameCollection.Value
                .TryGetValue(animationName, out int setToAnimIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!foundAnimation)
                throw new NSpritesException($"{nameof(AnimationManager)}.{nameof(SetAnimation)}: incorrect {nameof(setToAnimIndex)} was passed. The entity has no animation with such name ({animationName}) was found");
#endif

            // Remet à zéro les données et change l'animation.
            if (foundAnimation)
            {
                _animationState.animationIndex = setToAnimIndex;
                _animationState.animationName = animationName;
                ResetAnimation(ref _animationState, _animationSetLink, worldTime, keepFrameIndex);
            }
        }

        public static void SetPlayback(ref AnimationState _animationState, int playback)
        {

            if (playback == (int)TypesLecture.lectureAvant || playback == (int)TypesLecture.lectureArriere)
            {
                _animationState.playback = playback;
            }
        }

        public static void SetFramesDuration(ref AnimationState _animationState,
            RefRO<AnimationSetLink> _animationSetLink, float framesDuration)
        {
            _animationState.currentFramesDuration = framesDuration;
            _animationState.currentAnimationDuration = framesDuration 
                * _animationSetLink.ValueRO.value.Value[_animationState.animationIndex].FrameCount;
        }

        public static void SetToFrame(ref AnimationState _animationState, int frameIndex, in double worldTime)
        {
            _animationState.frameIndex = frameIndex;
            _animationState.time = worldTime - _animationState.currentFramesDuration;
        }

        public static void ResetLoop(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) => 
            _animationState.loop = GetCurrentAnimation(ref _animationState, _animationSetLink).loop;

        public static void ResetPause(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.pause = GetCurrentAnimation(ref _animationState, _animationSetLink).pause;

        public static void ResetPlayback(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.playback = GetCurrentAnimation(ref _animationState, _animationSetLink).playback;

        public static void ResetFramesDuration(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.currentFramesDuration = GetCurrentAnimation(ref _animationState, _animationSetLink).FramesDuration;

        public static void ResetAnimationDuration(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.currentAnimationDuration = GetCurrentAnimation(ref _animationState, _animationSetLink).AnimationDuration;

        public static void ResetAnimation(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink,
            double worldTime, bool keepFrameIndex = false)
        {

            ResetLoop(ref _animationState, _animationSetLink);
            ResetPause(ref _animationState, _animationSetLink);
            ResetPlayback(ref _animationState, _animationSetLink);
            ResetFramesDuration(ref _animationState, _animationSetLink);
            ResetAnimationDuration(ref _animationState, _animationSetLink);

            if (keepFrameIndex)
            {

                // Evite de chevaucher les frames.
                _animationState.frameIndex 
                    = math.clamp(_animationState.frameIndex, 0, GetCurrentAnimation(ref _animationState, _animationSetLink).FrameCount - 1);
            }
            else
            {
                SetToFrame(ref _animationState, 0, worldTime);
            }
        }

        private static ref SpriteAnimationBlobData GetCurrentAnimation(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink)
        {
            return ref _animationSetLink.value.Value[_animationState.animationIndex];
        }
    }
}