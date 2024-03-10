using Unity.Entities;

namespace TMG.GameOfLife
{
    public struct HilbertCurveProperties : IComponentData
    {
        public int Levels;
        public Direction Direction;
    }
}