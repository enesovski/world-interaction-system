
using UnityEngine;

namespace Archura.WIS.Core
{
    public readonly struct PlanarSurfaceMapping
    {
        private readonly Transform surface;
        private readonly Bounds bounds;

        private readonly Vector2 uvOrigin;
        private readonly Vector2 uvRight;
        private readonly Vector2 uvForward;

        private PlanarSurfaceMapping(Transform surface, Bounds bounds,
            Vector2 uvOrigin, Vector2 uvRight, Vector2 uvForward)
        {
            this.surface = surface;
            this.bounds = bounds;
            this.uvOrigin = uvOrigin;
            this.uvRight = uvRight;
            this.uvForward = uvForward;
        }

        public static bool TryCreate(Mesh mesh, Transform surface, out PlanarSurfaceMapping mapping)
        {
            mapping = default;

            if (mesh == null || surface == null || !mesh.isReadable)
                return false;

            Bounds bounds = mesh.bounds;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;

            if (bounds.size.x <= 0f || bounds.size.z <= 0f || uvs.Length != vertices.Length)
                return false;

            int origin = FindCorner(vertices, bounds.min.x, bounds.min.z);
            int right = FindCorner(vertices, bounds.max.x, bounds.min.z);
            int forward = FindCorner(vertices, bounds.min.x, bounds.max.z);

            if (origin < 0 || right < 0 || forward < 0)
                return false;

            mapping = new PlanarSurfaceMapping(
                surface,
                bounds,
                uvs[origin],
                uvs[right] - uvs[origin],
                uvs[forward] - uvs[origin]
            );

            return true;
        }

        public bool TryWorldToUV(Vector3 position, out Vector2 uv)
        {
            uv = default;

            if (surface == null)
                return false;

            Vector3 local = surface.InverseTransformPoint(position);

            float x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x);
            float z = Mathf.InverseLerp(bounds.min.z, bounds.max.z, local.z);

            if (!bounds.Contains(new Vector3(local.x, bounds.center.y, local.z)))
                return false;

            uv = uvOrigin + uvRight * x + uvForward * z;
            return true;
        }

        public void GetWorldUvAxes(out Vector3 uAxis, out Vector3 vAxis)
        {
            float determinant = uvRight.x * uvForward.y - uvRight.y * uvForward.x;

            if (Mathf.Abs(determinant) < 0.00001f)
            {
                uAxis = Vector3.zero;
                vAxis = Vector3.zero;
                return;
            }

            Vector3 right = surface.TransformVector(Vector3.right * bounds.size.x);
            Vector3 forward = surface.TransformVector(Vector3.forward * bounds.size.z);

            uAxis = (right * uvForward.y - forward * uvRight.y) / determinant;
            vAxis = (forward * uvRight.x - right * uvForward.x) / determinant;
        }

        private static int FindCorner(Vector3[] vertices, float x, float z)
        {
            const float tolerance = 0.0001f;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (Mathf.Abs(vertices[i].x - x) < tolerance &&
                    Mathf.Abs(vertices[i].z - z) < tolerance)
                    return i;
            }

            return -1;
        }
    }
}
