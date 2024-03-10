using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TMG.GameOfLife
{
    public partial struct SetRenderEntitySystem : ISystem
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
}