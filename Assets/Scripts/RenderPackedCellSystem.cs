using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TMG.GameOfLife
{
    public partial struct RenderPackedCellSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell16>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var packedCells = SystemAPI.GetSingleton<PackedCell16>();
            foreach (var (color, dataEntity) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, PackedDataEntity>())
            {
                var cellBit = (ushort)(1 << dataEntity.Index);
                var isAlive = (cellBit & packedCells.Value) != 0;
                
                if (isAlive)
                {
                    color.ValueRW.Value = new float4(0, 1, 0, 1);
                }
                else
                {
                    color.ValueRW.Value = new float4(0.25f, 0.25f, 0.25f, 1f);
                }
            }
        }
    }
}