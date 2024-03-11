using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TMG.GameOfLife
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct SpawnGridLinearSystem : ISystem
    {
        private EntityArchetype _dataCellArchetype;
        private NativeArray<int2> _neighborOffsets;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridProperties>();
            
            _dataCellArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<NeighborCells>(),
                ComponentType.ReadWrite<IsAlive>(), ComponentType.ReadWrite<AliveNextGen>());

            _neighborOffsets = new NativeArray<int2>(8, Allocator.Persistent);
            _neighborOffsets[0] = new int2(-1, -1);
            _neighborOffsets[1] = new int2(-1, 0);
            _neighborOffsets[2] = new int2(-1, 1);
            _neighborOffsets[3] = new int2(0, 1);
            _neighborOffsets[4] = new int2(1, 1);
            _neighborOffsets[5] = new int2(1, 0);
            _neighborOffsets[6] = new int2(1, -1);
            _neighborOffsets[7] = new int2(0, -1);
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var mainCamera = Camera.main;
            
            var gridProperties = SystemAPI.GetSingleton<GridProperties>();
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var random = Random.CreateFromIndex(777);
            var dataEntities = new NativeArray<Entity>(gridProperties.GridSize.x * gridProperties.GridSize.y, Allocator.Temp);
            
            for (var x = 0; x < gridProperties.GridSize.x; x++)
            {
                for (var y = 0; y < gridProperties.GridSize.y; y++)
                {
                    var newRenderCell = ecb.Instantiate(gridProperties.CellPrefab);
                    var newTransform = LocalTransform.FromPosition(x * gridProperties.CellSize, y * gridProperties.CellSize, 0f);
                    ecb.SetComponent(newRenderCell, newTransform);

                    var newDataCell = ecb.CreateEntity(_dataCellArchetype);
                    ecb.SetComponentEnabled<IsAlive>(newDataCell, random.NextBool());
                    ecb.SetComponent(newRenderCell, new DataEntity { Value = newDataCell });

                    var index = x * gridProperties.GridSize.y + y;
                    dataEntities[index] = newDataCell;
                }
            }

            for (var x = 0; x < gridProperties.GridSize.x; x++)
            {
                for (var y = 0; y < gridProperties.GridSize.y; y++)
                {
                    var curPosition = new int2(x, y);
                    var curIndex = x * gridProperties.GridSize.y + y;
                    var curDataEntity = dataEntities[curIndex];
                    
                    foreach (var neighborOffset in _neighborOffsets)
                    {
                        var neighborPosition = curPosition + neighborOffset;
                        if (neighborPosition.x < 0 || neighborPosition.x >= gridProperties.GridSize.x ||
                            neighborPosition.y < 0 || neighborPosition.y >= gridProperties.GridSize.y)
                        {
                            continue;
                        }

                        var neighborIndex = neighborPosition.x * gridProperties.GridSize.y + neighborPosition.y;
                        ecb.AppendToBuffer(curDataEntity, new NeighborCells { Value = dataEntities[neighborIndex] });
                    }
                }
            }

            var gridCenter = (float2)gridProperties.GridSize * gridProperties.CellSize * 0.5f;
            mainCamera.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
            mainCamera.orthographicSize = gridProperties.GridSize.y * (gridProperties.CellSize + 0.1f) * 0.5f;
            
            ecb.Playback(state.EntityManager);
        }
    }
}