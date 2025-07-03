using Unity.Entities;

/// <summary>
/// Composants o� les param�tres peuvent changer en temps r�el.
/// </summary>
public struct AnimationState : IComponentData
{

    public int playback;

    public float currentFramesDuration;
    public float currentAnimationDuration;

    public bool loop;
    public bool pause;
}

