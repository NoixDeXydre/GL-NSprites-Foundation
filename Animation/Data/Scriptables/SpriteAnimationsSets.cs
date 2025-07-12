using System;
using System.Collections.Generic;
using UnityEngine;

namespace NSprites
{
    [CreateAssetMenu(fileName = "NewNSpriteAnimationsSet", menuName = "NSprites/AnimationsSets")]
    public class SpriteAnimationsSets : ScriptableObject
    {

        [Serializable]
        public struct SingleAnimationSet
        {
            public string Tag;
            public SpriteAnimation[] Animations;
        }

        [SerializeField]
        private SingleAnimationSet[] _animationsSets;

        public IReadOnlyCollection<SingleAnimationSet> AnimationsSets => _animationsSets;
        
        // returns true if all animation's sprites have the same texture as passed in
        public bool IsValid(Texture2D mainTexture)
        {

            var passed = true;
            foreach(var set in _animationsSets)
            {

                foreach (var animation in set.Animations)
                {
                    var texture = animation.SpriteSheet.texture;

                    if (texture == null)
                    {
                        Debug.LogException(new NSpritesException($"{nameof(SpriteAnimationsSets)} {name}: all {set.Tag}'s {nameof(SpriteAnimation.SpriteSheet)} must be not null"), this);
                        passed = false;
                    }
                    else if (animation.SpriteSheet.texture != mainTexture)
                    {
                        Debug.LogException(new NSpritesException($"{nameof(SpriteAnimationsSets)} {name}: all {nameof(set.Tag)}'s {nameof(SpriteAnimation.SpriteSheet)} must have same texture as passed \"{mainTexture.name}\", but animation \"{animation.name}\" have {nameof(SpriteAnimation.SpriteSheet)}'s texture \"{animation.SpriteSheet.name}\""), this);
                        passed = false;
                    }
                }
            }

            return passed;
        }
    }
}