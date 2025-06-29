using Unity.Entities;

public struct AnimationState : IComponentData
{
    public bool hasRootAnimationFinished;
    public bool loop;
    public bool pause;
}

