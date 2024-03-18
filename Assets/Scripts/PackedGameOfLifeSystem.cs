using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TMG.GameOfLife
{
    // [DisableAutoCreation]
    public partial struct PackedGameOfLifeSystem : ISystem
    {
        private NativeArray<int> _bitOffsets;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell64>();
            
            // ulong layout
            // 00 08 16 24 32 40 48 56
            // 01 09 17 25 33 41 49 57
            // 02 10 18 26 34 42 50 58
            // 03 11 19 27 35 43 51 59
            // 04 12 20 28 36 44 52 60
            // 05 13 21 29 37 45 53 61
            // 06 14 22 30 38 46 54 62
            // 07 15 23 31 39 47 55 63
            
            // Bit offsets
            // -9, -8, -7, -1, +1, +7, +8, +9
            _bitOffsets = new NativeArray<int>(8, Allocator.Persistent);
            _bitOffsets[0] = -9;
            _bitOffsets[1] = -8;
            _bitOffsets[2] = -7;
            _bitOffsets[3] = -1;
            _bitOffsets[4] = 1;
            _bitOffsets[5] = 7;
            _bitOffsets[6] = 8;
            _bitOffsets[7] = 9;

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var nextCellList = new NativeList<ulong>(8, state.WorldUpdateAllocator);
            
            foreach (var packedCellBuffer in SystemAPI.Query<DynamicBuffer<PackedCell64>>())
            {
                var nextCells = (ulong)0;
                for (var bufferIndex = 0; bufferIndex < packedCellBuffer.Length; bufferIndex++)
                {
                    var packedCells = packedCellBuffer[bufferIndex].Value;
                    
                    for (var i = 0; i < 64; i++)
                    {
                        var aliveCount = 0;
                        var isAlive = (packedCells | (ulong)1 << i) != 0;
                        foreach (var bitOffset in _bitOffsets)
                        {
                            var curOffset = i + bitOffset;
                            if (curOffset is < 0 or > 63) continue;
                            if ((packedCells & (ulong)1 << curOffset) != 0)
                            {
                                aliveCount++;
                            }
                        }

                        if (isAlive)
                        {
                            if (aliveCount is 2 or 3)
                            {
                                nextCells |= (ulong)1 << i;
                            }
                        }
                        else
                        {
                            if (aliveCount is 3)
                            {
                                nextCells |= (ulong)1 << i;
                            }
                        }
                    }
                }

                nextCellList.Add(nextCells);
            }

            var index = 0;
            foreach (var packedCells in SystemAPI.Query<DynamicBuffer<PackedCell64>>())
            {
                packedCells.ElementAt(index).Value = nextCellList[index];
                index++;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}