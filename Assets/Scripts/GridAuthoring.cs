using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class GridAuthoring : MonoBehaviour
    {
        public int2 GridSize;
        public float CellSize;
        public GameObject CellPrefab;
        
        public class GridBaker : Baker<GridAuthoring>
        {
            public override void Bake(GridAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GridProperties
                {
                    GridSize = authoring.GridSize,
                    CellSize = authoring.CellSize,
                    CellPrefab = GetEntity(authoring.CellPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}