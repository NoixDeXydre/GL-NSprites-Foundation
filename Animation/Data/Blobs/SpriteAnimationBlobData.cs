using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{
    public struct SpriteAnimationBlobData
    {
        public int ID;
        public float4 UVAtlas;
        public int2 GridSize;
        public int2 FrameRange;
        public BlobArray<float> FrameDurations;
        public float AnimationDuration;

        public bool loop;
        public bool pause;

        public int FrameOffset => FrameRange.x;
        public int FrameCount => FrameRange.y;

        // Transitions possibles d'une animation
        public BlobAssetReference<BlobArray<SpriteAnimationTransitionBlobData>> AnimationTransitions;
    }
}