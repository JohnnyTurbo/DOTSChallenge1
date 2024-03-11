using System;
using Random = Unity.Mathematics.Random;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace TMG.GameOfLife
{
    [Serializable]
    public enum Direction
    {
        Up = 2,
        Left = 1,
        Down = 0,
        Right = 3
    }
    
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct SpawnGridViaHilbertCurveSystem : ISystem
    {
        private NativeArray<int2> _directionOffsets;
        private EntityArchetype _dataCellArchetype;
        private NativeArray<int2> _neighborOffsets;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HilbertCurveProperties>();
            _directionOffsets = new NativeArray<int2>(4, Allocator.Persistent);
            _directionOffsets[0] = new int2(0, 1);
            _directionOffsets[1] = new int2(-1, 0);
            _directionOffsets[2] = new int2(0, -1);
            _directionOffsets[3] = new int2(1, 0);
            _dataCellArchetype = state.EntityManager.CreateArchetype(ComponentType.ReadWrite<NeighborCells>(),
                ComponentType.ReadWrite<IsAlive>(), ComponentType.ReadWrite<AliveNextGen>());

            _neighborOffsets = new NativeArray<int2>(8, Allocator.Persistent);
            _neighborOffsets[0] = new int2(-1, -1);
            _neighborOffsets[1] = new int2(-1, 0);
            _neighborOffsets[2] = new int2(-1, 1);
            _neighborOffsets[3] = new int2(0, 1);
            _neighborOffsets[4] = new int2(1, 1);
            _neighborOffsets[5] = new int2(1, 0);
            _neighborOffsets[6] = new int2(1, -1);
            _neighborOffsets[7] = new int2(0, -1);
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var mainCamera = Camera.main;
            
            var hilbertProperties = SystemAPI.GetSingleton<HilbertCurveProperties>();
            var gridSize = new int2(math.pow(2, hilbertProperties.Levels));
            
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            var random = Random.CreateFromIndex(777);
            var dataEntities = new NativeArray<Entity>(gridSize.x * gridSize.y, Allocator.Temp);
            var dataCellArchetype = _dataCellArchetype;

            var directionOffsets = new NativeArray<int2>(4, Allocator.Temp);
            directionOffsets[0] = new int2(0, 1);
            directionOffsets[1] = new int2(-1, 0);
            directionOffsets[2] = new int2(0, -1);
            directionOffsets[3] = new int2(1, 0);

            var curPos = new int2(0, 0);
            var nextPos = new int2(0, 0);

            var hilbertIndex = 0;
            
            Hilbert(hilbertProperties.Levels, hilbertProperties.Direction);
            MoveNext(Direction.Right, 1);
            
            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var curPosition = new int2(x, y);
                    var curIndex = x * gridSize.y + y;
                    var curDataEntity = dataEntities[curIndex];
                    
                    foreach (var neighborOffset in _neighborOffsets)
                    {
                        var neighborPosition = curPosition + neighborOffset;
                        if (neighborPosition.x < 0 || neighborPosition.x >= gridSize.x ||
                            neighborPosition.y < 0 || neighborPosition.y >= gridSize.y)
                        {
                            continue;
                        }

                        var neighborIndex = neighborPosition.x * gridSize.y + neighborPosition.y;
                        ecb.AppendToBuffer(curDataEntity, new NeighborCells { Value = dataEntities[neighborIndex] });
                    }
                }
            }

            var gridCenter = (float2)gridSize * hilbertProperties.CellSize * 0.5f;
            mainCamera.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
            mainCamera.orthographicSize = gridSize.y * (hilbertProperties.CellSize + 0.1f) * 0.5f;

            
            ecb.Playback(state.EntityManager);
            
            // LocalFunctions
            void Hilbert(int level, Direction direction)
            {
                if (level == 1)
                {
                    switch (direction)
                    {
                        case Direction.Up:
                            MoveNext(Direction.Down, level);
                            MoveNext(Direction.Right, level);
                            MoveNext(Direction.Up, level);
                            break;
                        case Direction.Left:
                            MoveNext(Direction.Right, level);
                            MoveNext(Direction.Down, level);
                            MoveNext(Direction.Left, level);
                            break;
                        case Direction.Down:
                            MoveNext(Direction.Up, level);
                            MoveNext(Direction.Left, level);
                            MoveNext(Direction.Down, level);
                            break;
                        case Direction.Right:
                            MoveNext(Direction.Left, level);
                            MoveNext(Direction.Up, level);
                            MoveNext(Direction.Right, level);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                    }
                }
                else
                {
                    switch (direction)
                    {
                        case Direction.Up:
                            Hilbert(level - 1, Direction.Left);
                            MoveNext(Direction.Down, level);
                            Hilbert(level - 1, Direction.Up);
                            MoveNext(Direction.Right, level);
                            Hilbert(level - 1, Direction.Up);
                            MoveNext(Direction.Up, level);
                            Hilbert(level - 1, Direction.Right);
                            break;
                        case Direction.Left:
                            Hilbert(level - 1, Direction.Up);
                            MoveNext(Direction.Right, level);
                            Hilbert(level - 1, Direction.Left);
                            MoveNext(Direction.Down, level);
                            Hilbert(level - 1, Direction.Left);
                            MoveNext(Direction.Left, level);
                            Hilbert(level - 1, Direction.Down);
                            break;
                        case Direction.Down:
                            Hilbert(level - 1, Direction.Right);
                            MoveNext(Direction.Up, level);
                            Hilbert(level - 1, Direction.Down);
                            MoveNext(Direction.Left, level);
                            Hilbert(level - 1, Direction.Down);
                            MoveNext(Direction.Down, level);
                            Hilbert(level - 1, Direction.Left);
                            break;
                        case Direction.Right:
                            Hilbert(level - 1, Direction.Down);
                            MoveNext(Direction.Left, level);
                            Hilbert(level - 1, Direction.Right);
                            MoveNext(Direction.Up, level);
                            Hilbert(level - 1, Direction.Right);
                            MoveNext(Direction.Right, level);
                            Hilbert(level - 1, Direction.Up);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                    }
                }
            }
            
            void MoveNext(Direction direction, int level)
            {
                curPos = nextPos;
                SpawnCell(curPos.x, curPos.y);
                nextPos = curPos + directionOffsets[(int)direction];
                var startPos = new float3(curPos.x * hilbertProperties.CellSize, curPos.y * hilbertProperties.CellSize, -1f);
                var endPos = new float3(nextPos.x * hilbertProperties.CellSize, nextPos.y * hilbertProperties.CellSize, -1f);
                Debug.DrawLine(startPos, endPos, Color.white, 10f);
            }

            void SpawnCell(int x, int y)
            {
                var newRenderCell = ecb.Instantiate(hilbertProperties.CellPrefab);
                var col = Color.HSVToRGB((float)hilbertIndex / 256, 1, 1);
                /*ecb.SetComponent(newRenderCell, new URPMaterialPropertyBaseColor{Value = new float4
                {
                    x = (float)hilbertIndex / 256,
                    y = (float)hilbertIndex / 256,
                    z = (float)hilbertIndex / 256,
                    w = 1
                }});*/
                
                ecb.SetComponent(newRenderCell, new URPMaterialPropertyBaseColor{Value = new float4
                {
                    x = col.r,
                    y = col.g,
                    z = col.b,
                    w = 1
                }});
                
                hilbertIndex++;
                var newTransform = LocalTransform.FromPosition(x * hilbertProperties.CellSize, y * hilbertProperties.CellSize, 0f);
                ecb.SetComponent(newRenderCell, newTransform);

                var newDataCell = ecb.CreateEntity(dataCellArchetype);
                ecb.SetComponentEnabled<IsAlive>(newDataCell, random.NextBool());
                ecb.SetComponent(newRenderCell, new DataEntity { Value = newDataCell });

                var index = x * gridSize.y + y;
                dataEntities[index] = newDataCell;
                // Debug.Log($"Spawned at index {index}");
            }
        }
    }
}