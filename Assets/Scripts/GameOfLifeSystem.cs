using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnGridLinearSystem))]
    public partial struct GameOfLifeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var isAliveLookup = SystemAPI.GetComponentLookup<IsAlive>(true);
            var entityInfoLookup = SystemAPI.GetEntityStorageInfoLookup();
            var hits = 0;
            var misses = 0;
            
            foreach (var (neighborCells, aliveNext, entity) in SystemAPI.Query<DynamicBuffer<NeighborCells>, 
                         EnabledRefRW<AliveNextGen>>().WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var aliveNeighbors = 0;
                var curCellInfo = entityInfoLookup[entity];
                
                foreach (var neighborCell in neighborCells)
                {
                    var neighborInfo = entityInfoLookup[neighborCell.Value];
                    if (curCellInfo.Chunk == neighborInfo.Chunk)
                    {
                        hits++;
                    }
                    else
                    {
                        misses++;
                    }
                    
                    if (isAliveLookup.IsComponentEnabled(neighborCell.Value))
                    {
                        aliveNeighbors++;
                    }
                }

                if (isAliveLookup.IsComponentEnabled(entity))
                {
                    aliveNext.ValueRW = aliveNeighbors is >= 2 and <= 3;
                }
                else if (aliveNeighbors == 3)
                {
                    aliveNext.ValueRW = true;
                }
            }

            var hitrate = (float)hits / (hits + misses) * 100f;
            Debug.Log($"{hits} hits, {misses} misses - {hitrate}% Hitrate");
        }
    }
    
    // [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnGridLinearSystem))]
    public partial struct MultiThreadGameOfLifeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new GameOfLifeJob { IsAliveLookup = SystemAPI.GetComponentLookup<IsAlive>(true) }
                .ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct GameOfLifeJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<IsAlive> IsAliveLookup;
        
        private void Execute(EnabledRefRW<AliveNextGen> aliveNext, DynamicBuffer<NeighborCells> neighborCells,
            Entity entity)
        {
            var aliveNeighbors = 0;
            foreach (var neighborCell in neighborCells)
            {
                if (IsAliveLookup.IsComponentEnabled(neighborCell.Value))
                {
                    aliveNeighbors++;
                }
            }

            if (IsAliveLookup.IsComponentEnabled(entity))
            {
                aliveNext.ValueRW = aliveNeighbors is >= 2 and <= 3;
            }
            else if (aliveNeighbors == 3)
            {
                aliveNext.ValueRW = true;
            }
        }
    }
}