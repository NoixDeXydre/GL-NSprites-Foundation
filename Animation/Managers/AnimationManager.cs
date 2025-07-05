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
            return _animationState.AnimationIndex == index;
        }

        public static bool IsCurrentAnimationWithName(ref AnimationState _animationState, FixedString64Bytes animationName)
        {
            return _animationState.AnimationName.Equals(animationName);
        }
        
        public static void SetLoopState(ref AnimationState _animationState, bool isLoop)
        {
            _animationState.Loop = isLoop;
        }

        public static void SetPauseState(ref AnimationState _animationState, bool isPause)
        {
            _animationState.Pause = isPause;
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
            bool foundAnimation = _indexedAnimationsName.IndexedAnimationsNameCollection.Value
                .TryGetValue(animationName, out int setToAnimIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!foundAnimation)
                throw new NSpritesException($"{nameof(AnimationManager)}.{nameof(SetAnimation)}: incorrect {nameof(setToAnimIndex)} was passed. The entity has no animation with such name ({animationName}) was found");
#endif

            // Remet à zéro les données et change l'animation.
            if (foundAnimation)
            {
                _animationState.AnimationIndex = setToAnimIndex;
                _animationState.AnimationName = animationName;
                ResetAnimation(ref _animationState, _animationSetLink, worldTime, keepFrameIndex);
            }
        }

        public static void SetPlayback(ref AnimationState _animationState, int playback)
        {

            if (playback == (int)TypesLecture.lectureAvant || playback == (int)TypesLecture.lectureArriere)
            {
                _animationState.Playback = playback;
            }
        }

        public static void SetFramesDuration(ref AnimationState _animationState,
            RefRO<AnimationSetLink> _animationSetLink, float framesDuration)
        {
            _animationState.CurrentFramesDuration = framesDuration;
            _animationState.CurrentAnimationDuration = framesDuration 
                * _animationSetLink.ValueRO.value.Value[_animationState.AnimationIndex].FrameCount;
        }

        public static void SetToFrame(ref AnimationState _animationState, int frameIndex, in double worldTime)
        {
            _animationState.FrameIndex = frameIndex;
            _animationState.Time = worldTime - _animationState.CurrentFramesDuration;
        }

        public static void ResetLoop(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) => 
            _animationState.Loop = GetCurrentAnimation(ref _animationState, _animationSetLink).Loop;

        public static void ResetPause(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.Pause = GetCurrentAnimation(ref _animationState, _animationSetLink).Pause;

        public static void ResetPlayback(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.Playback = GetCurrentAnimation(ref _animationState, _animationSetLink).Playback;

        public static void ResetFramesDuration(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.CurrentFramesDuration = GetCurrentAnimation(ref _animationState, _animationSetLink).FramesDuration;

        public static void ResetAnimationDuration(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink) =>
            _animationState.CurrentAnimationDuration = GetCurrentAnimation(ref _animationState, _animationSetLink).AnimationDuration;

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
                _animationState.FrameIndex 
                    = math.clamp(_animationState.FrameIndex, 0, GetCurrentAnimation(ref _animationState, _animationSetLink).FrameCount - 1);
            }
            else
            {
                SetToFrame(ref _animationState, 0, worldTime);
            }
        }

        private static ref SpriteAnimationBlobData GetCurrentAnimation(ref AnimationState _animationState,
            in AnimationSetLink _animationSetLink)
        {
            return ref _animationSetLink.value.Value[_animationState.AnimationIndex];
        }
    }
}