#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.AI
{
    public class Navmesh2DEditor
    {
        [MenuItem("Gismo/Build Nav Meshes &n")]

        public static void BuildNavMeshes()
        {
            foreach (NavMeshSurface2d d in Object.FindObjectsOfType<NavMeshSurface2d>())
            {
                d.BuildNavMesh();
            }
        }
    }
}
#endif
