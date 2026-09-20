using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Builds tiny procedural meshes so the playable never depends on the physics module or PrimitiveType colliders.
    /// </summary>
    /// <remarks>
    /// Unity's built-in cubes/spheres pull PhysX when instantiated as primitives. These meshes are
    /// CPU-side only (no colliders) and are created once by <see cref="VisualKit"/>. Quads are the
    /// live path for fox, items, and backdrop; box is the ground slab. Sphere/cylinder/octahedron
    /// remain as cheap fallbacks if sprite materials are missing.
    /// </remarks>
    public static class PrimitiveFactory
    {
        /// <summary>
        /// Unit quad in the XY plane facing the camera (−Z). UVs 0–1. Used for sprites and the backdrop.
        /// </summary>
        public static Mesh CreateQuad()
        {
            var mesh = new Mesh { name = "PlayableQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Unit cube centered at the origin. Used for the thin ground slab under the fox.
        /// </summary>
        public static Mesh CreateBox()
        {
            var mesh = new Mesh { name = "PlayableBox" };
            Vector3[] v =
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };
            mesh.vertices = new[]
            {
                v[0], v[1], v[2], v[3],
                v[1], v[5], v[6], v[2],
                v[5], v[4], v[7], v[6],
                v[4], v[0], v[3], v[7],
                v[3], v[2], v[6], v[7],
                v[4], v[5], v[1], v[0]
            };
            int[] t = new int[36];
            for (int f = 0; f < 6; f++)
            {
                int o = f * 4;
                int i = f * 6;
                t[i] = o;
                t[i + 1] = o + 1;
                t[i + 2] = o + 2;
                t[i + 3] = o;
                t[i + 4] = o + 2;
                t[i + 5] = o + 3;
            }

            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Low-poly UV sphere of diameter 1. Optional decorative mesh; not used by the default catcher loop.
        /// </summary>
        /// <param name="latitudes">Rings from pole to pole. Clamped to at least 3.</param>
        /// <param name="longitudes">Segments around the equator. Clamped to at least 3.</param>
        public static Mesh CreateSphere(int latitudes = 10, int longitudes = 14)
        {
            latitudes = Mathf.Max(3, latitudes);
            longitudes = Mathf.Max(3, longitudes);
            int vertCount = (latitudes + 1) * (longitudes + 1);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            int i = 0;
            for (int lat = 0; lat <= latitudes; lat++)
            {
                float a1 = Mathf.PI * lat / latitudes;
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);
                for (int lon = 0; lon <= longitudes; lon++)
                {
                    float a2 = 2f * Mathf.PI * lon / longitudes;
                    var n = new Vector3(sin1 * Mathf.Cos(a2), cos1, sin1 * Mathf.Sin(a2));
                    vertices[i] = n * 0.5f;
                    normals[i] = n;
                    i++;
                }
            }

            var triangles = new int[latitudes * longitudes * 6];
            int t = 0;
            for (int lat = 0; lat < latitudes; lat++)
            {
                for (int lon = 0; lon < longitudes; lon++)
                {
                    int current = lat * (longitudes + 1) + lon;
                    int next = current + longitudes + 1;
                    triangles[t++] = current;
                    triangles[t++] = current + 1;
                    triangles[t++] = next + 1;
                    triangles[t++] = current;
                    triangles[t++] = next + 1;
                    triangles[t++] = next;
                }
            }

            var mesh = new Mesh { name = "PlayableSphere" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Unit-height cylinder of diameter 1. Can be scaled flat on Y as a coin disc fallback.
        /// </summary>
        /// <param name="segments">Rim subdivisions. Clamped to at least 6.</param>
        public static Mesh CreateCylinder(int segments = 16)
        {
            segments = Mathf.Max(6, segments);
            int vertCount = (segments + 1) * 2 + 2;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            vertices[0] = new Vector3(0f, 0.5f, 0f);
            normals[0] = Vector3.up;
            vertices[1] = new Vector3(0f, -0.5f, 0f);
            normals[1] = Vector3.down;
            for (int s = 0; s <= segments; s++)
            {
                float a = 2f * Mathf.PI * s / segments;
                var rim = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                vertices[2 + s] = rim + Vector3.up * 0.5f;
                normals[2 + s] = Vector3.up;
                vertices[3 + segments + s] = rim + Vector3.down * 0.5f;
                normals[3 + segments + s] = Vector3.down;
            }

            var triangles = new int[segments * 12];
            int t = 0;
            for (int s = 0; s < segments; s++)
            {
                int topA = 2 + s;
                int topB = 2 + s + 1;
                int botA = 3 + segments + s;
                int botB = 3 + segments + s + 1;
                triangles[t++] = 0;
                triangles[t++] = topB;
                triangles[t++] = topA;
                triangles[t++] = 1;
                triangles[t++] = botA;
                triangles[t++] = botB;
                triangles[t++] = topA;
                triangles[t++] = topB;
                triangles[t++] = botB;
                triangles[t++] = topA;
                triangles[t++] = botB;
                triangles[t++] = botA;
            }

            var mesh = new Mesh { name = "PlayableCylinder" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Regular octahedron of height 1. Gem fallback if the painted sprite material is missing.
        /// </summary>
        public static Mesh CreateOctahedron()
        {
            var mesh = new Mesh { name = "PlayableOctahedron" };
            Vector3[] p =
            {
                Vector3.up * 0.5f,
                Vector3.down * 0.5f,
                Vector3.left * 0.5f,
                Vector3.right * 0.5f,
                Vector3.forward * 0.5f,
                Vector3.back * 0.5f
            };
            mesh.vertices = new[]
            {
                p[0], p[4], p[3],
                p[0], p[3], p[5],
                p[0], p[5], p[2],
                p[0], p[2], p[4],
                p[1], p[3], p[4],
                p[1], p[5], p[3],
                p[1], p[2], p[5],
                p[1], p[4], p[2]
            };
            var triangles = new int[24];
            for (int i = 0; i < 24; i++)
            {
                triangles[i] = i;
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
