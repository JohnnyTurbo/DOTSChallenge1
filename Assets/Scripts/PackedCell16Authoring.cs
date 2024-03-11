using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class PackedCell16Authoring : MonoBehaviour
    {
        public class PackedCell16Baker : Baker<PackedCell16Authoring>
        {
            public override void Bake(PackedCell16Authoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PackedCell16>(entity);
            }
        }
    }
}