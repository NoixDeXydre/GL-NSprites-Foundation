using Unity.Entities;

namespace NSprites
{
    public readonly partial struct AnimatorAspect : IAspect
    {

        private readonly Entity _entity;

        private readonly RefRW<AnimationIndex> _animationIndex;
        private readonly RefRW<AnimationTimer> _animationTimer;
        private readonly RefRW<FrameIndex> _frameIndex;
        private readonly RefRW<AnimationState> _animationState;
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

        public void SetAnimation(int toAnimationIndex, in double worldTime)
        {
            // find animation by animation ID
            ref var animSet = ref _animationSetLink.ValueRO.value.Value;
            var setToAnimIndex = -1;
            for (int i = 0; i < animSet.Length; i++)
                if (animSet[i].ID == toAnimationIndex)
                {
                    setToAnimIndex = i;
                    break;
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (setToAnimIndex == -1)
                throw new NSpritesException($"{nameof(AnimatorAspect)}.{nameof(SetAnimation)}: incorrect {nameof(toAnimationIndex)} was passed. {_entity} has no animation with such ID ({toAnimationIndex}) was found");
#endif

            // Remet à zéro les données et change l'animation.
            if (_animationIndex.ValueRO.value != setToAnimIndex)
            {
                _animationIndex.ValueRW.value = setToAnimIndex;
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
                * _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.value].FrameCount;
        }

        public void SetToFrame(int frameIndex, in double worldTime)
        {
            ref var animData = ref _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.value];
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
            return ref _animationSetLink.ValueRO.value.Value[_animationIndex.ValueRO.value];
        }
    }
}