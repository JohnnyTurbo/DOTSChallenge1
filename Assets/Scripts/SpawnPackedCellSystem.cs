using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
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

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var gridProperties = SystemAPI.GetSingleton<PackedGridProperties>();
            var random = Random.CreateFromIndex(777);
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var cellBufferQuery = SystemAPI.QueryBuilder().WithAll<PackedCell64>().Build();
            var cellBuffers = cellBufferQuery.ToEntityArray(state.WorldUpdateAllocator);
            var cellBufferIndex = 0;
            var curIndexInBuffer = 0;

            var curCellBuffer = cellBuffers[cellBufferIndex];
            
            for (var gridX = 0; gridX < gridProperties.GridSize.x; gridX++)
            {
                for (var gridY = 0; gridY < gridProperties.GridSize.y; gridY++)
                {
                    if (curIndexInBuffer >= 16)
                    {
                        curIndexInBuffer = 0;
                        cellBufferIndex++;
                        curCellBuffer = cellBuffers[cellBufferIndex];
                    }

                    var packedBuffer = SystemAPI.GetBuffer<PackedCell64>(curCellBuffer);
                    var packedElement = packedBuffer.ElementAt(curIndexInBuffer);
                    packedElement.Position = new int2(gridX, gridY);
                    packedBuffer.ElementAt(curIndexInBuffer) = packedElement;
                    
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
                                Entity = curCellBuffer,
                                IndexInBuffer = curIndexInBuffer,
                                IndexInElement = curIndex
                            });

                            var newTransform = LocalTransform.FromPosition(
                                (gridX * 8 + x) * gridProperties.CellSize,
                                (gridY * 8 + y) * gridProperties.CellSize, 0f);
                            ecb.SetComponent(newRenderCell, newTransform);
                        }
                    }
                    curIndexInBuffer++;
                }
            }

            ecb.Playback(state.EntityManager);

            var gridCenter = (float2)gridProperties.GridSize * 8 * gridProperties.CellSize * 0.5f;
            Camera.main.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
            Camera.main.orthographicSize = gridProperties.GridSize.y * 8 * (gridProperties.CellSize + 0.1f) * 0.5f;
        }
    }
}