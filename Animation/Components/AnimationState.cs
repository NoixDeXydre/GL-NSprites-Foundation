using Unity.Entities;

/// <summary>
/// Composants où les paramètres peuvent changer en temps réel.
/// </summary>
public struct AnimationState : IComponentData
{

    public int playback;

    public float currentFramesDuration;
    public float currentAnimationDuration;

    public bool loop;
    public bool pause;
}

