using Unity.Mathematics;

namespace NSprites
{

    // Ne devrait PAS changer
    public struct SpriteAnimationBlobData
    {

        public int ID;
        public float4 UVAtlas;
        public int2 GridSize;
        public int2 FrameRange;
        public float FramesDuration;
        public float AnimationDuration; // Calculé pendant le baking

        public int playback;

        public bool loop;
        public bool pause;

        public int FrameOffset => FrameRange.x;
        public int FrameCount => FrameRange.y;
    }
}