using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{
    public struct SpriteAnimationTransitionBlobData
    {
        public float4 UVAtlas;
        public int2 GridSize;
        public int2 FrameRange;
        public BlobArray<float> FrameDurations;
        public float AnimationDuration;

        public bool loop;
        public bool pause;

        public bool redo_animation;

        public int FrameOffset => FrameRange.x;
        public int FrameCount => FrameRange.y;
    }
}