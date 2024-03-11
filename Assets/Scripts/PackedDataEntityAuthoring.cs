using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class PackedDataEntityAuthoring : MonoBehaviour
    {
        public class PackedDataEntityBaker : Baker<PackedDataEntityAuthoring>
        {
            public override void Bake(PackedDataEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PackedDataEntity>(entity);
            }
        }
    }
}