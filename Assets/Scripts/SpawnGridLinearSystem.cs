using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TMG.GameOfLife
{
    public partial struct SpawnGridLinearSystem : ISystem
    {
        private EntityArchetype _dataCellArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridProperties>();

            _dataCellArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<NeighborCells>(),
                ComponentType.ReadWrite<IsAlive>(), ComponentType.ReadWrite<DataEntity>());
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var mainCamera = Camera.main;
            
            var gridProperties = SystemAPI.GetSingleton<GridProperties>();
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var random = Random.CreateFromIndex(777);
            
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
                }
            }

            var gridCenter = (float2)gridProperties.GridSize * gridProperties.CellSize * 0.5f;
            mainCamera.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
            mainCamera.orthographicSize = gridProperties.GridSize.y * (gridProperties.CellSize + 0.1f) * 0.5f;
            
            ecb.Playback(state.EntityManager);
        }
    }
}