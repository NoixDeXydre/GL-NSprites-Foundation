using NSprites;
using Unity.Transforms;

[assembly: InstancedPropertyComponent(typeof(UVAtlas), "_uvAtlasBuffer")]
[assembly: InstancedPropertyComponent(typeof(LocalToWorld), "_positionBuffer")]
[assembly: InstancedPropertyComponent(typeof(Scale2D), "_heightWidthBuffer")]
[assembly: InstancedPropertyComponent(typeof(Pivot), "_pivotBuffer")]
[assembly: InstancedPropertyComponent(typeof(SortingData), "_sortingDataBuffer")]
[assembly: InstancedPropertyComponent(typeof(ColorRGBA), "_colorBuffer")]