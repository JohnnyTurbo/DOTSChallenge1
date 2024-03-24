using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TMG.GameOfLife
{
    // [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnPackedCellSystem))]
    public partial struct PackedGameOfLifeSystem : ISystem
    {
        private int shouldRun;

        private NativeArray<ulong> _bitMasks;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedCell64>();
            shouldRun = 2;
            // ulong layout
            // 07 15 23 31 39 47 55 63
            // 06 14 22 30 38 46 54 62
            // 05 13 21 29 37 45 53 61
            // 04 12 20 28 36 44 52 60
            // 03 11 19 27 35 43 51 59
            // 02 10 18 26 34 42 50 58
            // 01 09 17 25 33 41 49 57
            // 00 08 16 24 32 40 48 56

            GenerateBitMasks();
        }

        private void GenerateBitMasks()
        {
            _bitMasks = new NativeArray<ulong>(64, Allocator.Persistent);
            
            var neighborOffsets = new NativeArray<int2>(8, Allocator.Persistent);
            neighborOffsets[0] = new int2(-1, -1);
            neighborOffsets[1] = new int2(-1, 0);
            neighborOffsets[2] = new int2(-1, 1);
            neighborOffsets[3] = new int2(0, 1);
            neighborOffsets[4] = new int2(1, 1);
            neighborOffsets[5] = new int2(1, 0);
            neighborOffsets[6] = new int2(1, -1);
            neighborOffsets[7] = new int2(0, -1);
            
            var i = 0;
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var curPos = new int2(x, y);
                    ulong bitmask = 0;
                    foreach (var neighborOffset in neighborOffsets)
                    {
                        var neighborPos = curPos + neighborOffset;
                        if (neighborPos.x < 0 || neighborPos.x > 7 || neighborPos.y < 0 || neighborPos.y > 7) continue;
                        var neighborIndex = neighborPos.x * 8 + neighborPos.y;
                        bitmask |= (ulong)1 << neighborIndex;
                    }
                    
                    _bitMasks[i] = bitmask;
                    i++;
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (shouldRun > 0)
            {
                shouldRun--;
                return;
            }
            
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
                        var isAlive = (packedCells & (ulong)1 << i) != 0;

                        var aliveNeighbors = packedCells & _bitMasks[i];
                        ulong v = 1;
                        for (var j = 0; j < 64; j++)
                        {
                            if ((v & aliveNeighbors) != 0)
                            {
                                aliveCount++;
                            }
                            v <<= 1;
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