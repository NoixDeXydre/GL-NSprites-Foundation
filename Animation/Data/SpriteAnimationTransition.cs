using UnityEngine;

[CreateAssetMenu(fileName = "NewNSpriteAnimationTransition", menuName = "NSprites/Transition")]
public class SpriteAnimationTransition : ScriptableObject
{

    [Tooltip("Animation de transition")]
    public SpriteAnimation spriteAnimation;
}