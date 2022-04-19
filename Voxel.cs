

using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;

using Camera3D = VoxelEditor.FPCamera;
using System.Collections.Generic;
using System;
using System.Linq;

namespace VoxelEditor
{
	enum DrawFlag : byte
	{
		Init,
		A,
		B
	}

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
		/*
					( new Vector3(0, 0, -1), new Vector3(1, 0, 0)),
					 (new Vector3(0, 1, 0), new Vector3(0, 0, -1)),
					 (new Vector3(0, 1, 0), new Vector3(0, 0, 1)),
					 (new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
					 (new Vector3(0, 1, 0), new Vector3(-1, 0, 0)),
					 (new Vector3(0, 0, -1), new Vector3(-1, 0, 0)),
		*/
		public static (Vector3, Vector3)[] map = new (Vector3, Vector3)[] {
			// new Vector3(1, 0, -1),
			// new Vector3(0, 1, -1),
			// new Vector3(0, 1, 1),
			// new Vector3(1, 1, 0),
			// new Vector3(-1, 1, 0),
			// new Vector3(-1, 0, -1),
			(new Vector3(0, 0, -1), new Vector3(1, 0, 0)),
			(new Vector3(0, 1, 0), new Vector3(0, 0, -1)),
			(new Vector3(0, 1, 0), new Vector3(0, 0, 1)),
			(new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
			(new Vector3(0, 1, 0), new Vector3(-1, 0, 0)),
			(new Vector3(0, 0, -1), new Vector3(-1, 0, 0)),
		};
	}


	class VoxelChunk
	{
		public DrawFlag drawFlag;
		public int cubeCount;
		public Vector3 position;
	}

	class VoxelCubeSideItem
	{
		//public string tileName;

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

	class VoxelCubeSide
	{
		public VoxelCubeSideItem body;
		public List<VoxelCubeSideItem> decorations; // TODO:

		public VoxelCubeSide(SubTexture2D subtex)
		{
			body = new VoxelCubeSideItem(subtex);
		}
	}


	class VoxelCube
	{
		// Cube side must be drawn both sides if not opaque
		public bool opaque;

		// All sides have the same texture
		public bool uniform;

		// CubeSide -> VoxelCubeSide
		public VoxelCubeSide[] data;

		public DrawFlag drawFlag;

		public VoxelCube(SubTexture2D subtex)
		{
			data = new VoxelCubeSide[6];
			data[0] = new VoxelCubeSide(subtex);
			uniform = true;
			opaque = true;
		}
	}

	// Okay, unlike most voxel world implementations,
	// mine isn't going to be vast or infinite world,
	// so that alone will simplify a lot of things for me,
	// and I don't have to worry about using fancy datastructures
	// or optimizations. Or at most, I would use spatial hash.
	unsafe class VoxelWorld
	{
		public int renderDistance = Config.renderDistance;
		public int chunkSize = Config.chunkSize;

		public AtlasManager atlasManager;

		public VoxelCube[] data;
		public VoxelChunk[] chunks;
		public DrawFlag drawFlag;
		public int sidesDrawn = 0;

		public List<Entity> entities;
		public Vector3 minBounds;
		public Vector3 maxBounds;
		public Vector3 minChunkBounds;
		public Vector3 maxChunkBounds;
		public Vector3 size;

		public int iterations;

		public Camera3D cam;


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
			//drawBuffer = new byte[dataSize];
			//drawChunkBuffer = new byte[dataSize / chunkSize];

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
			var p = chunkPos;
			var s = Voxel.GetChunkPosition(chunkSize, size);
			p = new Vector3(
				MathF.Floor(rm.Remap(p.X, minChunkBounds.X, maxChunkBounds.X, 0, size.X)),
				MathF.Floor(rm.Remap(p.Y, minChunkBounds.Y, maxChunkBounds.Y, 0, size.Y)),
				MathF.Floor(rm.Remap(p.Z, minChunkBounds.Z, maxChunkBounds.Z, 0, size.Z))
			);

			var index = (int)((p.X + 1) * s.Y * s.Z + (p.Y + 1) * s.Z + p.Z);
			return index;
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
			//R.DrawPlane(p3, p7, p8, p4, new Color(50, 50, 50, 255));
			//R.DrawPlane(p4, p2, p6, p8, new Color(50, 50, 50, 255));
			//R.DrawPlane(p2, p6, p5, p1, new Color(50, 50, 50, 255));

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
			drawFlag = drawFlag == DrawFlag.A ? DrawFlag.B : DrawFlag.A;

			DrawBoundaries();
			DrawRaycastedCubes(cam.ray);

			// switch draw flag to render a cube only once per frame
		}

		public IEnumerable<Vector3> IterateRayCastedChunks(Ray ray)
		{
			var (right, up) = R.GetOrthogonalAxis(ray.direction, cam.Camera.up);
			var size = R.GetFarsideDimension(cam.GetFOVX(), renderDistance + 0);
			var center = Voxel.AlignPosition(ray.position) + ray.direction * renderDistance;

			// TODO: remove added
			var added = new Dictionary<int, Vector3>();
			foreach (var v in R.IterateOutwards((int)Math.Ceiling(size.X + 1), (int)Math.Ceiling(size.Y + 1), chunkSize))
			{
				var dir = Vector3.Normalize((center + right * v.X + up * v.Y) - Voxel.AlignPosition(ray.position));
				for (var d = 0.5f; d <= renderDistance; d += chunkSize * 0.5f)
				{

					var q = Voxel.AlignPosition(ray.position) + dir * d;
					var chunkPos = Voxel.GetChunkPosition(chunkSize, q);
					//foreach (var chunkPos in Voxel.GetChunkPositions(chunkSize, q))
					{
						if (ChunkOutBounds(chunkPos))
						{
							continue;
						}

						// I knew it, this is broken
						// <1, -1, 1> <0, 1, 1>
						// same index 126
						// TODO: make sure toIndex and toChunkIndex is not broken
						var chunkIndex = toChunkIndex(chunkPos);
						/*
						*/
						if (added.ContainsKey(chunkIndex))
						{
							Vector3 p;
							if (added.TryGetValue(chunkIndex, out p))
							{
								if (!p.Equals(chunkPos))
								{
									toChunkIndex(chunkPos);
									Console.WriteLine("huh: {0} {1}", chunkPos, p);
								}
							}

							continue;
						}
						added.Add(chunkIndex, chunkPos);
						yield return chunkPos;
					}
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
					//continue;
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
				// TODO: drawCubeMesh around chunk

				var batchSize = 5000;
				var drawnCubes = 0;
				Voxel.StartDrawTile(batchSize);

				// TODO: optmize by project p to the camera axis
				//       skip next chunk in the ray if a whole "wall" is formed
				//       also skip next cube "wall" 
				foreach (var p in IterateChunk(chunkSize, chunkPos))
				{
					if (DrawCube(p))
					{
						drawnCubes++;
					}

					if (drawnCubes > chunks[chunkIndex].cubeCount)
					{
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
			Console.WriteLine("sides drawn: {0}", sidesDrawn);

		}

		public IEnumerable<Vector3> IterateChunk(int chunkSize, Vector3 chunkPos)
		{
			// TODO: can be optimized by iterating from two opposite corners of the chunk
			for (var x = 0; x < chunkSize; x++)
			{
				for (var y = 0; y < chunkSize; y++)
				{
					for (var z = 0; z < chunkSize; z++)
					{
						yield return new Vector3(chunkPos.X * chunkSize + x, chunkPos.Y * chunkSize + y, chunkPos.Z * chunkSize + z);
					}
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


				//Voxel.DrawCubeWires(position, Color.GREEN);
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
					if (dot <= 0.005f)
					{
						continue;
					}
					var dataIndex = toIndex(position + -normal);
					if (data[dataIndex] != null)
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
			return chunkPos.X < min.X ||
				chunkPos.Y < min.Y ||
				chunkPos.Z < min.Z ||
				chunkPos.X > max.X ||
				chunkPos.Y > max.Y ||
				chunkPos.Z > max.Z;
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
			var index = toIndex(pos);
			if (OutBounds(pos))
			{
				return;
			}

			if (index < 0 || index >= data.Length)
			{
				return;
			}
			if (data[index] != null)
			{
				return;
			}

			var subTexture = atlasManager.Lookup(tileName);
			System.Diagnostics.Debug.Assert(subTexture != null, "tile name must be valid");
			System.Diagnostics.Debug.Assert(subTexture.texture.id > 0, "tile has no texture");
			data[index] = new VoxelCube(subTexture);

			IncrementCubeCount(Voxel.GetChunkPosition(chunkSize, pos));
		}

		public void RemoveCube(Vector3 position)
		{
			var chunkIndex = toChunkIndex(position);
			DecrementCubeCount(chunkIndex);
		}

		public void IncrementCubeCount(Vector3 chunkPos)
		{
			var chunkIndex = toChunkIndex(chunkPos);
			if (chunkIndex >= 0 && chunkIndex < chunks.Length)
			{
				if (chunks[chunkIndex] == null)
				{
					chunks[chunkIndex] = new VoxelChunk();
					chunks[chunkIndex].position = chunkPos;
				}
				chunks[chunkIndex].cubeCount++;
				System.Diagnostics.Debug.Assert(
					chunks[chunkIndex].cubeCount <= chunkSize * chunkSize * chunkSize,
					string.Format("cube count must not exceed chunk size: count={0} chunkSize={1}", chunks[chunkIndex].cubeCount, chunkSize * chunkSize * chunkSize)
				);
			}
		}
		public void DecrementCubeCount(int chunkIndex)
		{
			if (chunkIndex < 0 || chunkIndex >= chunks.Length)
			{
				return;
			}
			var chunk = chunks[chunkIndex];
			if (chunk == null)
			{
				return;
			}
			if (chunk.cubeCount > 0)
			{
				chunks[chunkIndex].cubeCount = chunk.cubeCount - 1;
			}
		}
	}

	class Voxel
	{

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
		public static Vector3 AlignPosition(Vector3 position)
		{
			position.X = (float)System.Math.Round(position.X);
			position.Y = (float)System.Math.Round(position.Y);
			position.Z = (float)System.Math.Round(position.Z);
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

			//gl.rlCheckRenderBatchLimit(4);
			//gl.rlBegin(DrawMode.QUADS);
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

			//gl.rlEnd();
			//gl.rlSetTexture(0);
		}
	}
}