using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class PackedCell64Authoring : MonoBehaviour
    {
        public int count;
        
        private class PackedCell64Baker : Baker<PackedCell64Authoring>
        {
            public override void Bake(PackedCell64Authoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<PackedCell64>(entity);
                for (var i = 0; i < authoring.count; i++)
                {
                    buffer.Add(new PackedCell64 { Value = 0 });
                }
            }
        }
    }
}