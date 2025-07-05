using CRL.BlobHashMaps;
using Unity.Collections;
using Unity.Entities;

namespace NSprites
{
    public struct IndexedAnimationsName : IComponentData
    {
        public BlobAssetReference<BlobHashMap<FixedString64Bytes, int>> IndexedAnimationsNameCollection;
    }
}
