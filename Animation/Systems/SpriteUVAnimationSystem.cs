using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{

    /// Compare <see cref="AnimationTimer"/> with global time and switch <see cref="FrameIndex"/> when timer expired.
    /// Perform only not-culled entities. Restore <see cref="FrameIndex"/> and duration time for entities which be culled for some time.
    /// Somehow calculations goes a bit wrong and unculled entities gets synchronized, don't know how to fix
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
                if (animationState.pause)
                    return;

                ref var animData = ref animationSet.value.Value[animationState.animationIndex];
                var timerDelta = Time - animationState.time;
                var framesDuration = animationState.currentFramesDuration;
                if (timerDelta >= framesDuration)
                {

                    // Avance le nombre de trames requises.
                    int framesToAdvance = (int)(timerDelta / framesDuration);
                    animationState.frameIndex = IncrementFrames(animationState.frameIndex,
                        framesToAdvance * animationState.playback, animData.FrameCount);
                    animationState.time += framesToAdvance * framesDuration;

                    // On vérifie ici si l'animation à rencontré sa fin.
                    animationState.pause = animationState.frameIndex == 0 && !animationState.loop;
                }

                // Mise à jour du découpage de la texture.
                var textureFrameIndex = animationState.frameIndex + animData.FrameOffset;
                var frameSize = new float2(animData.UVAtlas.xy / animData.GridSize);
                var framePosition = new int2(textureFrameIndex % animData.GridSize.x,
                    animData.GridSize.y - 1 - textureFrameIndex / animData.GridSize.x);
                uvAtlas = new UVAtlas { value = new float4(frameSize, animData.UVAtlas.zw + frameSize * framePosition) };
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