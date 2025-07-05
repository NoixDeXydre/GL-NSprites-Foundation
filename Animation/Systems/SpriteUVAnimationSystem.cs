using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{

    [BurstCompile]
    public partial struct SpriteUVAnimationSystem : ISystem
    {
        [BurstCompile]
        [WithNone(typeof(CullSpriteTag))]
        private partial struct AnimationJob : IJobEntity
        {
            public double Time;

            private void Execute(ref UVAtlas uvAtlas,
                ref AnimationState animationState, 
                in AnimationSetLink animationSet)
            {

                // On s'arrête là en pause.
                if (animationState.Pause)
                    return;

                ref var animData = ref animationSet.value.Value[animationState.AnimationIndex];
                var timerDelta = Time - animationState.Time;
                var framesDuration = animationState.CurrentFramesDuration;
                if (timerDelta >= framesDuration)
                {

                    // Avance le nombre de trames requises.
                    int framesToAdvance = (int)(timerDelta / framesDuration);
                    animationState.FrameIndex = IncrementFrames(animationState.FrameIndex,
                        framesToAdvance * animationState.Playback, animData.FrameCount);
                    animationState.Time += framesToAdvance * framesDuration;

                    // On vérifie ici si l'animation à rencontré sa fin.
                    animationState.Pause = animationState.FrameIndex == 0 && !animationState.Loop;
                }

                // Mise à jour du découpage de la texture.
                var textureFrameIndex = animationState.FrameIndex + animData.FrameOffset;
                var framePosition = new int2(textureFrameIndex % animData.GridSize.x,
                    animData.GridSize.y - 1 - textureFrameIndex / animData.GridSize.x);
                uvAtlas = new UVAtlas 
                { 
                    value = new float4(animData.frameSize, animData.UVAtlas.zw + animData.frameSize * framePosition) 
                };
            }
        }
        
        [BurstCompile]
        private void OnUpdate(ref SystemState state)
        {
            var animationJob = new AnimationJob { Time = SystemAPI.Time.ElapsedTime };
            state.Dependency = animationJob.ScheduleParallelByRef(state.Dependency);
        }

        /// Changement de trame cyclique (dans les deux sens.)
        [BurstCompile]
        private static int IncrementFrames(int currentFrame, int incrementation, int totalFrames)
        {
            return ((currentFrame + incrementation) % totalFrames + totalFrames) % totalFrames;
        }
    }
}