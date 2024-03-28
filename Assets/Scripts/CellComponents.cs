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

    public struct PackedGridProperties : IComponentData
    {
        public int2 GridSize;
        public float CellSize;
        public Entity CellPrefab;

        public int GridCount => GridSize.x * GridSize.y;
    }
    
    [InternalBufferCapacity(16)]
    public struct PackedCell64 : IBufferElementData
    {
        public ulong Value;
        public int2 Position;
    }

    public class MainCamera : IComponentData
    {
        public Camera Value;
    }
    
    public struct AliveNextGen : IComponentData, IEnableableComponent {}
    public struct IsAlive : IComponentData, IEnableableComponent {}

    [InternalBufferCapacity(8)]
    public struct NeighborCells : IBufferElementData
    {
        public Entity Value;
    }

    public struct DataEntity : IComponentData
    {
        public Entity Value;
    }

    public struct PackedDataEntity : IComponentData
    {
        public Entity Entity;
        public int IndexInBuffer;
        public int IndexInElement;
    }
}