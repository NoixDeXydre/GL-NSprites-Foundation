using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Composants où les paramètres peuvent changer en temps réel.
/// </summary>
public struct AnimationState : IComponentData
{

    public int playback;

    public float currentFramesDuration;
    public float currentAnimationDuration;

    public int animationIndex;
    public FixedString64Bytes animationName;

    public int frameIndex;
    public double time;

    public bool loop;
    public bool pause;
}

