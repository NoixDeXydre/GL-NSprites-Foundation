using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static SpriteAnimation;

namespace NSprites
{

    // TODO adapter pour cause d'obsolescence : https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/upgrade-guide.html#change-entitiesforeach-code

    public readonly partial struct AnimationManager : IAspect
    {

        private readonly Entity _entity;

        private readonly RefRW<AnimationReference> _animationReference;
        private readonly RefRW<AnimationTimer> _animationTimer;
        private readonly RefRW<FrameIndex> _frameIndex;
        private readonly RefRW<AnimationState> _animationState;
        private readonly RefRO<IndexedAnimationsName> _indexedAnimationsName;
        private readonly RefRO<AnimationSetLink> _animationSetLink;

        public bool IsCurrentAnimationWithIndex(int index)
        {
            return _animationReference.ValueRO.index == index;
        }

        public bool IsCurrentAnimationWithName(FixedString64Bytes animationName)
        {
            return _animationReference.ValueRO.animationName.Equals(animationName);
        }
        
        public void SetLoopState(bool isLoop)
        {
            _animationState.ValueRW.loop = isLoop;
        }

        public void SetPauseState(bool isPause)
        {
            _animationState.ValueRW.pause = isPause;
        }

        public void SetAnimation(FixedString64Bytes animationName, double worldTime, bool keepFrameIndex = false)
        {

            // Change d'animation SEULEMENT SI ce n'est pas la même.
            // Il faut reset l'animation si voulez faire les deux.
            if (IsCurrentAnimationWithName(animationName))
            {
                return;
            }

            ref var animSet = ref _animationSetLink.ValueRO.value.Value;
            bool foundAnimation = _indexedAnimationsName.ValueRO.indexedAnimationsNameCollection.Value.TryGetValue(animationName, out int setToAnimIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!foundAnimation)
                throw new NSpritesException($"{nameof(AnimationManager)}.{nameof(SetAnimation)}: incorrect {nameof(setToAnimIndex)} was passed. {_entity} has no animation with such name ({animationName}) was found");
#endif

            // Remet à zéro les données et change l'animation.
            if (foundAnimation)
            {
                _animationReference.ValueRW.index = setToAnimIndex;
                _animationReference.ValueRW.animationName = animationName;
                ResetAnimation(worldTime, keepFrameIndex);
            }
        }

        public void SetPlayback(int playback)
        {

            if (playback == (int)TypesLecture.lectureAvant || playback == (int)TypesLecture.lectureArriere)
            {
                _animationState.ValueRW.playback = playback;
            }
        }

        public void SetFramesDuration(float framesDuration)
        {
            _animationState.ValueRW.currentFramesDuration = framesDuration;
            _animationState.ValueRW.currentAnimationDuration = framesDuration 
                * _animationSetLink.ValueRO.value.Value[_animationReference.ValueRO.index].FrameCount;
        }

        public void SetToFrame(int frameIndex, in double worldTime)
        {
            ref var animData = ref _animationSetLink.ValueRO.value.Value[_animationReference.ValueRO.index];
            _frameIndex.ValueRW.value = frameIndex;
            _animationTimer.ValueRW.value = worldTime - _animationState.ValueRO.currentFramesDuration;
        }

        public void ResetLoop() => 
            _animationState.ValueRW.loop = GetCurrentAnimation().loop;

        public void ResetPause() =>
            _animationState.ValueRW.pause = GetCurrentAnimation().pause;

        public void ResetPlayback() =>
            _animationState.ValueRW.playback = GetCurrentAnimation().playback;

        public void ResetFramesDuration() =>
            _animationState.ValueRW.currentFramesDuration = GetCurrentAnimation().FramesDuration;

        public void ResetAnimationDuration() =>
            _animationState.ValueRW.currentAnimationDuration = GetCurrentAnimation().AnimationDuration;

        public void ResetAnimation(double worldTime, bool keepFrameIndex = false)
        {

            ResetLoop();
            ResetPause();
            ResetPlayback();
            ResetFramesDuration();
            ResetAnimationDuration();

            if (keepFrameIndex)
            {

                // Evite de chevaucher les frames.
                _frameIndex.ValueRW.value = math.clamp(_frameIndex.ValueRO.value, 0, GetCurrentAnimation().FrameCount - 1);
            }
            else
            {
                SetToFrame(0, worldTime);
            }
        }

        private ref SpriteAnimationBlobData GetCurrentAnimation()
        {
            return ref _animationSetLink.ValueRO.value.Value[_animationReference.ValueRO.index];
        }
    }
}