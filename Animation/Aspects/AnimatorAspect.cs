using Unity.Collections;
using Unity.Entities;

namespace NSprites
{
    public readonly partial struct AnimatorAspect : IAspect
    {

        private readonly Entity _entity;

        private readonly RefRW<AnimationReference> _animationIndex;
        private readonly RefRW<AnimationTimer> _animationTimer;
        private readonly RefRW<FrameIndex> _frameIndex;
        private readonly RefRW<AnimationState> _animationState;
        private readonly RefRO<IndexedAnimationsName> _indexedAnimationsName;
        private readonly RefRO<AnimationPlaybackType> _animationPlaybackType;
        private readonly RefRO<AnimationSetLink> _animationSetLink;

        public void SetLoopState(bool isLoop)
        {
            _animationState.ValueRW.loop = isLoop;
        }

        public void SetPauseState(bool isPause)
        {
            _animationState.ValueRW.pause = isPause;
        }

        public void SetAnimation(FixedString64Bytes animationName, in double worldTime)
        {

            ref var animSet = ref _animationSetLink.ValueRO.value.Value;
            bool foundAnimation = _indexedAnimationsName.ValueRO.indexedAnimationsNameCollection.Value.TryGetValue(animationName, out int setToAnimIndex);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!foundAnimation)
                throw new NSpritesException($"{nameof(AnimatorAspect)}.{nameof(SetAnimation)}: incorrect {nameof(setToAnimIndex)} was passed. {_entity} has no animation with such name ({animationName}) was found");
#endif

            // Remet à zéro les données et change l'animation.
            if (foundAnimation)
            {
                _animationIndex.ValueRW.index = setToAnimIndex;
                _animationIndex.ValueRW.animationName = animationName;
                ResetAnimation(worldTime);
            }
        }

        public void SetPlayback(int playback)
        {

            if (playback == _animationPlaybackType.ValueRO.forward || playback == _animationPlaybackType.ValueRO.backward)
            {
                _animationState.ValueRW.playback = playback;
            }
        }

        public void SetFramesDuration(float framesDuration)
        {
            _animationState.ValueRW.currentFramesDuration = framesDuration;
            _animationState.ValueRW.currentAnimationDuration = framesDuration 
                * _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.index].FrameCount;
        }

        public void SetToFrame(int frameIndex, in double worldTime)
        {
            ref var animData = ref _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.index];
            _frameIndex.ValueRW.value = frameIndex;
            _animationTimer.ValueRW.value = worldTime + _animationState.ValueRO.currentFramesDuration;
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

        public void ResetAnimation(in double worldTime)
        {
            SetToFrame(0, worldTime);
            ResetLoop();
            ResetPause();
            ResetPlayback();
            ResetFramesDuration();
            ResetAnimationDuration();
        }

        private ref SpriteAnimationBlobData GetCurrentAnimation()
        {
            return ref _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.index];
        }
    }
}