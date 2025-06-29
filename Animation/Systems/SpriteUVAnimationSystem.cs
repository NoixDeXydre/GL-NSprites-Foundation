using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;

namespace NSprites
{
    // TODO: check animation system can work with different frame size animations 

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

            private void Execute(ref AnimationTimer animationTimer,
                                    ref FrameIndex frameIndex,
                                    ref UVAtlas uvAtlas,
                                    ref AnimationSetLink animationSet,
                                    ref AnimationState animationState,
                                    in AnimationIndex animationIndex)
            {

                var timerDelta = Time - animationTimer.value;

                if (timerDelta < 0f) 
                    return;
                
                // On ne change pas de trame si l'animation est en pause.
                if (animationState.pause)
                {
                    return;
                }

                ref var animData = ref animationSet.value.Value[animationIndex.value];

                // Changement de trame
                if (frameIndex.value + 1 == animData.FrameCount)
                {
                    animationState.pause = !animationState.loop; // Sans boucle, l'animation sera en pause au prochain cycle.
                    frameIndex.value = 0;
                } else
                {
                    frameIndex.value++;
                }
                
                // Gère la durée de la trame
                if (timerDelta >= animData.FramesDuration)
                {
                    var extraTime = (float)(timerDelta % animData.AnimationDuration);
                    while (extraTime > animData.FramesDuration)
                    {
                        extraTime -= animData.FramesDuration;
                        frameIndex.value = (frameIndex.value + 1) % animData.FrameCount;
                    }
                }
                
                animationTimer.value = Time + animData.FramesDuration;

                // Mise à jour du découpage de la texture.

                var textureFrameIndex = frameIndex.value + animData.FrameOffset;
                var frameSize = new float2(animData.UVAtlas.xy / animData.GridSize);

                // y should be inverted because 0.0 in UV starts from left bottom point but we assume 1st frame is at left up point
                var framePosition = new int2(textureFrameIndex % animData.GridSize.x, animData.GridSize.y - 1 - textureFrameIndex / animData.GridSize.x);
                uvAtlas = new UVAtlas { value = new float4(frameSize, animData.UVAtlas.zw + frameSize * framePosition) };
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationJob = new AnimationJob { Time = SystemAPI.Time.ElapsedTime };
            state.Dependency = animationJob.ScheduleParallelByRef(state.Dependency);
        }
    }
}