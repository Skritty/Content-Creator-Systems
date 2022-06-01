using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;

public class MeshSlicer
{
    /// <summary>
    /// Slices a mesh contained within a mesh filter and sets all the submesh materials
    /// </summary>
    /// <param name="mf"></param>
    /// <param name="mr"></param>
    public static void SliceMesh(MeshFilter mf, MeshRenderer mr, Vector3 scale)
    {
        mf.sharedMesh = SliceMesh(mf.sharedMesh, scale);

        //Fill out materials
        Material[] newMats = new Material[mf.sharedMesh.subMeshCount];
        for (int i = 0; i < newMats.Length; i++)
            newMats[i] = mr.sharedMaterial;
        mr.sharedMaterials = newMats;
    }

    /// <summary>
    /// Slices a mesh
    /// </summary>
    /// <param name="mesh">The mesh to be sliced</param>
    /// <returns>The sliced mesh</returns>
    public static UnityEngine.Mesh SliceMesh(UnityEngine.Mesh mesh, Vector3 scale)
    {
        //Slice mesh
        Matrix4x4 warp = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
        UnityEngine.Mesh originalMesh = mesh;
        Vector3[] vertices = new Vector3[originalMesh.vertices.Length];
        for (int i = 0; i < originalMesh.vertices.Length; i++)
            vertices[i] = warp * originalMesh.vertices[i];
        originalMesh.vertices = vertices;
        SliceJob jobData = new SliceJob(mesh);
        JobHandle handle = jobData.Schedule();
        handle.Complete();
        //Recreate mesh using new data
        UnityEngine.Mesh m = new UnityEngine.Mesh();
        m.name = mesh.name;
        m.vertices = jobData.vertices.Take(jobData.vertLength[0]).ToArray();

        vertices = new Vector3[m.vertices.Length];
        for (int i = 0; i < originalMesh.vertices.Length; i++)
            vertices[i] = Matrix4x4.Inverse(warp) * m.vertices[i];
        m.vertices = vertices;

        m.triangles = jobData.triangles.ToArray();
        m.uv = jobData.uv.Take(jobData.uvLength[0]).ToArray();
        m.subMeshCount = jobData.submeshCount[0];
        for (int i = 0; i < m.subMeshCount; i++)
            m.SetSubMesh(i, jobData.submeshes[i], MeshUpdateFlags.Default);
        m.RecalculateTangents();
        m.RecalculateBounds();
        m.RecalculateNormals();
        jobData.Dispose();

        return m;
    }

    private struct SliceJob : IJob
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<int> vertLength;
        public NativeArray<int> triangles;
        public NativeArray<Vector2> uv;
        public NativeArray<int> uvLength;
        public NativeArray<SubMeshDescriptor> submeshes;
        public NativeArray<int> submeshCount;

        public SliceJob(UnityEngine.Mesh mesh)
        {
            triangles = new NativeArray<int>(mesh.triangles, Allocator.TempJob);

            vertices = new NativeArray<Vector3>(mesh.vertices.Length * 2, Allocator.TempJob);
            vertLength = new NativeArray<int>(new int[] { mesh.vertices.Length }, Allocator.TempJob);
            for (int i = 0; i < mesh.vertices.Length; i++)
                vertices[i] = mesh.vertices[i];

            uv = new NativeArray<Vector2>(mesh.uv.Length * 2, Allocator.TempJob);
            uvLength = new NativeArray<int>(new int[] { mesh.uv.Length }, Allocator.TempJob);
            for (int i = 0; i < mesh.uv.Length; i++)
                uv[i] = mesh.uv[i];

            submeshes = new NativeArray<SubMeshDescriptor>(1000, Allocator.TempJob);
            submeshCount = new NativeArray<int>(1, Allocator.TempJob);
        }
        public void Execute()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.Take(vertLength[0]).ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.Take(uvLength[0]).ToArray();

            SlicingMesh sMesh = new SlicingMesh(mesh);
            sMesh.SliceIntoSubmeshes(50f);
            sMesh.CombineNeighboringSubmeshes(.5f);
            Mesh m2 = sMesh.ToMeshStruct();

            vertLength[0] = m2.vertices.Length;
            for (int i = 0; i < m2.vertices.Length; i++)
                vertices[i] = m2.vertices[i];
            uvLength[0] = m2.uv.Length;
            for (int i = 0; i < m2.uv.Length; i++)
                uv[i] = m2.uv[i];
            for (int i = 0; i < m2.triangles.Length; i++)
                triangles[i] = m2.triangles[i];
            submeshCount[0] = m2.submeshCount;
            for (int i = 0; i < m2.submeshes.Count; i++)
            {
                submeshes[i] = m2.submeshes[i];
            }
        }

        public void Dispose()
        {
            vertices.Dispose();
            vertLength.Dispose();
            triangles.Dispose();
            uv.Dispose();
            uvLength.Dispose();
            submeshCount.Dispose();
            submeshes.Dispose();
        }
    }

    /// <summary>
    /// Splits all submeshes into seperate GameObjects
    /// </summary>
    /// <param name="mf"></param>
    /// <param name="mr"></param>
    public static void DisconnectSubmeshes(MeshFilter mf, MeshRenderer mr)
    {

    }

    /// <summary>
    /// Splits a submesh off from the mesh of an object, removing it from that mesh
    /// </summary>
    /// <param name="mf"></param>
    /// <param name="mr"></param>
    /// <param name="submeshIndex"></param>
    /// <returns></returns>
    public static UnityEngine.Mesh DisconnectSubmesh(MeshFilter mf, int submeshIndex)
    {
        UnityEngine.Mesh submesh = DisconnectSubmesh(mf.mesh, submeshIndex);
        // Remove the submesh from the current mesh
        SubMeshDescriptor empty = new SubMeshDescriptor();
        empty.topology = MeshTopology.Triangles;
        mf.mesh.SetSubMesh(submeshIndex, empty);
        mf.mesh.RecalculateBounds();
        mf.mesh.RecalculateTangents();
        mf.mesh.RecalculateNormals();

        return submesh;
    }

    /// <summary>
    /// Splits off a submesh from a mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="submeshIndex"></param>
    /// <returns></returns>
    public static UnityEngine.Mesh DisconnectSubmesh(UnityEngine.Mesh mesh, int submeshIndex)
    {
        UnityEngine.Mesh disconnected = new UnityEngine.Mesh();

        SubMeshDescriptor submesh = mesh.GetSubMesh(submeshIndex);
        //Debug.Log($"first v: {submesh.firstVertex} | v count: {submesh.vertexCount} \n mesh");

        // Remap vertex and uv indices
        Dictionary<int, int> newVertMapping = new Dictionary<int, int>();
        int c = 0;
        int[] triangles = mesh.GetIndices(submeshIndex);
        foreach (int v in triangles)
        {
            if (!newVertMapping.ContainsKey(v))
                newVertMapping.Add(v, c++);
        }

        Vector3[] vertices = new Vector3[c];
        Vector2[] uv = new Vector2[c];
        for (int i = 0; i < triangles.Length; i++)
        {
            vertices[newVertMapping[triangles[i]]] = mesh.vertices[triangles[i]];
            uv[newVertMapping[triangles[i]]] = mesh.uv[triangles[i]];
            triangles[i] = newVertMapping[triangles[i]];
        }
        disconnected.vertices = vertices;
        disconnected.uv = uv;
        disconnected.triangles = triangles;

        disconnected.RecalculateBounds();
        disconnected.RecalculateNormals();
        disconnected.RecalculateTangents();

        return disconnected;
    }

    public struct Mesh
    {
        public string name;
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[] triangles;
        public List<SubMeshDescriptor> submeshes;
        public List<int[]> indices;
        public int submeshCount;
    }

    public class SlicingMesh
    {
        public class Submesh
        {
            public List<Triangle> tris = new List<Triangle>();
            public List<Vector3> vertices = new List<Vector3>();
            public List<Submesh> adjacentSubmeshes = new List<Submesh>();

            public void AddTri(Triangle tri)
            {
                this.tris.Add(tri);
                foreach(Vector3 v in tri.vertices)
                    if(!vertices.Contains(v))
                        vertices.Add(v);
            }

            public void MergeSubmeshes(Submesh other)
            {
                if (this == other) return;
                tris.AddRange(other.tris);
                foreach (Triangle t in other.tris)
                    vertices.AddRange(t.vertices);
            }
        }

        public class Triangle
        {
            public Vector3[] vertices = new Vector3[3];
            public int[] originalVerts = new int[3];
            public Triangle[] neighbours;
            public Vector3 normal;
            public bool inFace = false;

            public Triangle(Mesh m, Vector3 v1, Vector3 v2, Vector3 v3, int a, int b, int c)
            {
                vertices[0] = v1;
                vertices[1] = v2;
                vertices[2] = v3;
                originalVerts[0] = a;
                originalVerts[1] = b;
                originalVerts[2] = c;
                normal = Vector3.Cross(v2 - v1, v3 - v1);
            }
        }

        public string name = "";
        public List<Submesh> subMeshes = new List<Submesh>();
        private Triangle[] meshTris;
        private Mesh originalMesh;
        private Dictionary<Vector3, List<Triangle>> tris = new Dictionary<Vector3, List<Triangle>>();
        private Dictionary<Vector3, List<Submesh>> vertParentSubmeshes = new Dictionary<Vector3, List<Submesh>>();
        private Vector3 averageNormal;

        public SlicingMesh(Mesh mesh)
        {
            name = mesh.name;
            meshTris = new Triangle[mesh.triangles.Length/3];
            originalMesh = mesh;
            int triIndex = 0;
            // Creates a graph of vertices
            for (int t = 0; t < mesh.triangles.Length; t += 3)
            {

                int i1 = mesh.triangles[t + 0];
                int i2 = mesh.triangles[t + 1];
                int i3 = mesh.triangles[t + 2];
                Vector3 v1 = mesh.vertices[i1];
                Vector3 v2 = mesh.vertices[i2];
                Vector3 v3 = mesh.vertices[i3];
                Triangle tri = new Triangle(mesh, v1, v2, v3, i1, i2, i3);

                if (tris.ContainsKey(v1))
                    tris[v1].Add(tri);
                else
                    tris.Add(v1, new List<Triangle>() { tri });
                if (tris.ContainsKey(v2))
                    tris[v2].Add(tri);
                else
                    tris.Add(v2, new List<Triangle>() { tri });
                if (tris.ContainsKey(v3))
                    tris[v3].Add(tri);
                else
                    tris.Add(v3, new List<Triangle>() { tri });

                meshTris[triIndex++] = tri;
            }
            // Find which triangles are next to each other
            foreach (Triangle t in meshTris)
            {
                List<Triangle> potential = new List<Triangle>();
                potential.AddRange(tris[t.vertices[0]]);
                potential.AddRange(tris[t.vertices[1]]);
                potential.AddRange(tris[t.vertices[2]]);
                t.neighbours = potential.Where(t1 => t1.vertices.Intersect(t.vertices).Count() == 2).ToArray();
            }
        }

        public void SliceIntoSubmeshes(float threshold)
        {
            subMeshes = new List<Submesh>();
            Stack<Triangle> stack = new Stack<Triangle>();
            HashSet<Triangle> exists = new HashSet<Triangle>();
            foreach (Triangle t in meshTris)
            {
                if (t.inFace) continue;
                stack.Push(t);
                exists.Add(t);
                Submesh submesh = new Submesh();
                while (stack.Count > 0)
                {
                    Triangle current = stack.Pop();
                    exists.Remove(current);
                    submesh.AddTri(current);
                    current.inFace = true;

                    foreach (Vector3 v in current.vertices)
                    {
                        if (!vertParentSubmeshes.ContainsKey(v))
                            vertParentSubmeshes.Add(v, new List<Submesh>());
                        if (!vertParentSubmeshes[v].Contains(submesh))
                            vertParentSubmeshes[v].Add(submesh);
                    }

                    foreach (Triangle next in current.neighbours)
                    {
                        if (next.inFace) continue;
                        float angle = Vector3.Angle(current.normal, next.normal);
                        if (angle < threshold && !exists.Contains(next))
                        {
                            stack.Push(next);
                            exists.Add(next);
                        }
                    }
                }
                subMeshes.Add(submesh);
            }
        }
        static System.Random r;
        public void CombineNeighboringSubmeshes(float threshold)
        {
            // Find submesh neighbors
            foreach(Submesh submesh in subMeshes)
            {
                foreach (Triangle t in submesh.tris)
                    foreach (Vector3 v in t.vertices)
                        foreach (Submesh s in vertParentSubmeshes[v])
                            if (submesh != s && !submesh.adjacentSubmeshes.Contains(s))
                            {
                                submesh.adjacentSubmeshes.Add(s);
                            }
            }

            // Find submesh triplets
            List<Submesh> triplets = new List<Submesh>();
            foreach (Submesh s2 in subMeshes)
            {
                Dictionary<Submesh, HashSet<Submesh>> ignore = new Dictionary<Submesh, HashSet<Submesh>>();
                foreach (Submesh s1 in s2.adjacentSubmeshes)
                {
                    foreach (Submesh s3 in s2.adjacentSubmeshes)
                        if ((!ignore.ContainsKey(s1) || !ignore[s1].Contains(s3)) && !s1.adjacentSubmeshes.Contains(s3))
                        {
                            if (!ignore.ContainsKey(s3))
                                ignore.Add(s3, new HashSet<Submesh>());
                            ignore[s3].Add(s1);
                            triplets.Add(s1);
                            triplets.Add(s2);
                            triplets.Add(s3);
                        }
                }
            }

            bool[] valid = new bool[triplets.Count / 3];
            for (int i = 0; i < triplets.Count; i += 3)
            {
                // Find the border vertices between 1-2 and 2-3
                List<Vector3> border1, border2;
                border1 = triplets[i + 1].vertices.Where(x => triplets[i].vertices.Contains(x)).ToList();
                border2 = triplets[i + 1].vertices.Where(x => triplets[i + 2].vertices.Contains(x)).ToList();
                if (border1.Count == border2.Count && border1.All(x => border2.Contains(x))) continue;
                //if (r == null)
                //    r = new System.Random();
                //Color c = new Color(r.Next(0, 255) / 255f, r.Next(0, 255) / 255f, r.Next(0, 255) / 255f);
                //Vector3 v = new Vector3(r.Next(5, 10) / 100f, r.Next(5, 10) / 100f, r.Next(5, 10) / 100f);
                //Debug.DrawLine(border1[0] + v, border1[border1.Count - 1] + v, c, 30);
                //Debug.DrawLine(border2[0] + v, border2[border2.Count - 1] + v, c, 30);
                //Debug.DrawLine((border1[0] + border1[border1.Count - 1]) / 2f, (border2[0] + border2[border2.Count - 1]) / 2f, c, 30);

                // Find the closest distance between borders
                float closestDist = float.PositiveInfinity;
                foreach(Vector3 v1 in border1)
                    foreach(Vector3 v2 in border2)
                    {
                        float dist = Vector3.Distance(v1, v2);
                        if (dist < closestDist)
                            closestDist = dist;
                    }

                valid[i / 3] = closestDist < threshold;
                //Debug.Log($"Distance: {closestDist} < {threshold} = {valid[i/3]}");
            }

            for (int i = 0; i < triplets.Count; i += 3)
            {
                if (!valid[i / 3]) continue;
                // Merge submeshes if they are within the treshold
                if(triplets[i + 1] != triplets[i])
                {
                    triplets[i + 1].MergeSubmeshes(triplets[i]);
                    subMeshes.Remove(triplets[i]);

                    Submesh compare = triplets[i];
                    for (int j = 0; j < triplets.Count; j++)
                        if (compare == triplets[j])
                            triplets[j] = triplets[i + 1];
                }
                if(triplets[i + 1] != triplets[i + 2] && triplets[i] != triplets[i + 2])
                {
                    triplets[i + 1].MergeSubmeshes(triplets[i + 2]);
                    subMeshes.Remove(triplets[i + 2]);

                    Submesh compare = triplets[i + 2];
                    for (int j = 0; j < triplets.Count; j++)
                        if (compare == triplets[j])
                            triplets[j] = triplets[i + 1];
                }
            }
        }

        /*public void SliceIntoSubmeshes(float threshold)
        {
            subMeshes = new List<List<Triangle>>();
            
            foreach (Triangle t in meshTris)
            {
                if (t.inFace) continue;
                List<Triangle> face = new List<Triangle>();
                FindFace(t, threshold, face);
                subMeshes.Add(face);
            }
        }

        private void FindFace(Triangle current, float threshold, List<Triangle> face)
        {
            face.Add(current);
            current.inFace = true;
            foreach (Triangle next in current.neighbours)
            {
                if (next.inFace) continue;
                float angle = Vector3.Angle(current.normal, next.normal);
                if (angle < threshold)
                    FindFace(next, threshold, face);
            }
        }*/

        //public UnityEngine.Mesh ToMesh()
        //{
        //    UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        //    mesh.name = name + "(Copy)";
        //    mesh.vertices = new Vector3[0];
        //    mesh.SetUVs(0, new Vector2[0]);
        //    mesh.subMeshCount = 0;
        //    int vertIndexStart = 0;
        //    int triIndexStart = 0;
        //    foreach (List<Triangle> tris in subMeshes)
        //    {
        //        // Populate triangle array with triangles from this face
        //        int[] triangles = new int[tris.Count * 3];
        //        for (int i = 0; i < tris.Count; i++)
        //        {
        //            triangles[i * 3 + 0] = tris[i].originalVerts[0];
        //            triangles[i * 3 + 1] = tris[i].originalVerts[1];
        //            triangles[i * 3 + 2] = tris[i].originalVerts[2];
                    
        //        }

        //        // Remap vertex and uv indices
        //        Dictionary<int, int> newVertMapping = new Dictionary<int, int>();
        //        int c = 0;
        //        foreach (int v in triangles)
        //        {
        //            if (!newVertMapping.ContainsKey(v))
        //                newVertMapping.Add(v, c++);
        //        }
        //        Vector2[] uv = new Vector2[c];
        //        Vector3[] vertices = new Vector3[c];
        //        for (int i = 0; i < triangles.Length; i++)
        //        {
        //            vertices[newVertMapping[triangles[i]]] = originalMesh.vertices[triangles[i]];
        //            uv[newVertMapping[triangles[i]]] = originalMesh.uv[triangles[i]];
        //            triangles[i] = newVertMapping[triangles[i]] + vertIndexStart;
        //        }

        //        // Add to submeshes
        //        SubMeshDescriptor submesh = new SubMeshDescriptor();
        //        submesh.topology = MeshTopology.Triangles;
        //        submesh.firstVertex = 0;
        //        vertIndexStart += vertices.Length;
        //        submesh.vertexCount = vertices.Length;
        //        submesh.indexStart = triIndexStart;
        //        triIndexStart += triangles.Length;
        //        submesh.indexCount = triangles.Length;
        //        mesh.subMeshCount++;
        //        List<Vector3> verts2 = new List<Vector3>();
        //        mesh.GetVertices(verts2);
        //        List<Vector2> uvs = new List<Vector2>();
        //        mesh.GetUVs(0, uvs);
        //        verts2.AddRange(vertices);
        //        mesh.SetVertices(verts2);
        //        uvs.AddRange(uv);
        //        mesh.SetUVs(0, uvs);
        //        mesh.SetIndices(triangles, MeshTopology.Triangles, mesh.subMeshCount-1);
        //        mesh.SetSubMesh(mesh.subMeshCount-1, submesh);
        //    }

        //    return mesh;
        //}

        public Mesh ToMeshStruct()
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = new Vector3[0];
            mesh.uv = new Vector2[0];
            List<int> tempTris = new List<int>();
            mesh.indices = new List<int[]>();
            mesh.submeshes = new List<SubMeshDescriptor>();
            mesh.submeshCount = 0;
            int vertIndexStart = 0;
            int triIndexStart = 0;
            foreach (Submesh smesh in subMeshes)
            {
                // Populate triangle array with triangles from this face
                int[] triangles = new int[smesh.tris.Count * 3];
                for (int i = 0; i < smesh.tris.Count; i++)
                {
                    triangles[i * 3 + 0] = smesh.tris[i].originalVerts[0];
                    triangles[i * 3 + 1] = smesh.tris[i].originalVerts[1];
                    triangles[i * 3 + 2] = smesh.tris[i].originalVerts[2];
                }

                // Remap vertex and uv indices
                Dictionary<int, int> newVertMapping = new Dictionary<int, int>();
                int c = 0;
                foreach (int v in triangles)
                {
                    if (!newVertMapping.ContainsKey(v))
                        newVertMapping.Add(v, c++);
                }
                Vector2[] uv = new Vector2[c];
                Vector3[] vertices = new Vector3[c];
                for (int i = 0; i < triangles.Length; i++)
                {
                    vertices[newVertMapping[triangles[i]]] = originalMesh.vertices[triangles[i]];
                    uv[newVertMapping[triangles[i]]] = originalMesh.uv[triangles[i]];
                    triangles[i] = newVertMapping[triangles[i]] + vertIndexStart;
                }
                tempTris.AddRange(triangles);

                // Add to submeshes
                SubMeshDescriptor submesh = new SubMeshDescriptor();
                submesh.topology = MeshTopology.Triangles;
                submesh.firstVertex = vertIndexStart;
                vertIndexStart += vertices.Length;
                submesh.vertexCount = vertices.Length;
                submesh.indexStart = triIndexStart;
                triIndexStart += triangles.Length;
                submesh.indexCount = triangles.Length;
                mesh.submeshCount++;
                List<Vector3> verts2 = mesh.vertices.ToList();
                List<Vector2> uvs = mesh.uv.ToList();
                verts2.AddRange(vertices);
                mesh.vertices = verts2.ToArray();
                uvs.AddRange(uv);
                mesh.uv = uvs.ToArray();
                mesh.indices.Add(triangles);
                mesh.submeshes.Add(submesh);
            }
            mesh.triangles = tempTris.ToArray();

            return mesh;
        }
    }
}
