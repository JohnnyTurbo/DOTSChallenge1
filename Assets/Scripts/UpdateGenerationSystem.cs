using Unity.Burst;
using Unity.Entities;

namespace TMG.GameOfLife
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GameOfLifeSystem))]
    public partial struct SingleThreadUpdateGenerationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (isAlive, aliveNextGen) in SystemAPI.Query<EnabledRefRW<IsAlive>, EnabledRefRO<AliveNextGen>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                isAlive.ValueRW = aliveNextGen.ValueRO;
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MultiThreadGameOfLifeSystem))]
    public partial struct MultiThreadUpdateGenerationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new UpdateGenerationJob().ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct UpdateGenerationJob : IJobEntity
    {
        private void Execute(EnabledRefRW<IsAlive> isAlive, EnabledRefRO<AliveNextGen> aliveNextGen)
        {
            isAlive.ValueRW = aliveNextGen.ValueRO;
        }
    }
}