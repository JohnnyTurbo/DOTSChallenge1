using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace TMG.GameOfLife
{
    public partial struct SpawnPackedCellSystem : ISystem
    {
        private EntityArchetype PackedCellArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedGridProperties>();
            state.RequireForUpdate<PackedCell64>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var gridProperties = SystemAPI.GetSingleton<PackedGridProperties>();
            var random = Random.CreateFromIndex(777);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (packedCellBuffer, dataEntity) in SystemAPI.Query<DynamicBuffer<PackedCell64>>().WithEntityAccess())
            {
                for (var i = 0; i < packedCellBuffer.Length; i++)
                {
                    var packedPosition = new int2(i / gridProperties.GridSize.x, i % gridProperties.GridSize.x);
                    
                    var packedCell = packedCellBuffer[i];
                    for (var x = 0; x < 8; x++)
                    {
                        for (var y = 0; y < 8; y++)
                        {
                            var curPos = new int2(x, y);
                            var curIndex = (x * 8 + y);
                            // if (random.NextBool())
                            // {
                            //     Debug.Log($"i{i} Index: {curIndex} pos: {curPos}");
                            //     packedCell.Value |= (ulong)(1 << curIndex);
                            // }

                            var newRenderCell = ecb.Instantiate(gridProperties.CellPrefab);
                            ecb.SetComponent(newRenderCell, new PackedDataEntity
                            {
                                Entity = dataEntity,
                                IndexInBuffer = i,
                                IndexInElement = curIndex
                            });

                            var newTransform = LocalTransform.FromPosition(
                                (packedPosition.x * 8 + x) * gridProperties.CellSize,
                                (packedPosition.y * 8 + y) * gridProperties.CellSize, 0f);
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