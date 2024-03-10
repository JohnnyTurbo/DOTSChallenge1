using Unity.Burst;
using Unity.Entities;

namespace TMG.GameOfLife
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GameOfLifeSystem))]
    public partial struct UpdateGenerationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (isAlive, aliveNextGen) in SystemAPI.Query<EnabledRefRW<IsAlive>, EnabledRefRO<AliveNextGen>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                isAlive.ValueRW = aliveNextGen.ValueRO;
            }
        }
    }
}