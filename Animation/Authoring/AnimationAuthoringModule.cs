using System;
using Unity.Entities;
using UnityEngine;

namespace NSprites.Authoring
{

    [Serializable]
    public struct AnimationAuthoringModule
    {

        [SerializeField] public SpriteAnimationsSets AnimationsSets;
        [SerializeField] public string InitialAnimationName;

        public SpriteAnimation GetInitialData()
        {

            foreach (var set in AnimationsSets.AnimationsSets)
            {
                foreach (var animation in set.Animations)
                {
                    if (animation.nomAnimation.Equals(InitialAnimationName))
                        return animation;
                }
            }

            return null;
        }

        public void Bake<TAuthoring>(Baker<TAuthoring> baker)
            where TAuthoring : MonoBehaviour
            => baker.BakeAnimation(baker.GetEntity(TransformUsageFlags.None), AnimationsSets, InitialAnimationName);
    }
}