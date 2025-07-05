using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Composants où les paramètres peuvent changer en temps réel.
/// </summary>
public struct AnimationState : IComponentData
{

    public int Playback;

    public float CurrentFramesDuration;
    public float CurrentAnimationDuration;

    public int AnimationIndex;
    public FixedString64Bytes AnimationName;

    public int FrameIndex;
    public double Time;

    public bool Loop;
    public bool Pause;
}

