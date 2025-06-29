using Unity.Mathematics;

namespace NSprites
{
    public struct SpriteAnimationTransitionBlobData
    {
        public float4 UVAtlas;
        public int2 GridSize;
        public int2 FrameRange;
        public float FramesDuration;

        public bool loop;
        public bool pause;

        public bool redo_animation;

        public int FrameOffset => FrameRange.x;
        public int FrameCount => FrameRange.y;
    }
}