using Unity.Entities;

namespace NSprites
{
    public struct AnimationPlaybackType : IComponentData
    {
        public int forward;
        public int backward;
    }
}
