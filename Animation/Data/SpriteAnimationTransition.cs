using UnityEngine;

[CreateAssetMenu(fileName = "NewNSpriteAnimationTransition", menuName = "NSprites/Transition")]
public class SpriteAnimationTransition : ScriptableObject
{

    [Tooltip("Animation de transition")]
    public SpriteAnimation spriteAnimation;

    [Tooltip("Si activé, la première animation est relancé quand la termine")]
    public bool retourAnimationRacine;
}