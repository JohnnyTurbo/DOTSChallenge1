using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TMG.GameOfLife
{
    public struct GridProperties : IComponentData
    {
        public int2 GridSize;
        public float CellSize;
        public Entity CellPrefab;
    }

    public class MainCamera : IComponentData
    {
        public Camera Value;
    }
    
    public struct IsAlive : IComponentData, IEnableableComponent {}

    [InternalBufferCapacity(8)]
    public struct NeighborCells : IBufferElementData
    {
        public Entity Value;
    }
}