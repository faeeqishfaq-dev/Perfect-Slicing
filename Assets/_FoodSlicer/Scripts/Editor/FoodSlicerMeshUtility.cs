using System.Collections.Generic;
using UnityEngine;

namespace FoodSlicer.EditorTools
{
    public static class FoodSlicerMeshUtility
    {
        #region Stylized Curved Banana
        public static Vector3 GetBananaCenter(float t)
        {
            // t from 0 (flower tip) to 1 (stem end)
            // Banana curves along Z, arches up in Y, and convex belly bows smoothly to the right in X matching Image 2
            float z = Mathf.Lerp(-0.40f, 0.40f, t);
            float arch = Mathf.Sin(t * Mathf.PI) * 0.14f;
            float stemCurl = t > 0.72f ? Mathf.Pow((t - 0.72f) / 0.28f, 1.8f) * 0.09f : 0f;
            float y = 0.04f + arch + stemCurl;
            // Convex curve to the right in X, with stalk curling back left at top end
            float x = Mathf.Sin(t * Mathf.PI) * 0.14f - (t > 0.65f ? Mathf.Pow((t - 0.65f) / 0.35f, 1.5f) * 0.15f : 0f);
            return new Vector3(x, y, z);
        }

        public static float GetBananaRadius(float t)
        {
            if (t < 0.12f) return Mathf.Lerp(0.035f, 0.12f, t / 0.12f);
            if (t > 0.78f) return Mathf.Lerp(0.12f, 0.035f, (t - 0.78f) / 0.22f);
            // Plump curved profile
            return Mathf.Lerp(0.10f, 0.125f, Mathf.Sin(t * Mathf.PI));
        }

        public static Mesh CreateCurvedBananaSegment(float startT, float endT, int lengthSteps = 4, int facets = 16, bool closeStart = true, bool closeEnd = true, bool centerPivot = false)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"BananaSegment_{startT:F2}_{endT:F2}";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trianglesPeel = new List<int>();
            List<int> trianglesCut = new List<int>();

            // Generate peel tube
            for (int i = 0; i <= lengthSteps; i++)
            {
                float t = Mathf.Lerp(startT, endT, (float)i / lengthSteps);
                Vector3 center = GetBananaCenter(t);
                float radius = GetBananaRadius(t);

                // Coordinate frame along curve
                float dt = 0.02f;
                Vector3 tangent = (GetBananaCenter(Mathf.Min(1f, t + dt)) - GetBananaCenter(Mathf.Max(0f, t - dt))).normalized;
                Vector3 upDir = Vector3.up;
                Vector3 binormal = Vector3.Cross(tangent, upDir).normalized;
                if (binormal.sqrMagnitude < 0.01f) binormal = Vector3.right;
                Vector3 normal = Vector3.Cross(binormal, tangent).normalized;

                for (int j = 0; j <= facets; j++)
                {
                    float angle = (float)j / facets * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 radialDir = (binormal * cos + normal * sin).normalized;
                    Vector3 pos = center + radialDir * radius;

                    vertices.Add(pos);
                    normals.Add(radialDir);
                    uvs.Add(new Vector2((float)j / facets, t));
                }
            }

            // Peel triangles (Submesh 0) - Double sided to ensure solid rendering from all angles
            int vertsPerRing = facets + 1;
            for (int i = 0; i < lengthSteps; i++)
            {
                for (int j = 0; j < facets; j++)
                {
                    int b0 = i * vertsPerRing + j;
                    int b1 = b0 + 1;
                    int t0 = (i + 1) * vertsPerRing + j;
                    int t1 = t0 + 1;

                    // Front side
                    trianglesPeel.Add(b0);
                    trianglesPeel.Add(t0);
                    trianglesPeel.Add(b1);

                    trianglesPeel.Add(b1);
                    trianglesPeel.Add(t0);
                    trianglesPeel.Add(t1);

                    // Back side
                    trianglesPeel.Add(b0);
                    trianglesPeel.Add(b1);
                    trianglesPeel.Add(t0);

                    trianglesPeel.Add(b1);
                    trianglesPeel.Add(t1);
                    trianglesPeel.Add(t0);
                }
            }

            // Start Cap (if closed) -> Submesh 1 (Cut face) - Double sided
            if (closeStart)
            {
                Vector3 center = GetBananaCenter(startT);
                float radius = GetBananaRadius(startT);
                float dt = 0.02f;
                Vector3 tangent = (GetBananaCenter(Mathf.Min(1f, startT + dt)) - GetBananaCenter(Mathf.Max(0f, startT - dt))).normalized;
                Vector3 capNormal = -tangent;
                Vector3 upDir = Vector3.up;
                Vector3 binormal = Vector3.Cross(tangent, upDir).normalized;
                if (binormal.sqrMagnitude < 0.01f) binormal = Vector3.right;
                Vector3 normal = Vector3.Cross(binormal, tangent).normalized;

                int centerIdx = vertices.Count;
                vertices.Add(center);
                normals.Add(capNormal);
                uvs.Add(new Vector2(0.5f, 0.5f));

                int startCapIdx = vertices.Count;
                for (int j = 0; j <= facets; j++)
                {
                    float angle = (float)j / facets * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    Vector3 pos = center + (binormal * cos + normal * sin) * radius;

                    vertices.Add(pos);
                    normals.Add(capNormal);
                    uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
                }

                for (int j = 0; j < facets; j++)
                {
                    // Clockwise
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(startCapIdx + j);
                    trianglesCut.Add(startCapIdx + j + 1);

                    // Counter-clockwise
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(startCapIdx + j + 1);
                    trianglesCut.Add(startCapIdx + j);
                }
            }

            // End Cap (if closed) -> Submesh 1 (Cut face) - Double sided
            if (closeEnd)
            {
                Vector3 center = GetBananaCenter(endT);
                float radius = GetBananaRadius(endT);
                float dt = 0.02f;
                Vector3 tangent = (GetBananaCenter(Mathf.Min(1f, endT + dt)) - GetBananaCenter(Mathf.Max(0f, endT - dt))).normalized;
                Vector3 capNormal = tangent;
                Vector3 upDir = Vector3.up;
                Vector3 binormal = Vector3.Cross(tangent, upDir).normalized;
                if (binormal.sqrMagnitude < 0.01f) binormal = Vector3.right;
                Vector3 normal = Vector3.Cross(binormal, tangent).normalized;

                int centerIdx = vertices.Count;
                vertices.Add(center);
                normals.Add(capNormal);
                uvs.Add(new Vector2(0.5f, 0.5f));

                int endCapIdx = vertices.Count;
                for (int j = 0; j <= facets; j++)
                {
                    float angle = (float)j / facets * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    Vector3 pos = center + (binormal * cos + normal * sin) * radius;

                    vertices.Add(pos);
                    normals.Add(capNormal);
                    uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
                }

                for (int j = 0; j < facets; j++)
                {
                    // Clockwise
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(endCapIdx + j);
                    trianglesCut.Add(endCapIdx + j + 1);

                    // Counter-clockwise
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(endCapIdx + j + 1);
                    trianglesCut.Add(endCapIdx + j);
                }
            }

            if (centerPivot)
            {
                Vector3 pivot = GetBananaCenter((startT + endT) * 0.5f);
                for (int v = 0; v < vertices.Count; v++)
                {
                    vertices[v] -= pivot;
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            if (trianglesCut.Count > 0)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(trianglesPeel, 0);
                mesh.SetTriangles(trianglesCut, 1);
            }
            else
            {
                mesh.subMeshCount = 1;
                mesh.SetTriangles(trianglesPeel, 0);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateBananaCutCoin(float tMid, float thickness = 0.18f, int facets = 16)
        {
            float dt = thickness / 1.5f;
            float startT = Mathf.Clamp(tMid - dt * 0.5f, 0.05f, 0.95f);
            float endT = Mathf.Clamp(tMid + dt * 0.5f, 0.05f, 0.95f);
            return CreateCurvedBananaSegment(startT, endT, 2, facets, true, true, true);
        }
        #endregion

        #region Segmented Ellipsoid / Citrus / Watermelon
        public static Mesh CreateSegmentedEllipsoid(Vector3 radius, float zMin, float zMax, int latSteps = 6, int lonSteps = 24, bool closeFront = true, bool closeBack = true)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"SegmentEllipsoid_{zMin:F2}_{zMax:F2}";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trianglesSkin = new List<int>();
            List<int> trianglesCut = new List<int>();

            // Clamp z range
            float clampedMin = Mathf.Clamp(zMin, -radius.z, radius.z);
            float clampedMax = Mathf.Clamp(zMax, -radius.z, radius.z);

            // Shell vertices
            for (int lat = 0; lat <= latSteps; lat++)
            {
                float t = (float)lat / latSteps;
                float z = Mathf.Lerp(clampedMin, clampedMax, t);
                float normalizedZ = radius.z > 0 ? Mathf.Clamp(z / radius.z, -1f, 1f) : 0f;
                float ringScale = Mathf.Sqrt(Mathf.Max(0f, 1f - (normalizedZ * normalizedZ)));

                float rx = radius.x * ringScale;
                float ry = radius.y * ringScale;

                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float angle = (float)lon / lonSteps * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 pos = new Vector3(cos * rx, sin * ry, z);
                    Vector3 n = new Vector3(
                        radius.x > 0 ? pos.x / (radius.x * radius.x) : 0,
                        radius.y > 0 ? pos.y / (radius.y * radius.y) : 0,
                        radius.z > 0 ? pos.z / (radius.z * radius.z) : 0
                    ).normalized;

                    vertices.Add(pos);
                    normals.Add(n);
                    uvs.Add(new Vector2((float)lon / lonSteps, (z + radius.z) / (2f * radius.z)));
                }
            }

            // Shell triangles (Submesh 0)
            int vertsPerRing = lonSteps + 1;
            for (int lat = 0; lat < latSteps; lat++)
            {
                for (int lon = 0; lon < lonSteps; lon++)
                {
                    int b0 = lat * vertsPerRing + lon;
                    int b1 = b0 + 1;
                    int t0 = (lat + 1) * vertsPerRing + lon;
                    int t1 = t0 + 1;

                    trianglesSkin.Add(b0);
                    trianglesSkin.Add(b1);
                    trianglesSkin.Add(t0);

                    trianglesSkin.Add(b1);
                    trianglesSkin.Add(t1);
                    trianglesSkin.Add(t0);
                }
            }

            // Front Cap at zMin (if closed and not at the extreme pole)
            if (closeFront && clampedMin > -radius.z * 0.98f)
            {
                float normalizedZ = clampedMin / radius.z;
                float ringScale = Mathf.Sqrt(Mathf.Max(0f, 1f - (normalizedZ * normalizedZ)));
                float rx = radius.x * ringScale;
                float ry = radius.y * ringScale;

                int centerIdx = vertices.Count;
                vertices.Add(new Vector3(0, 0, clampedMin));
                normals.Add(Vector3.back);
                uvs.Add(new Vector2(0.5f, 0.5f));

                int capStartIdx = vertices.Count;
                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float angle = (float)lon / lonSteps * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    vertices.Add(new Vector3(cos * rx, sin * ry, clampedMin));
                    normals.Add(Vector3.back);
                    uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
                }

                for (int lon = 0; lon < lonSteps; lon++)
                {
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(capStartIdx + lon + 1);
                    trianglesCut.Add(capStartIdx + lon);
                }
            }

            // Back Cap at zMax (if closed and not at the extreme pole)
            if (closeBack && clampedMax < radius.z * 0.98f)
            {
                float normalizedZ = clampedMax / radius.z;
                float ringScale = Mathf.Sqrt(Mathf.Max(0f, 1f - (normalizedZ * normalizedZ)));
                float rx = radius.x * ringScale;
                float ry = radius.y * ringScale;

                int centerIdx = vertices.Count;
                vertices.Add(new Vector3(0, 0, clampedMax));
                normals.Add(Vector3.forward);
                uvs.Add(new Vector2(0.5f, 0.5f));

                int capStartIdx = vertices.Count;
                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float angle = (float)lon / lonSteps * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    vertices.Add(new Vector3(cos * rx, sin * ry, clampedMax));
                    normals.Add(Vector3.forward);
                    uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
                }

                for (int lon = 0; lon < lonSteps; lon++)
                {
                    trianglesCut.Add(centerIdx);
                    trianglesCut.Add(capStartIdx + lon);
                    trianglesCut.Add(capStartIdx + lon + 1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            if (trianglesCut.Count > 0)
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(trianglesSkin, 0);
                mesh.SetTriangles(trianglesCut, 1);
            }
            else
            {
                mesh.subMeshCount = 1;
                mesh.SetTriangles(trianglesSkin, 0);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateWatermelonMesh(Vector3 radius, int latSteps = 14, int lonSteps = 24)
        {
            Mesh mesh = new Mesh();
            mesh.name = "WatermelonMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int lat = 0; lat <= latSteps; lat++)
            {
                float theta = (float)lat / latSteps * Mathf.PI;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSteps; lon++)
                {
                    float phi = (float)lon / lonSteps * Mathf.PI * 2f;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    Vector3 pos = new Vector3(cosPhi * sinTheta * radius.x, cosTheta * radius.y, sinPhi * sinTheta * radius.z);
                    Vector3 n = new Vector3(
                        radius.x > 0 ? pos.x / (radius.x * radius.x) : 0,
                        radius.y > 0 ? pos.y / (radius.y * radius.y) : 0,
                        radius.z > 0 ? pos.z / (radius.z * radius.z) : 0
                    ).normalized;

                    vertices.Add(pos);
                    normals.Add(n);
                    uvs.Add(new Vector2((float)lon / lonSteps, (float)lat / latSteps));
                }
            }

            int vertsPerRow = lonSteps + 1;
            for (int lat = 0; lat < latSteps; lat++)
            {
                for (int lon = 0; lon < lonSteps; lon++)
                {
                    int b0 = lat * vertsPerRow + lon;
                    int b1 = b0 + 1;
                    int t0 = (lat + 1) * vertsPerRow + lon;
                    int t1 = t0 + 1;

                    triangles.Add(b0);
                    triangles.Add(t0);
                    triangles.Add(b1);

                    triangles.Add(b1);
                    triangles.Add(t0);
                    triangles.Add(t1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion

        #region Stylized Large Chef Knife (Matches Image 2)
        public static Mesh CreateLargeChefBlade(float length = 1.90f, float height = 0.24f, float thickness = 0.025f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "LargeChefBlade";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // The blade originates at X = 0 (bolster) and extends toward negative X (towards left of screen)
            // Spine runs along top, cutting edge along bottom, curved belly sweeping up to the tip
            int steps = 14;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = -t * length;

                // Spine profile: gentle upward bow then curve down to pointed tip
                float spineY = 0.12f + Mathf.Sin(t * Mathf.PI * 0.7f) * 0.035f - (t > 0.7f ? Mathf.Pow((t - 0.7f) / 0.3f, 2f) * 0.08f : 0f);

                // Cutting edge profile: belly drops down and sweeps up toward tip
                float edgeY;
                if (t < 0.6f)
                {
                    edgeY = -0.11f - Mathf.Sin(t / 0.6f * Mathf.PI * 0.5f) * 0.025f;
                }
                else
                {
                    float tipT = (t - 0.6f) / 0.4f;
                    edgeY = Mathf.Lerp(-0.135f, spineY - 0.012f, Mathf.SmoothStep(0f, 1f, tipT));
                }

                float halfThick = Mathf.Lerp(thickness * 0.5f, 0.002f, t);

                // Vertices for this slice:
                // 0: Spine Left, 1: Spine Right, 2: Mid Left, 3: Mid Right, 4: Sharp Edge
                float midY = (spineY + edgeY) * 0.5f;

                vertices.Add(new Vector3(x, spineY, -halfThick)); // 0: spine left
                vertices.Add(new Vector3(x, spineY,  halfThick)); // 1: spine right
                vertices.Add(new Vector3(x, midY, -halfThick * 0.7f)); // 2: mid left
                vertices.Add(new Vector3(x, midY,  halfThick * 0.7f)); // 3: mid right
                vertices.Add(new Vector3(x, edgeY, 0f)); // 4: sharp edge

                normals.Add(new Vector3(0, 0.4f, -0.9f).normalized);
                normals.Add(new Vector3(0, 0.4f,  0.9f).normalized);
                normals.Add(new Vector3(0, 0.1f, -0.98f).normalized);
                normals.Add(new Vector3(0, 0.1f,  0.98f).normalized);
                normals.Add(new Vector3(0, -1f, 0f));

                uvs.Add(new Vector2(t, 1f));
                uvs.Add(new Vector2(t, 1f));
                uvs.Add(new Vector2(t, 0.5f));
                uvs.Add(new Vector2(t, 0.5f));
                uvs.Add(new Vector2(t, 0f));
            }

            // Build blade face quads
            int vertsPerStep = 5;
            for (int i = 0; i < steps; i++)
            {
                int b = i * vertsPerStep;
                int n = (i + 1) * vertsPerStep;

                // Spine Top
                triangles.Add(b + 0);
                triangles.Add(n + 0);
                triangles.Add(b + 1);

                triangles.Add(n + 0);
                triangles.Add(n + 1);
                triangles.Add(b + 1);

                // Left Upper (Spine Left -> Mid Left)
                triangles.Add(b + 0);
                triangles.Add(b + 2);
                triangles.Add(n + 0);

                triangles.Add(n + 0);
                triangles.Add(b + 2);
                triangles.Add(n + 2);

                // Left Lower (Mid Left -> Edge)
                triangles.Add(b + 2);
                triangles.Add(b + 4);
                triangles.Add(n + 2);

                triangles.Add(n + 2);
                triangles.Add(b + 4);
                triangles.Add(n + 4);

                // Right Upper (Spine Right -> Mid Right)
                triangles.Add(b + 1);
                triangles.Add(n + 1);
                triangles.Add(b + 3);

                triangles.Add(n + 1);
                triangles.Add(n + 3);
                triangles.Add(b + 3);

                // Right Lower (Mid Right -> Edge)
                triangles.Add(b + 3);
                triangles.Add(n + 3);
                triangles.Add(b + 4);

                triangles.Add(n + 3);
                triangles.Add(n + 4);
                triangles.Add(b + 4);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateChefHandle(float length = 0.58f, float height = 0.11f, float width = 0.08f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ChefHandleMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // Handle extends from X = 0 (bolster) toward positive X (lower-right)
            int steps = 8;
            int radSteps = 10;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = t * length;
                // Slight ergonomic curve and flare at butt pommel
                float scale = 1f + (t > 0.7f ? (t - 0.7f) * 0.45f : 0f) - Mathf.Sin(t * Mathf.PI) * 0.08f;
                float curH = height * 0.5f * scale;
                float curW = width * 0.5f * scale;
                float yOffset = -t * 0.04f;

                for (int j = 0; j <= radSteps; j++)
                {
                    float angle = (float)j / radSteps * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 pos = new Vector3(x, yOffset + sin * curH, cos * curW);
                    Vector3 n = new Vector3(0, sin, cos).normalized;

                    vertices.Add(pos);
                    normals.Add(n);
                    uvs.Add(new Vector2(t * 3f, (float)j / radSteps));
                }
            }

            int vertsPerRing = radSteps + 1;
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < radSteps; j++)
                {
                    int b0 = i * vertsPerRing + j;
                    int b1 = b0 + 1;
                    int t0 = (i + 1) * vertsPerRing + j;
                    int t1 = t0 + 1;

                    triangles.Add(b0);
                    triangles.Add(b1);
                    triangles.Add(t0);

                    triangles.Add(b1);
                    triangles.Add(t1);
                    triangles.Add(t0);
                }
            }

            // Cap at end butt
            int centerIdx = vertices.Count;
            vertices.Add(new Vector3(length, -0.04f, 0));
            normals.Add(Vector3.right);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int capStartIdx = vertices.Count;
            int lastRing = steps * vertsPerRing;
            for (int j = 0; j <= radSteps; j++)
            {
                vertices.Add(vertices[lastRing + j]);
                normals.Add(Vector3.right);
                uvs.Add(new Vector2(0.5f, 0.5f));
            }

            for (int j = 0; j < radSteps; j++)
            {
                triangles.Add(centerIdx);
                triangles.Add(capStartIdx + j);
                triangles.Add(capStartIdx + j + 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateBolsterCollar(float length = 0.06f, float radius = 0.065f)
        {
            return CreateCylinderSegment(radius, radius, length, 12);
        }
        #endregion

        #region Backwards Compatibility Primitives
        public static Mesh CreateCylinderSegment(float radiusTop, float radiusBottom, float height, int radialSegments = 16)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SegmentCylinder";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float halfH = height * 0.5f;

            for (int i = 0; i <= radialSegments; i++)
            {
                float angle = (float)i / radialSegments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(cos * radiusBottom, -halfH, sin * radiusBottom));
                vertices.Add(new Vector3(cos * radiusTop, halfH, sin * radiusTop));

                Vector3 normal = new Vector3(cos, (radiusBottom - radiusTop) / height, sin).normalized;
                normals.Add(normal);
                normals.Add(normal);

                uvs.Add(new Vector2((float)i / radialSegments, 0f));
                uvs.Add(new Vector2((float)i / radialSegments, 1f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                int b0 = i * 2;
                int t0 = i * 2 + 1;
                int b1 = (i + 1) * 2;
                int t1 = (i + 1) * 2 + 1;

                triangles.Add(b0);
                triangles.Add(t0);
                triangles.Add(b1);

                triangles.Add(b1);
                triangles.Add(t0);
                triangles.Add(t1);
            }

            // Top Cap
            int topCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, halfH, 0f));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int topStartIndex = vertices.Count;
            for (int i = 0; i <= radialSegments; i++)
            {
                float angle = (float)i / radialSegments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(cos * radiusTop, halfH, sin * radiusTop));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles.Add(topCenterIndex);
                triangles.Add(topStartIndex + i);
                triangles.Add(topStartIndex + i + 1);
            }

            // Bottom Cap
            int bottomCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, -halfH, 0f));
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int bottomStartIndex = vertices.Count;
            for (int i = 0; i <= radialSegments; i++)
            {
                float angle = (float)i / radialSegments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(cos * radiusBottom, -halfH, sin * radiusBottom));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles.Add(bottomCenterIndex);
                triangles.Add(bottomStartIndex + i + 1);
                triangles.Add(bottomStartIndex + i);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateEllipsoid(Vector3 radius, int latSegments = 14, int lonSegments = 20)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Ellipsoid";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = (float)lat / latSegments * Mathf.PI;
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = (float)lon / lonSegments * Mathf.PI * 2f;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    Vector3 unitDir = new Vector3(cosPhi * sinTheta, cosTheta, sinPhi * sinTheta);
                    Vector3 pos = Vector3.Scale(unitDir, radius);

                    vertices.Add(pos);
                    normals.Add(unitDir.normalized);
                    uvs.Add(new Vector2((float)lon / lonSegments, (float)lat / latSegments));
                }
            }

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int first = (lat * (lonSegments + 1)) + lon;
                    int second = first + lonSegments + 1;

                    triangles.Add(first);
                    triangles.Add(second);
                    triangles.Add(first + 1);

                    triangles.Add(second);
                    triangles.Add(second + 1);
                    triangles.Add(first + 1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateCurvedBanana(float length, float curvature, float radius, float startFraction, float endFraction, int lengthSteps = 10, int radSteps = 12)
        {
            return CreateCurvedBananaSegment(startFraction, endFraction, lengthSteps, radSteps, true, true);
        }

        public static Mesh CreateBox(Vector3 size)
        {
            Mesh mesh = new Mesh();
            mesh.name = "BoxMesh";

            Vector3 half = size * 0.5f;

            Vector3[] vertices = new Vector3[]
            {
                // Front
                new Vector3(-half.x, -half.y,  half.z), new Vector3( half.x, -half.y,  half.z),
                new Vector3( half.x,  half.y,  half.z), new Vector3(-half.x,  half.y,  half.z),
                // Back
                new Vector3( half.x, -half.y, -half.z), new Vector3(-half.x, -half.y, -half.z),
                new Vector3(-half.x,  half.y, -half.z), new Vector3( half.x,  half.y, -half.z),
                // Top
                new Vector3(-half.x,  half.y,  half.z), new Vector3( half.x,  half.y,  half.z),
                new Vector3( half.x,  half.y, -half.z), new Vector3(-half.x,  half.y, -half.z),
                // Bottom
                new Vector3(-half.x, -half.y, -half.z), new Vector3( half.x, -half.y, -half.z),
                new Vector3( half.x, -half.y,  half.z), new Vector3(-half.x, -half.y,  half.z),
                // Left
                new Vector3(-half.x, -half.y, -half.z), new Vector3(-half.x, -half.y,  half.z),
                new Vector3(-half.x,  half.y,  half.z), new Vector3(-half.x,  half.y, -half.z),
                // Right
                new Vector3( half.x, -half.y,  half.z), new Vector3( half.x, -half.y, -half.z),
                new Vector3( half.x,  half.y, -half.z), new Vector3( half.x,  half.y,  half.z)
            };

            Vector3[] normals = new Vector3[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right
            };

            Vector2[] uvs = new Vector2[24];
            for (int i = 0; i < 6; i++)
            {
                uvs[i * 4 + 0] = new Vector2(0f, 0f);
                uvs[i * 4 + 1] = new Vector2(1f, 0f);
                uvs[i * 4 + 2] = new Vector2(1f, 1f);
                uvs[i * 4 + 3] = new Vector2(0f, 1f);
            }

            int[] triangles = new int[]
            {
                0, 3, 2, 0, 2, 1,       // Front
                4, 7, 6, 4, 6, 5,       // Back
                8, 9, 10, 8, 10, 11,    // Top
                12, 13, 14, 12, 14, 15, // Bottom
                16, 19, 18, 16, 18, 17, // Left
                20, 23, 22, 20, 22, 21  // Right
            };

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateQuad(float width, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = "QuadMesh";
            float hw = width * 0.5f;
            float hh = height * 0.5f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, -hh, 0f),
                new Vector3( hw, -hh, 0f),
                new Vector3( hw,  hh, 0f),
                new Vector3(-hw,  hh, 0f)
            };

            mesh.normals = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.triangles = new int[]
            {
                0, 3, 2,
                0, 2, 1
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateKnifeBlade(float length, float height, float thickness, float tipCurve = 0.3f, bool isCurved = false)
        {
            return CreateLargeChefBlade(length, height, thickness);
        }

        public static Mesh CreateKnifeHandle(float length, float width, float height)
        {
            return CreateChefHandle(length, height, width);
        }
        #endregion

        #region Ceremonial Cushion & Tassels
        public static Mesh CreateCushionMesh(Vector3 size, int subdivisions = 14)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Mesh_Cushion";

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float hx = size.x * 0.5f;
            float hz = size.z * 0.5f;
            float hy = size.y * 0.5f;

            // Generate puffy top hemisphere grid
            int n = subdivisions;
            int topStart = 0;
            for (int iz = 0; iz <= n; iz++)
            {
                float tz = (float)iz / n;
                float z = Mathf.Lerp(-hz, hz, tz);
                float fz = Mathf.Cos((tz - 0.5f) * Mathf.PI);

                for (int ix = 0; ix <= n; ix++)
                {
                    float tx = (float)ix / n;
                    float x = Mathf.Lerp(-hx, hx, tx);
                    float fx = Mathf.Cos((tx - 0.5f) * Mathf.PI);

                    // Puffy dome profile
                    float bulge = Mathf.Pow(Mathf.Clamp01(fx * fz), 0.65f);
                    float y = hy * bulge;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(tx, tz));
                    normals.Add(new Vector3(0, 1, 0));
                }
            }

            for (int iz = 0; iz < n; iz++)
            {
                for (int ix = 0; ix < n; ix++)
                {
                    int a = topStart + iz * (n + 1) + ix;
                    int b = a + 1;
                    int c = a + (n + 1);
                    int d = c + 1;

                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            // Generate puffy bottom hemisphere grid
            int bottomStart = verts.Count;
            for (int iz = 0; iz <= n; iz++)
            {
                float tz = (float)iz / n;
                float z = Mathf.Lerp(-hz, hz, tz);
                float fz = Mathf.Cos((tz - 0.5f) * Mathf.PI);

                for (int ix = 0; ix <= n; ix++)
                {
                    float tx = (float)ix / n;
                    float x = Mathf.Lerp(-hx, hx, tx);
                    float fx = Mathf.Cos((tx - 0.5f) * Mathf.PI);

                    float bulge = Mathf.Pow(Mathf.Clamp01(fx * fz), 0.65f);
                    float y = -hy * bulge;

                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(tx, tz));
                    normals.Add(new Vector3(0, -1, 0));
                }
            }

            for (int iz = 0; iz < n; iz++)
            {
                for (int ix = 0; ix < n; ix++)
                {
                    int a = bottomStart + iz * (n + 1) + ix;
                    int b = a + 1;
                    int c = a + (n + 1);
                    int d = c + 1;

                    // Winding order inverted for downward face
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreatePipingMesh(float width, float depth, float thickness)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Mesh_PillowPiping";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float r = thickness * 0.5f;

            // 4 perimeter tubular segments around the square cushion border
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-hw, 0f, -hd),
                new Vector3( hw, 0f, -hd),
                new Vector3( hw, 0f,  hd),
                new Vector3(-hw, 0f,  hd)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 p0 = corners[i];
                Vector3 p1 = corners[(i + 1) % 4];
                Vector3 dir = (p1 - p0).normalized;
                Vector3 normal = Vector3.up;
                Vector3 binormal = Vector3.Cross(dir, normal).normalized;

                int segStart = verts.Count;
                int radialSteps = 8;

                for (int ring = 0; ring <= 1; ring++)
                {
                    Vector3 center = ring == 0 ? p0 : p1;
                    for (int step = 0; step < radialSteps; step++)
                    {
                        float angle = (float)step / radialSteps * Mathf.PI * 2f;
                        Vector3 offset = normal * (Mathf.Sin(angle) * r) + binormal * (Mathf.Cos(angle) * r);
                        verts.Add(center + offset);
                        uvs.Add(new Vector2((float)step / radialSteps, ring));
                    }
                }

                for (int step = 0; step < radialSteps; step++)
                {
                    int next = (step + 1) % radialSteps;
                    int a = segStart + step;
                    int b = segStart + next;
                    int c = segStart + radialSteps + step;
                    int d = segStart + radialSteps + next;

                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateTasselMesh(float height = 0.08f, float radius = 0.022f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Mesh_CornerTassel";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int radial = 10;
            // 1. Tassel Top Cap / Bead
            verts.Add(Vector3.zero); // Apex tip
            uvs.Add(new Vector2(0.5f, 1f));

            for (int i = 0; i < radial; i++)
            {
                float angle = (float)i / radial * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * (radius * 0.6f);
                float z = Mathf.Sin(angle) * (radius * 0.6f);
                verts.Add(new Vector3(x, -height * 0.25f, z));
                uvs.Add(new Vector2((float)i / radial, 0.75f));
            }

            for (int i = 0; i < radial; i++)
            {
                int next = (i + 1) % radial;
                tris.Add(0); tris.Add(1 + i); tris.Add(1 + next);
            }

            // 2. Hanging fringe cone
            int fringeStart = verts.Count;
            for (int i = 0; i < radial; i++)
            {
                float angle = (float)i / radial * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                verts.Add(new Vector3(x, -height, z));
                uvs.Add(new Vector2((float)i / radial, 0f));
            }

            for (int i = 0; i < radial; i++)
            {
                int next = (i + 1) % radial;
                int topA = 1 + i;
                int topB = 1 + next;
                int botA = fringeStart + i;
                int botB = fringeStart + next;

                tris.Add(topA); tris.Add(botA); tris.Add(topB);
                tris.Add(topB); tris.Add(botA); tris.Add(botB);
            }

            // Bottom cap
            int centerBot = verts.Count;
            verts.Add(new Vector3(0, -height, 0));
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < radial; i++)
            {
                int next = (i + 1) % radial;
                tris.Add(centerBot); tris.Add(fringeStart + next); tris.Add(fringeStart + i);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
        #endregion
    }
}
