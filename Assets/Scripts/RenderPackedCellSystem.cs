using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TMG.GameOfLife
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PackedGameOfLifeSystem))]
    public partial struct RenderPackedCellSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell64>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cellLookup = SystemAPI.GetBufferLookup<PackedCell64>();
            
            foreach (var (color, dataEntity) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, PackedDataEntity>())
            {
                var cellBytes = cellLookup[dataEntity.Entity].ElementAt(dataEntity.IndexInBuffer).Value;
                var cellBit = (ulong)1 << dataEntity.IndexInElement;
                var isAlive = (cellBit & cellBytes) != 0;
                
                color.ValueRW.Value = isAlive ? new float4(0, 1, 0, 1) : new float4(0.25f, 0.25f, 0.25f, 1f);
            }
        }
    }
    
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PackedMultiThreadGameOfLifeSystem))]
    public partial struct RenderMultiThreadPackedCellSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell64>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new RenderMultiThreadPackedCellJob
            {
                CellLookup = SystemAPI.GetBufferLookup<PackedCell64>(true)
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct RenderMultiThreadPackedCellJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<PackedCell64> CellLookup;
        
        private void Execute(ref URPMaterialPropertyBaseColor color, in PackedDataEntity dataEntity)
        {
            var buffer = CellLookup[dataEntity.Entity];
            var cellBytes = buffer[dataEntity.IndexInBuffer].Value;
            var cellBit = (ulong)1 << dataEntity.IndexInElement;
            var isAlive = (cellBit & cellBytes) != 0;
                
            color.Value = isAlive ? new float4(0, 1, 0, 1) : new float4(0.25f, 0.25f, 0.25f, 1f);
        }
    }
}