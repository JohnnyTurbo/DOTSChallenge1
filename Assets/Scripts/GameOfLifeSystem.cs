using Unity.Burst;
using Unity.Entities;

namespace TMG.GameOfLife
{
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
}