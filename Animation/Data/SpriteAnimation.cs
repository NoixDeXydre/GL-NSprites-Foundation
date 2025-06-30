using System;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNSpriteAnimation", menuName = "NSprites/Animation (frame sequence)")]
public class SpriteAnimation : ScriptableObject
{
    [Serializable]
    public struct FrameRangeData
    {
        public int Offset;
        public int Count;

        public bool IsDefault => Offset == 0 && Count == 0;
        
        public static implicit operator int2(FrameRangeData range) => new(range.Offset, range.Count);
    }

    // Sprite here required because whe want to know UV of animation frame sequence on atlas
    [Tooltip("Texture représentant l'ensemble des animations")]
    public Sprite SpriteSheet;

    [Tooltip("Set this value to frame count (rows and cols) of the whole texture even if it has multiple animations. It used to calculate UVs.")]
    public int2 FrameCount = new(1);

    [Tooltip("Use this field to select frame sequence (by indexes) if your sprite sheet contains multiple animations. X for offset, Y for count. (0, 0) value takes all texture as default.")]
    public FrameRangeData FrameRange;

    [Tooltip("Durée de chaque frames en secondes")]
    public float FramesDuration = 0f;

    [Tooltip("Défini si l'animation doit boucler après sa dernière trame")]
    public bool animationABoucler;

    [Tooltip("Défini si l'animation est en pause ou non")]
    public bool animationEnPause;

    #region Editor
#if UNITY_EDITOR
    private const float DefaultFrameDuration = .1f;

    private void OnValidate()
    {
        CorrectFrameCount();
        var frameCount = FrameCount.x * FrameCount.y;
        CorrectFrameRange(frameCount);
    }

    private void CorrectFrameCount() 
        => FrameCount = new int2(math.max(0, FrameCount.x), math.max(0, FrameCount.y));

    private void CorrectFrameRange(in int frameCount)
    {
        FrameRange.Offset = math.clamp(FrameRange.Offset,0, frameCount - 1);
        FrameRange.Count = math.clamp(FrameRange.Count,0, frameCount - FrameRange.Offset);
    }
#endif
    #endregion
}