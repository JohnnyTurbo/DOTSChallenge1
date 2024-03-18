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
            state.RequireForUpdate<PackedCell64>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            // var packedCells = SystemAPI.GetSingletonRW<PackedCell16>();
            var gridProperties = SystemAPI.GetSingleton<GridProperties>();
            var random = Random.CreateFromIndex(777);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (packedCellBuffer, dataEntity) in SystemAPI.Query<DynamicBuffer<PackedCell64>>().WithEntityAccess())
            {
                for (var i = 0; i < packedCellBuffer.Length; i++)
                {
                    var packedCell = packedCellBuffer[i];
                    for (var x = 0; x < 8; x++)
                    {
                        for (var y = 0; y < 8; y++)
                        {
                            var curPos = new int2(x, y);
                            var curIndex = x * gridProperties.GridSize.y + y;
                            if (random.NextBool())
                            {
                                packedCell.Value |= (uint)(1 << curIndex);
                            }

                            var newRenderCell = ecb.Instantiate(gridProperties.CellPrefab);
                            ecb.SetComponent(newRenderCell, new PackedDataEntity
                            {
                                Entity = dataEntity,
                                IndexInBuffer = i,
                                IndexInElement = curIndex
                            });

                            var newTransform = LocalTransform.FromPosition((i*8+x) * gridProperties.CellSize,
                                y * gridProperties.CellSize, 0f);
                            ecb.SetComponent(newRenderCell, newTransform);
                        }
                    }

                    packedCellBuffer.ElementAt(i) = packedCell;
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}