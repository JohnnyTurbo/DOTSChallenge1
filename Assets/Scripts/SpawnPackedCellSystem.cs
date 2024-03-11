using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TMG.GameOfLife
{
    public partial struct SpawnPackedCellSystem : ISystem
    {
        private EntityArchetype PackedCellArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridProperties>();
            state.RequireForUpdate<PackedCell16>();
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var packedCells = SystemAPI.GetSingletonRW<PackedCell16>();
            var gridProperties = SystemAPI.GetSingleton<GridProperties>();
            var random = Random.CreateFromIndex(777);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            for (var x = 0; x < 4; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    var curPos = new int2(x, y);
                    var curIndex = x * gridProperties.GridSize.y + y;
                    if (random.NextBool())
                    {
                        packedCells.ValueRW.Value = (ushort)(packedCells.ValueRO.Value | (1 << curIndex));
                    }

                    var newRenderCell = ecb.Instantiate(gridProperties.CellPrefab);
                    ecb.SetComponent(newRenderCell, new PackedDataEntity
                    {
                        Entity = Entity.Null,
                        Index = curIndex
                    });
                    
                    var newTransform = LocalTransform.FromPosition(x * gridProperties.CellSize, y * gridProperties.CellSize, 0f);
                    ecb.SetComponent(newRenderCell, newTransform);
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}