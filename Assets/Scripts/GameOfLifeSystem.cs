using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

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
            foreach (var (neighborCells, aliveNext, entity) in SystemAPI.Query<DynamicBuffer<NeighborCells>, 
                         EnabledRefRW<AliveNextGen>>().WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var aliveNeighbors = 0;
                foreach (var neighborCell in neighborCells)
                {
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
        }
    }

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