using Unity.Collections;
using Unity.Entities;

public struct AnimationReference : IComponentData
{
    public int index;
    public FixedString64Bytes animationName;
}