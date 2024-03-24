using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class PackedCell64Authoring : MonoBehaviour
    {
        public int2 GridSize;
        public float CellSize;
        public GameObject CellPrefab;
        public ulong StartingValue;
        
        private int PackedCellCount => GridSize.x * GridSize.y;
        
        private class PackedCell64Baker : Baker<PackedCell64Authoring>
        {
            public override void Bake(PackedCell64Authoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PackedGridProperties
                {
                    GridSize = authoring.GridSize,
                    CellSize = authoring.CellSize,
                    CellPrefab = GetEntity(authoring.CellPrefab, TransformUsageFlags.Dynamic)
                });
                
                var buffer = AddBuffer<PackedCell64>(entity);
                for (var i = 0; i < authoring.PackedCellCount; i++)
                {
                    buffer.Add(new PackedCell64 { Value = authoring.StartingValue });
                }
            }
        }
    }
}