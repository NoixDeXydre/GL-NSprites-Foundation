using Unity.Mathematics;

namespace NSprites
{

    public struct SpriteAnimationBlobData
    {

        public float4 UVAtlas;
        public int2 GridSize;
        public float2 frameSize; // Taille des frames fixes, calculé lors du backing

        public int2 FrameRange;
        public float FramesDuration;
        public float AnimationDuration; // Calculé pendant le baking

        public int Playback;

        public bool Loop;
        public bool Pause;

        public int FrameOffset => FrameRange.x;
        public int FrameCount => FrameRange.y;
    }
}