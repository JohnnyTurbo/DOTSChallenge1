using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class RenderCellAuthoring : MonoBehaviour
    {
        public class RenderCellBaker : Baker<RenderCellAuthoring>
        {
            public override void Bake(RenderCellAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<DataEntity>(entity);
            }
        }
    }
}