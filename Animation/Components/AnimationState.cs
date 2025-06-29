using Unity.Entities;

public struct AnimationState : IComponentData
{
    public bool loop;
    public bool pause;
}

