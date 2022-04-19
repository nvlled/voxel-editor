

using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;
using System.Diagnostics;

using Camera3D = VoxelEditor.FPCamera;
using System.Collections.Generic;
using System;

namespace VoxelEditor
{

    enum CubeSideType : byte
    {
        TOP,
        LEFT,
        RIGHT,
        FRONT,
        BACK,
        BOTTOM,

    }

    public class SideAxis
    {
        public static (Vector3, Vector3)[] map = new (Vector3, Vector3)[] {
            (new Vector3(0, 0, -1), new Vector3(1, 0, 0)),
            (new Vector3(0, 1, 0), new Vector3(0, 0, -1)),
            (new Vector3(0, 1, 0), new Vector3(0, 0, 1)),
            (new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
            (new Vector3(0, 1, 0), new Vector3(-1, 0, 0)),
            (new Vector3(0, 0, -1), new Vector3(-1, 0, 0)),
        };
    }


    public class VoxelChunk
    {
        public byte drawFlag;
        public int cubeCount;
        public Vector3 position;
    }

    public class VoxelCubeSideItem
    {
        public SubTexture2D subTexture;
        public Vector2 offset;
        public Vector2 scale;
        public float angleRadian;

        public VoxelCubeSideItem(SubTexture2D subtex)
        {
            subTexture = subtex;
            scale = Vector2.One;
        }
    }

    public class VoxelCubeSide
    {
        public VoxelCubeSideItem body;
        public List<VoxelCubeSideItem> decorations; // TODO:

        public VoxelCubeSide(SubTexture2D subtex)
        {
            body = new VoxelCubeSideItem(subtex);
        }
    }

    public class SerializedCube
    {
        (int, int, int) position;
        string tileName;
        public SerializedCube(string tileName, Vector3 pos)
        {
            position = ((int)pos.X, (int)pos.Y, (int)pos.Z);
            this.tileName = tileName;
        }
    }

    public class SerializedWorld
    {
        public SerializedCube[] data;
        public Vector3 minBounds;
        public Vector3 maxBounds;
        public Ray cameraRay;
    }

    public class VoxelCube
    {
        public string tileName;

        // Cube side must be drawn both sides if not opaque
        public bool opaque;

        // All sides have the same texture
        public bool uniform;

        // CubeSide -> VoxelCubeSide
        public VoxelCubeSide[] data;

        public byte drawFlag;

        public VoxelCube(string tileName, SubTexture2D[] subTextures)
        {
            this.tileName = tileName; // TODO: wouldn't this bloat memory usage?
            data = new VoxelCubeSide[6];
            uniform = subTextures.Length <= 1;
            opaque = true;
            for (var i = 0; i < 6; i++)
            {
                data[i] = new VoxelCubeSide(subTextures[i % subTextures.Length]);
            }
        }

        public VoxelCube(string tileName, SubTexture2D subtex)
        {
            this.tileName = tileName;
            data = new VoxelCubeSide[6];
            data[0] = new VoxelCubeSide(subtex);
            uniform = true;
            opaque = true;
        }
    }

    public unsafe class VoxelWorld
    {
        public int renderDistance = Config.renderDistance;
        public int chunkSize = Config.chunkSize;

        public AtlasManager atlasManager;

        public VoxelCube[] data;
        public VoxelChunk[] chunks;
        public byte drawFlag;
        public int sidesDrawn = 0;

        public List<Entity> entities;
        public Vector3 minBounds;
        public Vector3 maxBounds;
        public Vector3 minChunkBounds;
        public Vector3 maxChunkBounds;
        public Vector3 size;

        public int iterations;

        public Camera3D cam;
        public Player player;


        Model modelB = rl.LoadModelFromMesh(rl.GenMeshCube(1.0f, 1.0f, 1.0f));
        Texture2D modelTexture = rl.LoadTexture("assets/texel_checker.png");



        public VoxelWorld(int size) : this(size, size, size)
        { }

        public VoxelWorld(int xSize, int ySize, int zSize) : this(new Vector3(-xSize, -ySize, -zSize), new Vector3(xSize, ySize, zSize)) { }

        public VoxelWorld(Vector3 min, Vector3 max)
        {
            minBounds = min;
            maxBounds = max;
            minChunkBounds = Voxel.GetChunkPosition(chunkSize, min + Vector3.One);
            maxChunkBounds = Voxel.GetChunkPosition(chunkSize, max + Vector3.One);
            size = max - min;

            var dataSize = (int)size.X * (int)size.Y * (int)size.Z;
            System.Diagnostics.Debug.Assert(dataSize < Math.Pow(2, 31));
            data = new VoxelCube[dataSize];
            chunks = new VoxelChunk[dataSize / chunkSize];

            modelB.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].texture = modelTexture;
        }

        public VoxelChunk GetVoxelChunk(Vector3 chunkPos)
        {
            var chunkIndex = toChunkIndex(chunkPos);
            if (chunkIndex < 0 || chunkIndex >= chunks.Length)
            {
                return null;
            }
            return chunks[chunkIndex];
        }
        public int toChunkIndex(Vector3 chunkPos)
        {
            return toIndex(chunkPos * chunkSize) / chunkSize;
        }
        public int toIndex(Vector3 p)
        {
            p = new Vector3(
                rm.Remap(p.X, minBounds.X, maxBounds.X, 0, size.X),
                rm.Remap(p.Y, minBounds.Y, maxBounds.Y, 0, size.Y),
                rm.Remap(p.Z, minBounds.Z, maxBounds.Z, 0, size.Z)
            );
            var index = (int)((p.X + 1) * size.Y * size.Z + (p.Y + 1) * size.Z + p.Z);
            return index;
        }
        Vector3 fromIndex(int index)
        {
            var pos = new Vector3
            {
                X = (int)index / (size.Y * size.Z),
                Y = ((int)index / size.Z) % size.Y,
                Z = (int)index % size.Z,
            };
            return pos + minBounds;
        }

        // TODO: save/load world state
        // TODO: physics/collision detection
        //       - block detection
        //       - restrict within boundary
        //       - gravity
        // TODO: tile switcher

        public void DrawBoundaryPlane(Color color, params Vector3[] ps)
        {
            R.DrawPlane(ps[0], ps[1], ps[2], ps[3], color);

            var r = 0.5f;
            var c = Color.RED;
            for (var i = 0; i < ps.Length; i++)
            {
                rl.DrawCylinderEx(ps[i], ps[(i + 1) % ps.Length], r, r, 5, c);
            }
        }

        public void DrawBoundaries()
        {
            var min = Voxel.AlignPosition(minBounds) + Vector3.One * -0.51f;
            var max = Voxel.AlignPosition(maxBounds) + Vector3.One * 0.51f;

            var p1 = min;
            var p2 = min + size * Vector3.UnitZ;
            var p3 = min + size * Vector3.UnitX;
            var p4 = min + size * Vector3.UnitX + size * Vector3.UnitZ;
            var p5 = min + size * Vector3.UnitY;
            var p6 = min + size * Vector3.UnitY + size * Vector3.UnitZ;
            var p7 = min + size * Vector3.UnitY + size * Vector3.UnitX;
            var p8 = min + size;


            var c = Color.DARKBLUE;
            DrawBoundaryPlane(c, p1, p3, p7, p5);
            DrawBoundaryPlane(c, p3, p7, p8, p4);
            DrawBoundaryPlane(c, p4, p2, p6, p8);
            DrawBoundaryPlane(c, p2, p6, p5, p1);
            DrawBoundaryPlane(c, p1, p2, p3, p4);
            DrawBoundaryPlane(c, p5, p6, p7, p8);
        }

        public void DrawInfo(VoxelWorld world)
        {
            rl.DrawText(string.Format("{0}", Voxel.AlignPosition(cam.ray.position)), 10, 10, 22, Color.BLACK);
            rl.DrawText(string.Format("{0}", Voxel.GetChunkPosition(world.chunkSize, cam.ray.position)), 10, 30, 22, Color.BLACK);
        }

        public void Draw(Camera3D cam)
        {
            // switch draw flag to render a cube only once per frame
            drawFlag = (byte)((drawFlag + 1) % 255);

            DrawBoundaries();
            DrawRaycastedCubes(cam.ray);
        }

        public void Update()
        {
        }

        public IEnumerable<Vector3> IterateRayCastedChunks(Ray ray)
        {
            var (right, up) = R.GetOrthogonalAxis(ray.direction, cam.Camera.up);
            var size = R.GetFarsideDimension(cam.GetFOVX() + 5, renderDistance + 0);
            var center = Voxel.AlignPosition(ray.position) + ray.direction * renderDistance;

            foreach (var v in R.IterateOutwards((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y) + 0, chunkSize))
            {
                var dir = Vector3.Normalize((center + right * v.X + up * v.Y) - ray.position);
                var unAlignedChunkPos = (ray.position + dir) / chunkSize;

                for (var d = (int)0; d <= renderDistance; d += chunkSize)
                {
                    var chunkPos = Voxel.AlignChunkPosition(unAlignedChunkPos);
                    if (ChunkOutBounds(chunkPos))
                    {
                        continue;
                    }

                    yield return chunkPos;

                    unAlignedChunkPos = Voxel.NextChunkPos(chunkSize, unAlignedChunkPos, dir);
                }
            }

        }

        public Vector3[] GetRayCastedChunks(Ray ray)
        {
            var result = new List<Vector3>();

            foreach (var chunkPos in IterateRayCastedChunks(ray))
            {
                var data = GetVoxelChunk(chunkPos);
                if (data == null)
                {
                    continue;
                }
                result.Add(chunkPos);
            }

            return result.ToArray();
        }

        public void DrawRaycastedCubes(Ray ray)
        {
            var center = cam.ray.position + cam.ray.direction * renderDistance;
            iterations = 0;
            sidesDrawn = 0;
            foreach (var chunkPos in IterateRayCastedChunks(ray))
            {
                var chunkIndex = toChunkIndex(chunkPos);

                if (chunkIndex < 0 || chunkIndex >= chunks.Length)
                {
                    continue;
                }
                if (chunks[chunkIndex] == null)
                {
                    continue;
                }
                if (chunks[chunkIndex].drawFlag == drawFlag)
                {
                    continue;
                }
                if (chunks[chunkIndex].cubeCount == 0)
                {
                    continue;
                }

                chunks[chunkIndex].drawFlag = drawFlag;


                var batchSize = 1000;
                var drawnCubes = 0;

                Voxel.StartDrawTile(batchSize);
                // TODO: optmize by project p to the camera axis
                //       skip next chunk in the ray if a whole "wall" is formed
                //       also skip next cube "wall" 
                foreach (var p in IterateChunk(chunkSize, chunkPos))
                {
                    if (OutBounds(p))
                    {
                        continue;
                    }
                    if (DrawCube(p))
                    {
                        drawnCubes++;
                    }

                    if (drawnCubes >= chunks[chunkIndex].cubeCount)
                    {
                        //Console.WriteLine("end chunk: {0}", chunkIndex);
                        break;
                    }

                    if ((iterations++ % batchSize) == 0)
                    {
                        iterations = 0;
                        Voxel.EndDrawTile();
                        Voxel.StartDrawTile(batchSize);
                    }
                }
                Voxel.EndDrawTile();
            }

        }

        public IEnumerable<Vector3> IterateChunk(int chunkSize, Vector3 chunkPos)
        {
            var p1 = chunkPos * chunkSize;
            var p2 = p1 + new Vector3(1, 1, 1) * (chunkSize - 1);
            var p3 = p1 + new Vector3(1, 0, 0) * (chunkSize - 1);
            var p4 = p1 + new Vector3(0, 0, 1) * (chunkSize - 1);
            var p5 = p1 + new Vector3(0, 1, 0) * (chunkSize - 1);
            var p6 = p1 + new Vector3(1, 0, 1) * (chunkSize - 1);
            var p7 = p1 + new Vector3(1, 1, 0) * (chunkSize - 1);
            var p8 = p1 + new Vector3(0, 1, 1) * (chunkSize - 1);
            var n = Math.Ceiling(chunkSize / 2f);
            for (var x = 0; x < n; x++)
            {
                for (var y = 0; y < n; y++)
                {
                    for (var z = 0; z < n; z++)
                    {
                        yield return new Vector3(p1.X + x, p1.Y + y, p1.Z + z);
                        yield return new Vector3(p2.X - x, p2.Y - y, p2.Z - z);
                        yield return new Vector3(p3.X - x, p3.Y + y, p3.Z + z);
                        yield return new Vector3(p4.X + x, p4.Y + y, p4.Z - z);
                        yield return new Vector3(p5.X + x, p5.Y - y, p5.Z + z);
                        yield return new Vector3(p6.X - x, p6.Y + y, p6.Z - z);
                        yield return new Vector3(p7.X - x, p7.Y - y, p7.Z + z);
                        yield return new Vector3(p8.X + x, p8.Y - y, p8.Z - z);
                    }
                }

            }
        }



        public IEnumerable<(float, float, float)> IterateOutwards(int radius)
        {
            yield return (0, 0, 0);
            var inc = 1.0f;
            for (var n = 1f; n <= radius; n += inc)
            {
                for (var a = -n; a <= n; a++)
                {
                    yield return (n, n, a);
                    yield return (n, -n, a);
                    yield return (n, a, n);
                    yield return (n, a, -n);
                }
            }
        }

        public bool DrawCube(Vector3 position)
        {
            var index = toIndex(position);
            if (index < 0 || index >= data.Length)
            {
                return false;
            }

            var cube = data[index];
            if (cube == null || cube.drawFlag == drawFlag)
            {
                return false;
            }

            cube.drawFlag = drawFlag;

            DrawCube(position, cube);
            return true;
        }


        public void DrawCube(Vector3 position, VoxelCube cube)
        {
            // TODO: decorations
            // TODO: draw on both sides if not opaque
            var numSides = 6;
            var uniformSide = cube.data[0];
            for (var index = 0; index < numSides; index++)
            {

                var useUniformSide = cube.uniform || index >= cube.data.Length;
                var side = useUniformSide ? uniformSide : cube.data[index];
                var sideType = index;
                var (texture, region) = side.body.subTexture;


                if (texture.id == 0)
                {
                    Voxel.DrawCube(position, Color.DARKBLUE);
                    Voxel.DrawCubeWires(position, Color.GREEN);
                }
                else
                {
                    var (up, right) = SideAxis.map[sideType];
                    var normal = Vector3.Cross(up, right);
                    var dot = Vector3.Dot(cam.ray.direction, normal);
                    if (dot < -0.5000000f && !Config.drawAll)
                    {
                        continue;
                    }
                    var dataIndex = toIndex(position + -normal);
                    if (dataIndex >= 0 && dataIndex < data.Length && data[dataIndex] != null && !Config.drawAll)
                    {
                        continue;
                    }
                    Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: (CubeSideType)sideType, sizeArg: side.body.scale);
                    sidesDrawn++;
                }
            }
        }

        public void InsertCube(Vector3 position, VoxelCube cube)
        {

        }
        public void InsertCube(Vector3 position, VoxelCubeSideItem side)
        {

        }

        public bool ChunkOutBounds(Vector3 chunkPos)
        {
            var min = minBounds / chunkSize;
            var max = maxBounds / chunkSize;
            return false;
        }

        public bool OutBounds(Vector3 pos)
        {
            var min = minBounds;
            var max = maxBounds;
            return pos.X < min.X ||
                pos.Y < min.Y ||
                pos.Z < min.Z ||
                pos.X >= max.X ||
                pos.Y >= max.Y ||
                pos.Z >= max.Z;
        }


        public void InsertCube(Vector3 pos, string tileName)
        {
            pos = Voxel.AlignPosition(pos);
            var index = toIndex(pos);
            if (OutBounds(pos))
            {
                return;
            }

            if (index < 0 || index >= data.Length)
            {
                return;
            }
            var newCube = data[index] == null;

            var subTextures = atlasManager.Lookup(tileName);
            System.Diagnostics.Debug.Assert(subTextures.Length > 0, "tile name must be valid");
            data[index] = new VoxelCube(tileName, subTextures);

            if (newCube)
            {
                IncrementCubeCount(Voxel.GetChunkPosition(chunkSize, pos));
            }

        }

        public void RemoveCube(Vector3 position)
        {
            var chunkIndex = toChunkIndex(position);
            DecrementCubeCount(chunkIndex);
        }

        public void IncrementCubeCount(Vector3 chunkPos)
        {
            var chunkIndex = toChunkIndex(chunkPos);
            if (chunkIndex < 0 || chunkIndex >= chunks.Length)
            {
                return;
            }
            if (chunks[chunkIndex] == null)
            {
                chunks[chunkIndex] = new VoxelChunk();
                chunks[chunkIndex].position = chunkPos;
            }
            if (chunks[chunkIndex].cubeCount >= chunkSize * chunkSize * chunkSize)
            {
                return;
            }
            chunks[chunkIndex].cubeCount++;
            System.Diagnostics.Debug.Assert(
                chunks[chunkIndex].cubeCount <= chunkSize * chunkSize * chunkSize,
                string.Format("cube count must not exceed chunk size: count={0} chunkSize={1}", chunks[chunkIndex].cubeCount, chunkSize * chunkSize * chunkSize)
            );
        }

        public void DecrementCubeCount(int chunkIndex)
        {
            if (chunkIndex < 0 || chunkIndex >= chunks.Length)
            {
                return;
            }
            if (chunks[chunkIndex] == null)
            {
                return;
            }
            if (chunks[chunkIndex].cubeCount <= 0)
            {
                return;
            }
            var chunk = chunks[chunkIndex];
            if (chunk.cubeCount > 0)
            {
                chunks[chunkIndex].cubeCount = chunk.cubeCount - 1;
            }
        }

        // TODO:
        public void Deserialize(SerializedWorld worldData)
        { }

        public SerializedWorld Serlialize()
        {
            var worldData = new SerializedWorld();
            var cubes = new List<SerializedCube>();
            foreach (var chunk in chunks)
            {
                if (chunk == null)
                {
                    continue;
                }
                if (chunk.cubeCount == 0)
                {
                    continue;
                }
                foreach (var cubePos in IterateChunk(chunkSize, chunk.position))
                {
                    var i = toIndex(cubePos);
                    if (i <= 0 || i >= data.Length)
                    {
                        continue;
                    }
                    var cube = data[i];
                    if (cube == null)
                    {
                        continue;
                    }
                    cubes.Add(new SerializedCube(cube.tileName, cubePos));
                }
            }

            worldData.data = cubes.ToArray();
            worldData.cameraRay = cam.ray;
            worldData.minBounds = minBounds;
            worldData.maxBounds = maxBounds;

            return worldData;
        }
    }

    class CubeQuadSide
    {
        public Vector3 p1;
        public Vector3 p2;
        public Vector3 p3;
        public Vector3 p4;
        public Vector3 normal;
    }

    class Voxel
    {
        public static Vector3 vec(float x, float y, float z) { return new Vector3(x, y, z); }
        public static CubeQuadSide[] CubeCorners = new CubeQuadSide[]
        {
            new CubeQuadSide() { p4=vec(0, 0, 0), p3=vec(1, 0, 0), p2=vec(1, 1, 0), p1=vec(0, 1, 0),normal=new Vector3(0, 0, -1)},
            new CubeQuadSide() { p4=vec(0, 0, 0), p3=vec(0, 1, 0), p2=vec(0, 1, 1), p1=vec(0, 0, 1),normal=new Vector3(-1, 0, 0)},
            new CubeQuadSide() { p4=vec(0, 0, 0), p3=vec(0, 0, 1), p2=vec(1, 0, 1), p1=vec(1, 0, 0),normal=new Vector3(0, -1, 0)},

            new CubeQuadSide() { p4=vec(1, 1, 1), p3=vec(1, 1, 0), p2=vec(1, 0, 0), p1=vec(1, 0, 1),normal=new Vector3(1, 0, 0)},
            new CubeQuadSide() { p4=vec(1, 1, 1), p3=vec(1, 0, 1), p2=vec(0, 0, 1), p1=vec(0, 1, 1),normal=new Vector3(0, 0, 1)},
            new CubeQuadSide() { p4=vec(1, 1, 1), p3=vec(0, 1, 1), p2=vec(0, 1, 0), p1=vec(1, 1, 0),normal=new Vector3(0, 1, 0)},
        };

        public static float Angle(Vector3 v, Vector3 w)
        {
            return MathF.Acos(Vector3.Dot(v, w) / (v.Length() * w.Length()));
        }

        public static Vector3 GetOrthonormal(Vector3 v)
        {
            if (v.Length() == 0)
            {
                return Vector3.UnitX;
            }

            var x = MathF.Abs(v.X);
            var y = MathF.Abs(v.Y);
            var z = MathF.Abs(v.Z);
            if (x >= y && x >= z)
            {
                return new Vector3(MathF.Sign(v.X), 0, 0);
            }
            if (y >= x && y >= z)
            {
                return new Vector3(0, MathF.Sign(v.Y), 0);
            }
            if (z >= x && z >= y)
            {
                return new Vector3(0, 0, MathF.Sign(v.Z));
            }

            return new Vector3(MathF.Sign(v.X), 0, 0);
        }

        public static Vector3 GetIntersectedSide(Vector3 pos, Vector3 dir)
        {

            var v = dir * 0.1f;
            var originalPos = pos;
            while (R.InCube(Vector3.Zero, pos + v))
            {
                pos += v;
            }
            pos += v;

            if (pos.X < 0)
            {
                return new Vector3(-1, 0, 0);
            }
            if (pos.Y < 0)
            {
                return new Vector3(0, -1, 0);
            }
            if (pos.Z < 0)
            {
                return new Vector3(0, 0, -1);
            }

            if (pos.X >= 1)
            {
                return new Vector3(1, 0, 0);
            }
            if (pos.Y >= 1)
            {
                return new Vector3(0, 1, 0);
            }
            if (pos.Z >= 1)
            {
                return new Vector3(0, 0, 1);
            }


            Debug.Fail("should not happen");
            return Vector3.Zero;
        }


        public static Vector3 NextChunkPos(int chunkSize, Vector3 unAlignedChunkPos, Vector3 dir)
        {
            var chunkPos = Voxel.AlignChunkPosition(unAlignedChunkPos);
            var v = unAlignedChunkPos - chunkPos;
            var n = GetIntersectedSide(v, dir);
            var isPositive = n.X + n.Y + n.Z > 0;
            var u = isPositive ? n - n * v : n * v;

            if (u.Length() > 0)
                Debug.Assert((n * v).Length() + (n - n * v).Length() == 1);

            var angleBetween = Angle(dir, n);
            var len = (angleBetween == 0 ? u.Length() : u.Length() / MathF.Cos(angleBetween));
            var nextPos = unAlignedChunkPos + dir * (len + 0.01f);

            Debug.Assert(!R.InCube(chunkPos, nextPos));
            Debug.Assert(!chunkPos.Equals(Voxel.AlignChunkPosition(nextPos)));

            return nextPos;
        }

        public static Vector3[] GetChunkVertices(int chunkSize, Vector3 pos)
        {
            var x = Vector3.UnitX * chunkSize;
            var y = Vector3.UnitY * chunkSize;
            var z = Vector3.UnitZ * chunkSize;
            return new Vector3[] {
                pos,
                pos + x,
                pos + z,
                pos + x+z,
                pos + y,
                pos + y + x,
                pos + y + z,
                pos + y + x+z
            };
        }

        public static Vector3 GetChunkPosition(int chunkSize, Vector3 pos)
        {
            pos.X = MathF.Floor(pos.X / chunkSize);
            pos.Y = MathF.Floor(pos.Y / chunkSize);
            pos.Z = MathF.Floor(pos.Z / chunkSize);
            return pos;
        }
        public static Vector3[] GetChunkPositions(int chunkSize, Vector3 pos)
        {
            return new Vector3[] {
            new Vector3(MathF.Floor(pos.X / chunkSize), MathF.Floor(pos.Y / chunkSize), MathF.Floor(pos.Z / chunkSize)),
            new Vector3(MathF.Ceiling(pos.X / chunkSize), MathF.Ceiling(pos.Y / chunkSize), MathF.Ceiling(pos.Z / chunkSize))
            };
        }

        public static Vector3[] AlignPositions(Vector3 position)
        {
            return new Vector3[] {
                new Vector3(
                    (float)System.Math.Floor(position.X),
                    (float)System.Math.Floor(position.Y),
                    (float)System.Math.Floor(position.Z)
                ),
                new Vector3(
                    (float)System.Math.Ceiling(position.X),
                    (float)System.Math.Ceiling(position.Y),
                    (float)System.Math.Ceiling(position.Z)
                )
            };
        }
        public static Vector3 AlignChunkPosition(Vector3 position)
        {

            position.X = System.MathF.Floor(position.X);
            position.Y = System.MathF.Floor(position.Y);
            position.Z = System.MathF.Floor(position.Z);
            return position;
        }

        public static Vector3 AlignPosition(Vector3 position)
        {
            position.X = System.MathF.Round(position.X);
            position.Y = System.MathF.Round(position.Y);
            position.Z = System.MathF.Round(position.Z);
            return position;
        }
        public static void DrawCube(Vector3 pos, Color color)
        {
            var offset = 0.0f;
            pos = AlignPosition(pos);
            rl.DrawCube(new Vector3(pos.X + offset, pos.Y + offset, pos.Z + offset), 1, 1, 1, color);
        }
        public static void DrawCubeWires(Vector3 pos, Color color)
        {
            var offset = 0.0f;
            pos = AlignPosition(pos);
            rl.DrawCubeWires(new Vector3(pos.X + offset, pos.Y + offset, pos.Z + offset), 1, 1, 1, color);
        }
        public static void DrawSurroundingCubeWires(Vector3 position, Color color)
        {
            position.X = (float)System.Math.Floor(position.X);
            position.Y = (float)System.Math.Floor(position.Y);
            position.Z = (float)System.Math.Floor(position.Z);
            for (var x = -1.0f; x <= 1; x++)
            {
                for (var y = -1.0f; y <= 1; y++)
                {
                    for (var z = -1.0f; z <= 1; z++)
                    {
                        DrawCubeWires(position + new Vector3(x, y, z), Color.GREEN);
                    }
                }

            }
        }


        public static void StartDrawTile(int batchSize)
        {
            gl.rlCheckRenderBatchLimit(4 * batchSize);
            gl.rlBegin(DrawMode.QUADS);
        }
        public static void EndDrawTile()
        {

            gl.rlEnd();
            gl.rlSetTexture(0);
        }

        public static void DrawCube(Vector3 position, SubTexture2D subTexture, Color color)
        {
            var numSides = 6;
            for (var index = 0; index < numSides; index++)
            {

                var sideType = index;
                var (texture, region) = subTexture;

                if (texture.id == 0)
                {
                    Voxel.DrawCube(position, Color.DARKBLUE);
                    Voxel.DrawCubeWires(position, Color.GREEN);
                }
                else
                {
                    var (up, right) = SideAxis.map[sideType];
                    var normal = Vector3.Cross(up, right);
                    Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: (CubeSideType)sideType, colorArg: color);
                }
            }
        }

        public static void DrawTile(Vector3 cubePos, ref Texture2D texture, ref Rectangle source, CubeSideType side, Vector2? sizeArg = null, Vector2? originArg = null, float rotation = 0, Color? colorArg = null)
        {
            var up = Vector3.Zero;
            var right = Vector3.Zero;
            switch (side)
            {
                case CubeSideType.TOP:
                    up = new Vector3(0, 0, -1);
                    right = new Vector3(1, 0, 0);
                    cubePos.Z += 0;
                    cubePos.Y += 0.5f;
                    cubePos.X += 0;
                    break;
                case CubeSideType.BOTTOM:
                    up = new Vector3(0, 0, -1);
                    right = new Vector3(-1, 0, 0);
                    cubePos.Z += 0.0f;
                    cubePos.Y += -0.5f;
                    cubePos.X += 0.0f;
                    break;
                case CubeSideType.FRONT:
                    up = new Vector3(0, 1, 0);
                    right = new Vector3(1, 0, 0);
                    cubePos.Z += 0.5f;
                    cubePos.Y += 0;
                    cubePos.X += 0;
                    break;
                case CubeSideType.BACK:
                    up = new Vector3(0, 1, 0);
                    right = new Vector3(-1, 0, 0);
                    cubePos.Z += -0.5f;
                    cubePos.Y += 0;
                    cubePos.X += 0;
                    break;
                case CubeSideType.RIGHT:
                    up = new Vector3(0, 1, 0);
                    right = new Vector3(0, 0, 1);
                    cubePos.Z += 0;
                    cubePos.Y += 0;
                    cubePos.X += -0.5f;
                    break;
                case CubeSideType.LEFT:
                    up = new Vector3(0, 1, 0);
                    right = new Vector3(0, 0, -1);
                    cubePos.Z += 0;
                    cubePos.Y += 0;
                    cubePos.X += 0.5f;
                    break;
            }

            var size = sizeArg ?? Vector2.One;
            var sizeRatio = new Vector2(size.Y, size.X * (float)source.height / source.width);
            var origin = originArg.GetValueOrDefault(Vector2.Zero);

            var rightScaled = rm.Vector3Scale(right, sizeRatio.X / 2);
            var upScaled = rm.Vector3Scale(up, sizeRatio.Y / 2);

            var p1 = rm.Vector3Add(rightScaled, upScaled);
            var p2 = rm.Vector3Subtract(rightScaled, upScaled);

            var topLeft = rm.Vector3Scale(p2, -1);
            var topRight = p1;
            var bottomRight = p2;
            var bottomLeft = rm.Vector3Scale(p1, -1);


            // Translate points to the draw center (position)
            topLeft = rm.Vector3Add(topLeft, cubePos);
            topRight = rm.Vector3Add(topRight, cubePos);
            bottomRight = rm.Vector3Add(bottomRight, cubePos);
            bottomLeft = rm.Vector3Add(bottomLeft, cubePos);
            var color = colorArg.GetValueOrDefault(Color.WHITE);

            gl.rlColor4ub(color.r, color.g, color.b, color.a);

            gl.rlSetTexture(texture.id);


            // Bottom-left corner for texture and quad
            gl.rlTexCoord2f((float)source.x / texture.width, (float)source.y / texture.height);
            gl.rlVertex3f(topLeft.X, topLeft.Y, topLeft.Z);

            // Top-left corner for texture and quad
            gl.rlTexCoord2f((float)source.x / texture.width, (float)(source.y + source.height) / texture.height);
            gl.rlVertex3f(bottomLeft.X, bottomLeft.Y, bottomLeft.Z);

            // Top-right corner for texture and quad
            gl.rlTexCoord2f((float)(source.x + source.width) / texture.width, (float)(source.y + source.height) / texture.height);
            gl.rlVertex3f(bottomRight.X, bottomRight.Y, bottomRight.Z);

            // Bottom-right corner for texture and quad
            gl.rlTexCoord2f((float)(source.x + source.width) / texture.width, (float)source.y / texture.height);
            gl.rlVertex3f(topRight.X, topRight.Y, topRight.Z);

        }
    }
}