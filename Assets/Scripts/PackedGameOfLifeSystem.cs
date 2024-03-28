using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TMG.GameOfLife
{
    public struct EdgeOffsets
    {
        public int Offset1;
        public int Offset2;
        public int Offset3;
    }
    
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SpawnPackedCellSystem))]
    public partial struct PackedGameOfLifeSystem : ISystem
    {
        private int shouldRun;

        private NativeArray<ulong> _bitMasks;
        private NativeArray<int2> _neighborOffsets;
        private NativeArray<EdgeOffsets> _edgeOffsets;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PackedGridProperties>();
            state.RequireForUpdate<CellLookupComponent>();
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
            
            // Neighboring layout
            // 56   00 08 16 24 32 40 48 56   00
            //    #                         #   
            // 63   07 15 23 31 39 47 55 63   07
            // 62   06 14 22 30 38 46 54 62   06
            // 61   05 13 21 29 37 45 53 61   05
            // 60   04 12 20 28 36 44 52 60   04
            // 59   03 11 19 27 35 43 51 59   03
            // 58   02 10 18 26 34 42 50 58   02
            // 57   01 09 17 25 33 41 49 57   01
            // 56   00 08 16 24 32 40 48 56   00
            //    #                         #
            // 63   07 15 23 31 39 47 55 63   07

            GenerateBitMasks();
        }

        private void GenerateBitMasks()
        {
            _bitMasks = new NativeArray<ulong>(64, Allocator.Persistent);
            
            _neighborOffsets = new NativeArray<int2>(8, Allocator.Persistent);
            _neighborOffsets[0] = new int2(-1, -1);
            _neighborOffsets[1] = new int2(-1, 0);
            _neighborOffsets[2] = new int2(-1, 1);
            _neighborOffsets[3] = new int2(0, 1);
            _neighborOffsets[4] = new int2(1, 1);
            _neighborOffsets[5] = new int2(1, 0);
            _neighborOffsets[6] = new int2(1, -1);
            _neighborOffsets[7] = new int2(0, -1);

            _edgeOffsets = new NativeArray<EdgeOffsets>(8, Allocator.Persistent);
            _edgeOffsets[0] = new EdgeOffsets
            {
                Offset1 = 63,
                Offset2 = 1000,
                Offset3 = 1000
            };
            _edgeOffsets[1] = new EdgeOffsets
            {
                Offset1 = 55,
                Offset2 = 56,
                Offset3 = 57
            };
            _edgeOffsets[2] = new EdgeOffsets
            {
                Offset1 = 49,
                Offset2 = 1000,
                Offset3 = 1000
            };
            _edgeOffsets[3] = new EdgeOffsets
            {
                Offset1 = -15,
                Offset2 = -7,
                Offset3 = 1
            };
            _edgeOffsets[4] = new EdgeOffsets
            {
                Offset1 = -63,
                Offset2 = 1000,
                Offset3 = 1000
            };
            _edgeOffsets[5] = new EdgeOffsets
            {
                Offset1 = -57,
                Offset2 = -56,
                Offset3 = -55
            };
            _edgeOffsets[6] = new EdgeOffsets
            {
                Offset1 = -49,
                Offset2 = 1000,
                Offset3 = 1000
            };
            _edgeOffsets[7] = new EdgeOffsets
            {
                Offset1 = -1,
                Offset2 = 7,
                Offset3 = 15
            };
            
            var i = 0;
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var curPos = new int2(x, y);
                    ulong bitmask = 0;
                    foreach (var neighborOffset in _neighborOffsets)
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

            var gridProperties = SystemAPI.GetSingleton<PackedGridProperties>();
            var nextCellArray = new NativeArray<ulong>(gridProperties.GridCount, Allocator.Temp);
            
            var cellLookupComponent = SystemAPI.GetSingleton<CellLookupComponent>().Value;
            ref var cellLookup = ref cellLookupComponent.Value.Cells;
            
            foreach (var (packedCellBuffer, entity) in SystemAPI.Query<DynamicBuffer<PackedCell64>>().WithEntityAccess())
            {
                for (var bufferIndex = 0; bufferIndex < packedCellBuffer.Length; bufferIndex++)
                {
                    var nextCells = (ulong)0;
                    var packedCells = packedCellBuffer[bufferIndex].Value;
                    var cellPosition = packedCellBuffer[bufferIndex].Position;
                    
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

                        if (i == 0)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[7], out var downIndex))
                            {
                                var downCellInfo = cellLookup[downIndex];
                                var neighborValue = entity.Equals(downCellInfo.Entity)
                                    ? packedCellBuffer[downCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(downCellInfo.Entity)
                                        .ElementAt(downCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[7]);
                            }
                            
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[0], out var downLeftIndex))
                            {
                                var downLeftCellInfo = cellLookup[downLeftIndex];
                                var neighborValue = entity.Equals(downLeftCellInfo.Entity)
                                    ? packedCellBuffer[downLeftCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(downLeftCellInfo.Entity)
                                        .ElementAt(downLeftCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[0]);
                            }
                            
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[1], out var leftIndex))
                            {
                                var leftCellInfo = cellLookup[leftIndex];
                                var neighborValue = entity.Equals(leftCellInfo.Entity)
                                    ? packedCellBuffer[leftCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(leftCellInfo.Entity)
                                        .ElementAt(leftCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[1]);
                            }
                        }
                        else if (i == 7)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[1], out var leftIndex))
                            {
                                var leftCellInfo = cellLookup[leftIndex];
                                var neighborValue = entity.Equals(leftCellInfo.Entity)
                                    ? packedCellBuffer[leftCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(leftCellInfo.Entity)
                                        .ElementAt(leftCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[1]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[2], out var upLeftIndex))
                            {
                                var upLeftCellInfo = cellLookup[upLeftIndex];
                                var neighborValue = entity.Equals(upLeftCellInfo.Entity)
                                    ? packedCellBuffer[upLeftCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(upLeftCellInfo.Entity)
                                        .ElementAt(upLeftCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[2]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[3], out var upIndex))
                            {
                                var upCellInfo = cellLookup[upIndex];
                                var neighborValue = entity.Equals(upCellInfo.Entity)
                                    ? packedCellBuffer[upCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(upCellInfo.Entity)
                                        .ElementAt(upCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[3]);
                            }
                        }
                        else if (i == 56)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[5], out var rightIndex))
                            {
                                var rightCellInfo = cellLookup[rightIndex];
                                var neighborValue = entity.Equals(rightCellInfo.Entity)
                                    ? packedCellBuffer[rightCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(rightCellInfo.Entity)
                                        .ElementAt(rightCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[5]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[6], out var downRightIndex))
                            {
                                var downRightCellInfo = cellLookup[downRightIndex];
                                var neighborValue = entity.Equals(downRightCellInfo.Entity)
                                    ? packedCellBuffer[downRightCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(downRightCellInfo.Entity)
                                        .ElementAt(downRightCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[6]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[7], out var downIndex))
                            {
                                var downCellInfo = cellLookup[downIndex];
                                var neighborValue = entity.Equals(downCellInfo.Entity)
                                    ? packedCellBuffer[downCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(downCellInfo.Entity)
                                        .ElementAt(downCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[7]);
                            }
                        }
                        else if (i == 63)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[3], out var upIndex))
                            {
                                var upCellInfo = cellLookup[upIndex];
                                var neighborValue = entity.Equals(upCellInfo.Entity)
                                    ? packedCellBuffer[upCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(upCellInfo.Entity)
                                        .ElementAt(upCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[3]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[4], out var upRightIndex))
                            {
                                var upRightCellInfo = cellLookup[upRightIndex];
                                var neighborValue = entity.Equals(upRightCellInfo.Entity)
                                    ? packedCellBuffer[upRightCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(upRightCellInfo.Entity)
                                        .ElementAt(upRightCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[4]);
                            }

                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[5], out var rightIndex))
                            {
                                var rightCellInfo = cellLookup[rightIndex];
                                var neighborValue = entity.Equals(rightCellInfo.Entity)
                                    ? packedCellBuffer[rightCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(rightCellInfo.Entity)
                                        .ElementAt(rightCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[5]);
                            }
                        }
                        else if (i is > 0 and < 7)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[1], out var leftIndex))
                            {
                                var leftCellInfo = cellLookup[leftIndex];
                                var neighborValue = entity.Equals(leftCellInfo.Entity)
                                    ? packedCellBuffer[leftCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(leftCellInfo.Entity)
                                        .ElementAt(leftCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[1]);
                            }
                        }
                        else if (i is > 56 and < 63)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[5], out var rightIndex))
                            {
                                var rightCellInfo = cellLookup[rightIndex];
                                var neighborValue = entity.Equals(rightCellInfo.Entity)
                                    ? packedCellBuffer[rightCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(rightCellInfo.Entity)
                                        .ElementAt(rightCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[5]);
                            }
                        }
                        else if (i % 8 == 0)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[7], out var downIndex))
                            {
                                var downCellInfo = cellLookup[downIndex];
                                var neighborValue = entity.Equals(downCellInfo.Entity)
                                    ? packedCellBuffer[downCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(downCellInfo.Entity)
                                        .ElementAt(downCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[7]);
                            }
                        }
                        else if (i % 8 == 7)
                        {
                            if (TryGetNeighborIndex(cellPosition, _neighborOffsets[3], out var upIndex))
                            {
                                var upCellInfo = cellLookup[upIndex];
                                var neighborValue = entity.Equals(upCellInfo.Entity)
                                    ? packedCellBuffer[upCellInfo.Index].Value
                                    : SystemAPI.GetBuffer<PackedCell64>(upCellInfo.Entity)
                                        .ElementAt(upCellInfo.Index).Value;
                                aliveCount += GetAliveNeighborCount(neighborValue, i, _edgeOffsets[3]);
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

                    var cellPositionIndex = cellPosition.x * gridProperties.GridSize.x + cellPosition.y;
                    nextCellArray[cellPositionIndex] = nextCells;
                }
            }

            foreach (var packedCells in SystemAPI.Query<DynamicBuffer<PackedCell64>>())
            {
                for (var bufferIndex = 0; bufferIndex < packedCells.Length; bufferIndex++)
                {
                    var packedCell = packedCells.ElementAt(bufferIndex);
                    var gridIndex = packedCell.Position.x * gridProperties.GridSize.x + packedCell.Position.y;
                    packedCells.ElementAt(bufferIndex).Value = nextCellArray[gridIndex];
                }
            }

            return;

            // LOCAL FUNCTIONS //
            bool TryGetNeighborIndex(int2 startPosition, int2 offset, out int neighborIndex)
            {
                var neighborPosition = startPosition + offset;
                if (neighborPosition.x < 0 || neighborPosition.x >= gridProperties.GridSize.x ||
                    neighborPosition.y < 0 || neighborPosition.y >= gridProperties.GridSize.y)
                {
                    neighborIndex = -1;
                    return false;
                }
                neighborIndex = neighborPosition.x * gridProperties.GridSize.x + neighborPosition.y;
                return true;
            }

            int GetAliveNeighborCount(ulong neighborValue, int startIndex, EdgeOffsets edgeOffsets)
            {
                var aliveNeighborCount = 0;

                var neighbor1 = startIndex + edgeOffsets.Offset1;
                if (IsValidNeighbor(startIndex, neighbor1))
                {
                    if ((neighborValue & (ulong)1 << neighbor1) != 0)
                    {
                        aliveNeighborCount++;
                    }
                }

                var neighbor2 = startIndex + edgeOffsets.Offset2;
                if (IsValidNeighbor(startIndex, neighbor2))
                {
                    if ((neighborValue & (ulong)1 << neighbor2) != 0)
                    {
                        aliveNeighborCount++;
                    }
                }
                
                var neighbor3 = startIndex + edgeOffsets.Offset3;
                if (IsValidNeighbor(startIndex, neighbor3))
                {
                    if ((neighborValue & (ulong)1 << neighbor3) != 0)
                    {
                        aliveNeighborCount++;
                    }
                }
                return aliveNeighborCount;
            }

            bool IsValidNeighbor(int myIndex, int neighborIndex)
            {
                if (neighborIndex is < 0 or > 63)
                    return false;
                if (myIndex == 0)
                    return neighborIndex is 7 or 15 or 56 or 57 or 63;
                if (myIndex == 7)
                    return neighborIndex is 0 or 8 or 56 or 62 or 63;
                if (myIndex == 56)
                    return neighborIndex is 0 or 1 or 7 or 55 or 63;
                if (myIndex == 63)
                    return neighborIndex is 0 or 6 or 7 or 48 or 56;
                if (myIndex is > 0 and < 7)
                    return neighborIndex is >= 56 and <= 63;
                if (myIndex is > 56 and < 63) 
                    return neighborIndex is >= 0 and <= 7;
                if (myIndex % 8 == 0)
                    return neighborIndex % 8 == 7;
                if (myIndex % 8 == 7)
                    return neighborIndex % 8 == 0;
                return false;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}