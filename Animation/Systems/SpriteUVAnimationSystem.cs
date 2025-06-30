using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;

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

            private void Execute(ref AnimationTimer animationTimer,
                                    ref FrameIndex frameIndex,
                                    ref UVAtlas uvAtlas,
                                    ref AnimationState animationState,
                                    in AnimationSetLink animationSet,
                                    in AnimationIndex animationIndex)
            {

                // On ne change pas de trame si l'animation est en pause ou si du délais doit être écoulé.
                var timerDelta = Time - animationTimer.value;
                if (timerDelta < 0f || animationState.pause) 
                    return;

                // On vérifie ici si l'animation à rencontré sa fin.
                ref var animData = ref animationSet.value.Value[animationIndex.value];
                animationState.pause = ++frameIndex.value == animData.FrameCount && !animationState.loop;

                frameIndex.value = frameIndex.value % animData.FrameCount;  // Changement de trame

                // Gère les pics de lag (EXPERIMENTAL)
                if (timerDelta >= animData.FramesDuration)
                {
                    var extraTime = (float)(timerDelta % animData.AnimationDuration); // Temps à rattraper
                    var decCount = (int)math.round((extraTime - animData.FramesDuration) / animData.FramesDuration); // Nombre de frames à passer
                    frameIndex.value = (frameIndex.value + decCount) % animData.FrameCount;
                }
                
                animationTimer.value = Time + animData.FramesDuration;

                // Mise à jour du découpage de la texture.
                var textureFrameIndex = frameIndex.value + animData.FrameOffset;
                var frameSize = new float2(animData.UVAtlas.xy / animData.GridSize);
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