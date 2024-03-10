using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class HilbertCurvePropertiesAuthoring : MonoBehaviour
    {
        public int Levels;
        public Direction StartDirection;

        public class HilbertCurvePropertiesBaker : Baker<HilbertCurvePropertiesAuthoring>
        {
            public override void Bake(HilbertCurvePropertiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new HilbertCurveProperties { Levels = authoring.Levels, Direction = authoring.StartDirection });
            }
        }
    }
}