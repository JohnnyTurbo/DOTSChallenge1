using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TMG.GameOfLife
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct SingleThreadSetRenderEntitySystem : ISystem
    {
        private float4 _aliveColor;
        private float4 _deadColor;

        public void OnCreate(ref SystemState state)
        {
            _aliveColor = new float4(0, 1, 0, 1);
            _deadColor = new float4(0.25f, 0.25f, 0.25f, 1f);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var aliveLookup = SystemAPI.GetComponentLookup<IsAlive>();
            
            foreach (var (color, dataEntity) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, DataEntity>())
            {
                color.ValueRW.Value = aliveLookup.IsComponentEnabled(dataEntity.Value) ? _aliveColor : _deadColor;
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct MultiThreadSetRenderEntitySystem : ISystem
    {
        /*private float4 _aliveColor;
        private float4 _deadColor;

        public void OnCreate(ref SystemState state)
        {
            _aliveColor = new float4(0, 1, 0, 1);
            _deadColor = new float4(0.25f, 0.25f, 0.25f, 1f);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SetRenderEntityJob
            {
                AliveLookup = SystemAPI.GetComponentLookup<IsAlive>(),
                AliveColor = _aliveColor,
                DeadColor = _deadColor
            }.ScheduleParallel(state.Dependency);
        }*/
    }

    // [BurstCompile]
    // public partial struct SetRenderEntityJob : IJobEntity
    // {
        /*[ReadOnly] public ComponentLookup<IsAlive> AliveLookup;
        [ReadOnly] public float4 AliveColor;
        [ReadOnly] public float4 DeadColor;

        private void Execute(ref URPMaterialPropertyBaseColor color, in DataEntity dataEntity)
        {
            color.Value = AliveLookup.IsComponentEnabled(dataEntity.Value) ? AliveColor : DeadColor;
        }*/
    // }
}