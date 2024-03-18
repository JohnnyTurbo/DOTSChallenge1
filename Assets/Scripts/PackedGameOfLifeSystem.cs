using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public partial struct PackedGameOfLifeSystem : ISystem
    {
        private NativeArray<int> _bitOffsets;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell16>();
            // ushort layout
            // 00 04 08 12
            // 01 05 09 13
            // 02 06 10 14
            // 03 07 11 15
            
            // Bit offsets
            // -5, -4, -3, -1, +1, +3, +4, +5
            _bitOffsets = new NativeArray<int>(8, Allocator.Persistent);
            _bitOffsets[0] = -5;
            _bitOffsets[1] = -4;
            _bitOffsets[2] = -3;
            _bitOffsets[3] = -1;
            _bitOffsets[4] = 1;
            _bitOffsets[5] = 3;
            _bitOffsets[6] = 4;
            _bitOffsets[7] = 5;

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var packedCells = SystemAPI.GetSingleton<PackedCell16>().Value;
            ushort nextCells = 0;

            for (var i = 0; i < 16; i++)
            {
                var aliveCount = 0;
                var isAlive = (packedCells | 1 << i) != 0;
                foreach (var bitOffset in _bitOffsets)
                {
                    var curOffset = i + bitOffset;
                    if(curOffset is < 0 or > 15) continue;
                    if ((packedCells & 1 << curOffset) != 0)
                    {
                        aliveCount++;
                    }
                }

                if (isAlive)
                {
                    if (aliveCount is 2 or 3)
                    {
                        nextCells = (ushort)(nextCells | 1 << i);
                    }
                }
                else
                {
                    if (aliveCount is 3)
                    {
                        nextCells = (ushort)(nextCells | 1 << i);
                    }
                }
            }
            
            SystemAPI.SetSingleton(new PackedCell16{Value = nextCells});
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}