using Unity.Entities;
using UnityEngine;

namespace TMG.GameOfLife
{
    public class CellDataAuthoring : MonoBehaviour
    {
        public class CellDataBaker : Baker<CellDataAuthoring>
        {
            public override void Bake(CellDataAuthoring authoring)
            {
                // var entity = GetEntity()
            }
        }
    }
}