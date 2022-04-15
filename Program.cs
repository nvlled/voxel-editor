
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;

using Camera3D = HelloWorld.FPCamera;
using System.Collections.Generic;
using System;
using System.Linq;



namespace HelloWorld
{
	public class Config
	{
		public static int renderSize = 50;
		public static int chunkSize = 5;
		public static float fov = 60;
		public static int fps = 60;
		public static int windowWidth = 1000;
		public static int windowHeight = 800;
		public static float cameraHeight = 1.5f;
		public static float runSpeed = 5.0f;
		public static float walkSpeed = 1.5f;
	}


	public record class SubTexture2D(Texture2D texture, Rectangle region);


	public record TileID
	{
		public record One(int value) : TileID();
		public record Many(int[] ids) : TileID();
	}

	public class RayCaster
	{
		float renderDistance;
		Vector2 size;
		public Ray ray;
		public List<Vector3> rays = new List<Vector3>();

		public Camera3D cam;

		public RayCaster(Camera3D cam) { this.cam = cam; }

		public void SetOrigin(Ray ray, Vector3 worldUp, int renderDistance, int stepSize = 1)
		{
			this.ray = ray;
			this.renderDistance = renderDistance;

			var (right, up) = R.GetOrthogonalAxis(ray.direction, worldUp);
			//size = RaylibExt.GetFarsideDimension(cam.FOV, renderDistance);
			size = R.GetFarsideDimension(cam.GetFOVX(), renderDistance);
			var center = ray.position + ray.direction * renderDistance;

			rays.Clear();
			foreach (var v in R.IterateOutwards((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y), stepSize))
			{
				//rays.Add(center + right * v.X + up * v.Y);
				var dir = Vector3.Normalize((center + right * v.X + up * v.Y) - ray.position);
				rays.Add(dir * renderDistance);
			}
		}

		public void Draw()
		{
			if (ray.direction.Equals(Vector3.Zero))
			{
				return;
			}

			var o = ray.position;
			var center = ray.position + ray.direction * renderDistance;
			foreach (var v in rays)
			{
				var c = (int)Math.Min(255, v.Length() / renderDistance * 255);
				//rl.DrawLine3D(, new Color(c, 0, 0, 255));
				//rl.DrawRay(new Ray(o, o + v), new Color(200, 0, 0, 255));
				rl.DrawCylinderEx(o, o + v, 0.01f, 0.01f, 4, Color.RED);
			}
			var (right, up) = R.GetOrthogonalAxis(ray.direction, new Vector3(0, 1, 0));
			var w = size.X / 2;
			var h = size.Y / 2;
			rl.DrawCube(center, 1f, 1f, 1f, Color.RED);

			rl.DrawLine3D(center, center + right * w, Color.BLUE);
			rl.DrawCube(center + right * w, 1f, 1f, 1f, Color.BLUE);

			rl.DrawLine3D(center, center + -right * w, Color.BLUE);
			rl.DrawCube(center + -right * w, 1f, 1f, 1f, Color.BLUE);

			rl.DrawLine3D(center, center + up * h, Color.GREEN);
			rl.DrawCube(center + up * h, 1f, 1f, 1f, Color.GREEN);

			rl.DrawLine3D(center, center + -up * h, Color.GREEN);
			rl.DrawCube(center + -up * h, 1f, 1f, 1f, Color.GREEN);

			rl.DrawLine3D(center, center + right * w + up * h, Color.GREEN);
			rl.DrawCube(center + right * w + up * h, 1f, 1f, 1f, Color.GREEN);

			//rl.DrawLine3D(o, center, Color.BLUE);
			//rl.DrawLine3D(o, center + right * size.X / 2, Color.BLUE);
			//rl.DrawLine3D(o, center + up * size.Y / 2, Color.BLUE);
		}
	}


	public class AtlasManager
	{
		public Atlas[] atlases = new Atlas[100];
		public Dictionary<string, (Atlas, TileID)> lookupTable = new Dictionary<string, (Atlas, TileID)>();

		public Random rand = new Random();

		public void AddAtlas(Atlas atlas)
		{
			var id = atlas.id;
			if (id < 0 || id >= atlases.Length)
			{
				throw new System.ArgumentException("invalid atlas id", id.ToString());
			}
			var existing = atlases[id];
			if (existing != null)
			{
				throw new System.ArgumentException("atlas id is already used", id.ToString());
			}
			atlases[id] = atlas;
		}


		public void AddLookup(string tileName, Atlas atlas, int i, int j)
		{
			var tileID = i * atlas.cols + j;
			lookupTable[tileName] = (atlas, new TileID.One(tileID));
		}
		public void AddLookup(string tileName, Atlas atlas, int[] ids)
		{
			lookupTable[tileName] = (atlas, new TileID.Many(ids));
		}

		public SubTexture2D Lookup(string tileName)
		{
			var (atlas, tileID) = lookupTable[tileName];
			if (atlas.id == 0)
			{
				return null;
			}
			var id = tileID switch
			{
				TileID.One oneID => oneID.value,
				TileID.Many anyID => anyID.ids[rand.Next(0, anyID.ids.Length)],
				_ => 0,
			};
			var i = id / atlas.cols;
			var j = id % atlas.cols;
			var x = j * atlas.tileSize;
			var y = i * atlas.tileSize;

			var region = new Rectangle(x, y, atlas.tileSize, atlas.tileSize);
			return new SubTexture2D(atlas.texture, region);
		}

		public static AtlasManager Init()
		{
			var atlasMan = new AtlasManager();
			var rogueAtlas = new Atlas(1, rl.LoadTexture("./assets/atlas.png"), 32);
			atlasMan.AddAtlas(rogueAtlas);

			atlasMan.AddLookup("greenwall-1", rogueAtlas, 2, 1);
			atlasMan.AddLookup("greenwall", rogueAtlas, new int[] { 192, 193, 194, 195 });
			atlasMan.AddLookup("mazewall", rogueAtlas, new int[] { 196, 197, 198, 199, 200 });
			atlasMan.AddLookup("sand", rogueAtlas, new int[] { 381, 382, 383, 384, 385, 386, 387, 388 });
			atlasMan.AddLookup("marble", rogueAtlas, new int[] { 571, 572, 573, 574, 575, 576, 577 });
			atlasMan.AddLookup("brick", rogueAtlas, new int[] { 389, 391, 393 });


			return atlasMan;
		}
	}

	public class Atlas
	{
		const int COORD_SIZE = 30000;

		public int id;
		public Texture2D texture;
		public float tileSize;
		public int rows;
		public int cols;
		public Atlas(int id, Texture2D texture, float tileSize)
		{
			this.id = id;
			this.texture = texture;
			this.tileSize = tileSize;
			rows = texture.height / (int)tileSize;
			cols = texture.width / (int)tileSize;
		}

		public Rectangle GetRect(int tileID)
		{
			var result = new Rectangle();
			var i = tileID / cols;
			var j = tileID % cols;
			var size = tileSize;
			result.x = size * (float)j;
			result.y = size * (float)i;
			result.width = size;
			result.height = size;
			return result;
		}

		public Rectangle GetRect(int i, int j)
		{
			var result = new Rectangle();
			var size = tileSize;
			result.x = size * (float)j;
			result.y = size * (float)i;
			result.width = size;
			result.height = size;
			return result;
		}

		public (Atlas, int) GetTuple(int i, int j)
		{
			return (this, i * rows + j);
		}
	}

	public static class Text3DSettings
	{

		public const float LETTER_BOUNDRY_SIZE = 0.25f;
		public const float TEXT_MAX_LAYERS = 32;
		public static Color LETTER_BOUNDRY_COLOR = Color.VIOLET;

		public static bool SHOW_LETTER_BOUNDRY = false;
		public static bool SHOW_TEXT_BOUNDRY = false;
	}


	class Debug
	{
		static float angle = 0;
		static float cubeSize = 0.1f;
		static float cubePos = 0;
		static float cubeDir = 1;

		public static void DrawThickLine3D(Vector3 startPos, Vector3 endPos, Color color)
		{
			var worldUp = new Vector3(0, 1, 0);
			var forward = Vector3.Normalize(endPos - startPos);
			if (forward.Equals(worldUp))
			{
				worldUp = new Vector3(0, 0, 1);
			}

			var right = R.CrossN(forward, worldUp);
			var up = R.CrossN(forward, right);

			rl.DrawLine3D(startPos, endPos, color);
			var radius = 0.001f;
			for (var x = -2; x <= 2; x++)
			{
				for (var y = -2; y <= 2; y++)
				{
					var vx = radius * x * right;
					var vy = radius * y * up;
					rl.DrawLine3D(startPos + vx + vy, endPos + vx + vy, color);
				}

			}
		}

		/*
				public static void DrawThickLine3D(Vector3 startPos, Vector3 endPos, Color color)
				{
					var radius = 0.004f; // for thickness
					var up = Vector3.Normalize(endPos - startPos);
					var right = new Vector3(1, 0, 0);
					if (Vector3.Dot(up, right) != 0)
					{
						right = new Vector3(0, 1, 0);
					}

					for (var a = 0f; a <= 360; a += 20)
					{
						gl.rlPushMatrix();
						gl.rlRotatef(a, up.X, up.Y, up.Z);
						gl.rlTranslatef(radius * right.X, radius * right.Y, radius * right.Z);
						rl.DrawLine3D(startPos, endPos, color);
						gl.rlPopMatrix();
					}
				}
				*/

		public static void DrawOrigin()
		{
			var len = 10;
			var xColor = Color.RED;
			var yColor = Color.DARKGREEN;
			var zColor = Color.DARKBLUE;
			DrawThickLine3D(
				//right: new Vector3(1, 0, 0),
				startPos: new Vector3(0, 0, 0),
				endPos: new Vector3(0, 1 * len, 0),
				yColor
			);
			DrawThickLine3D(
				//right: new Vector3(1, 0, 0),
				startPos: new Vector3(0, 0, 0),
				endPos: new Vector3(1 * len, 0, 0),
				xColor
			);
			DrawThickLine3D(
				//right: new Vector3(1, 0, 0),
				startPos: new Vector3(0, 0, 0),
				endPos: new Vector3(0, 0, 1 * len),
				zColor
			);

			rl.DrawCube(new Vector3(cubePos, 0, 0), cubeSize, cubeSize, cubeSize, xColor);
			rl.DrawCube(new Vector3(0, cubePos, 0), cubeSize, cubeSize, cubeSize, yColor);
			rl.DrawCube(new Vector3(0, 0, cubePos), cubeSize, cubeSize, cubeSize, zColor);
			cubePos += cubeDir * 0.1f;
			if (cubePos > len)
			{
				cubeDir = -1;
			}
			else if (cubePos < 0)
			{
				cubeDir = 1;
			}

			/*
						RaylibExt.WithMatrix(() =>
						{
							gl.rlRotatef(90, 1, 0, 0);
							gl.rlTranslatef(0, 0.0f, -1.0f);
							RaylibExt.DrawText3D(rl.GetFontDefault(), "X", new Vector3(3, 0, 0), 24, 1, 1, true, xColor);
							RaylibExt.DrawText3D(rl.GetFontDefault(), "Y", new Vector3(0, 0, -3), 24, 1, 1, true, yColor);
							RaylibExt.DrawText3D(rl.GetFontDefault(), "Z", new Vector3(0, 3, 0), 24, 1, 1, true, zColor);
						});
						*/

		}
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

	class Entity
	{
		public Vector3 position;
		public Vector3 size;
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
	class VoxelWorld
	{
		public int renderDistance = 20;
		public int chunkSize = 5;

		public AtlasManager atlasManager;

		public VoxelCube[] data;
		public byte drawFlag = 1;
		public byte[] drawBuffer;
		public byte[] drawChunkBuffer;

		public List<Entity> entities;
		Vector3 minBounds;
		Vector3 maxBounds;
		Vector3 size;

		public int iterations;

		HashSet<int> renderCache = new HashSet<int>();



		public VoxelWorld(int size) : this(size, size, size)
		{ }

		public VoxelWorld(int xSize, int ySize, int zSize) : this(new Vector3(-xSize, -ySize, -zSize), new Vector3(xSize, ySize, zSize)) { }

		public VoxelWorld(Vector3 min, Vector3 max)
		{
			minBounds = min;
			maxBounds = max;
			size = max - min;

			var dataSize = (int)size.X * (int)size.Y * (int)size.Z;
			System.Diagnostics.Debug.Assert(dataSize < Math.Pow(2, 31));
			data = new VoxelCube[dataSize];
			drawBuffer = new byte[dataSize];
			drawChunkBuffer = new byte[dataSize / chunkSize];
		}

		int toChunkIndex(Vector3 chunkPos)
		{

			chunkPos = chunkPos - minBounds / chunkSize;
			var s = size / chunkSize;
			var index = (int)(chunkPos.X * s.Y * s.Z + chunkPos.Y * s.Z + chunkPos.Z);
			return index;
		}
		int toIndex(Vector3 p)
		{
			var q = p;
			p = p - minBounds;
			var index = (int)(p.X * size.Y * size.Z + p.Y * size.Z + p.Z);
			//if (index < 0)
			//{
			//	System.Diagnostics.Debug.Assert(index >= 0, string.Format("invalid position: index={0}, pos={1}, pos0={2}", index, p, q));
			//}
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

		public void DrawBoundaries()
		{
			var min = Voxel.AlignPosition(minBounds) + Vector3.One * -0.51f;
			var max = Voxel.AlignPosition(maxBounds) + Vector3.One * 0.51f;
			var org = Vector3.Zero;
			//rl.DrawLine3D(min, size * Vector3.UnitX, Color.BLACK);
			//rl.DrawLine3D(min, size * Vector3.UnitY, Color.BLACK);
			//rl.DrawLine3D(min, size * Vector3.UnitZ, Color.BLACK);
			var p1 = min;
			var p2 = min + size * Vector3.UnitZ;
			var p3 = min + size * Vector3.UnitX;
			var p4 = min + size * Vector3.UnitX + size * Vector3.UnitZ;
			var p5 = min + size * Vector3.UnitY;
			var p6 = min + size * Vector3.UnitY + size * Vector3.UnitZ;
			var p7 = min + size * Vector3.UnitY + size * Vector3.UnitX;
			var p8 = min + size;

			//Voxel.DrawCube(p1, Color.RED);
			//Voxel.DrawCube(p2, Color.BLUE);
			//Voxel.DrawCube(p3, Color.GREEN);
			//Voxel.DrawCube(p4, Color.YELLOW);
			//Voxel.DrawCube(max, Color.VIOLET);

			//Voxel.DrawCube(p5, Color.ORANGE);
			//Voxel.DrawCube(p6, Color.BLACK);
			//Voxel.DrawCube(p7, Color.BROWN);
			//Voxel.DrawCube(p8, Color.GRAY);

			R.DrawPlane(p1, p2, p3, p4, new Color(20, 20, 20, 50));
			R.DrawPlane(p5, p6, p7, p8, new Color(20, 20, 20, 50));

			R.DrawPlane(p1, p3, p7, p5, new Color(0, 0, 0, 255));
			R.DrawPlane(p3, p7, p8, p4, new Color(0, 0, 0, 255));
			R.DrawPlane(p4, p2, p6, p8, new Color(0, 0, 0, 255));
			R.DrawPlane(p2, p6, p5, p1, new Color(0, 0, 0, 255));

			rl.DrawLine3D(p1, p3, Color.RED);




			//rl.DrawLine3D(max, min + size * Vector3.UnitX, Color.VIOLET);
			//rl.DrawLine3D(max, min + size * Vector3.UnitY, Color.VIOLET);
			//rl.DrawLine3D(max, min + size * Vector3.UnitZ, Color.VIOLET);

			//rl.DrawPlane(min + new Vector3(size.X / 2, 0, size.Z / 2), new Vector2(size.X, size.Z), new Color(50, 50, 50, 128));
			//rl.DrawPlane(min + new Vector3(size.X / 2, -size.Y, size.Z / 2), new Vector2(size.X, size.Z), new Color(50, 50, 50, 128));
			//rl.DrawLine3D(minBounds, new Vector3(0, maxBounds.Y, 0), Color.BLACK);
			//rl.DrawLine3D(minBounds, new Vector3(0, 0, maxBounds.Z), Color.BLACK);
		}

		public void DrawInfo(VoxelWorld world, Ray camRay)
		{
			rl.DrawText(string.Format("{0}", Voxel.AlignPosition(camRay.position)), 10, 10, 22, Color.BLACK);
			rl.DrawText(string.Format("{0}", Voxel.GetChunkPosition(world.chunkSize, camRay.position)), 10, 30, 22, Color.BLACK);
		}

		public void Draw(Ray ray, Camera3D cam)
		{
			DrawBoundaries();
			DrawRaycastedCubes(ray, cam);

			// switch draw flag to render a cube only once per frame
			drawFlag = (byte)(drawFlag == 1 ? 2 : 1);
		}

		public Vector3[] GetRayCastedChunks(Ray ray, Camera3D cam, int renderDistance)
		{
			var (right, up) = R.GetOrthogonalAxis(ray.direction, cam.Camera.up);
			var size = R.GetFarsideDimension(cam.GetFOVX(), renderDistance);

			var added = new HashSet<int>();
			var result = new List<Vector3>();

			// TODO: commit
			// TODO: move larger classes into separate files
			// TODO: keep track of empty chunks, and skip them when rendering

			var center = ray.position + ray.direction * renderDistance;
			iterations = 0;
			foreach (var v in R.IterateOutwards((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y), chunkSize))
			{
				var dir = Vector3.Normalize((center + right * v.X + up * v.Y) - ray.position);
				for (var d = 1.0f; d <= renderDistance; d += chunkSize * 0.5f)
				{
					var q = ray.position + dir * d;
					//rays.Add(dir * renderDistance);

					var chunkPos = Voxel.GetChunkPosition(chunkSize, q);

					var chunkIndex = toChunkIndex(chunkPos);
					if (added.Contains(chunkIndex))
					{
						iterations++;
						continue;
					}
					added.Add(chunkIndex);
					result.Add(chunkPos);
				}
			}
			Console.WriteLine(iterations);
			Console.WriteLine(result.Count() * chunkSize * chunkSize * chunkSize);

			return result.ToArray();
		}

		// TODO: since I actually don't need pixel-like precision,
		// raycasting chunks would be way more efficient
		// but yeah, it's good enough for now
		// actually, some flickering blocks is kind of annoying
		// Looks like I really need to do chunking,
		// with more random blocks, the flickering blocks
		// becomes more apparent.
		public void DrawRaycastedCubes(Ray ray, Camera3D cam)
		{
			var camUp = cam.Camera.up;

			var right = R.CrossN(ray.direction, camUp);
			var left = Vector3.Negate(right);
			var up = R.CrossN(ray.direction, right);
			var down = Vector3.Negate(up);

			var xStep = Vector3.Normalize(right - left);
			var yStep = Vector3.Normalize(up - down);

			var farSide = ray.direction * renderDistance;
			var fovX = cam.GetFOVX();
			var fovY = cam.Camera.fovy;
			var fov = MathF.Max(fovX, fovY);

			//var m = v * rm.MatrixRotate(up, fovX);
			var v2 = rm.Vector3RotateByQuaternion(ray.direction, rm.QuaternionFromAxisAngle(up, fov));
			//var v3 = rm.Vector3RotateByQuaternion(ray.direction, rm.QuaternionFromAxisAngle(ray.direction, fovY));
			var radius = MathF.Ceiling(MathF.Abs((v2 - farSide).Length())) * 0.5f;


			var batchSize = 300;

			for (var d = 1f; d < renderDistance; d += chunkSize)
			{

				var p = ray.position + ray.direction * renderDistance;
				var u = Vector3.Normalize(p - ray.position);
				var q = ray.position + u * d;

				var chunkPos = Voxel.GetChunkPosition(chunkSize, q);

				var chunkIndex = toChunkIndex(chunkPos);
				if (chunkIndex < 0 || chunkIndex >= drawChunkBuffer.Length || drawChunkBuffer[chunkIndex] == drawFlag)
				{
					continue;
				}
				drawChunkBuffer[chunkIndex] = drawFlag;
				//var vs = Voxel.GetChunkVertices(chunkSize, chunkPos);
			}

			// TODO: fix toChunkIndex
			// TODO: highlight chunks
			/*
			foreach (var (n, a, b) in IterateChunkOutwards((int)radius))
			{
				var p = ray.position + ray.direction * renderDistance + xStep * a + yStep * b;
				var u = Vector3.Normalize(p - ray.position);
				for (var d = 1f; d < renderDistance; d += chunkSize / 2)
				{
					var q = ray.position + u * d;
					var chunkPos = Voxel.GetChunkPosition(chunkSize, q);

					var chunkIndex = toChunkIndex(chunkPos);
					if (chunkIndex < 0 || chunkIndex >= drawChunkBuffer.Length || drawChunkBuffer[chunkIndex] == drawFlag)
					{
						continue;
					}
					drawChunkBuffer[chunkIndex] = drawFlag;


					iterations = 1;
					Voxel.StartDrawTile(batchSize);
					foreach (var c in IterateChunk(chunkSize, chunkPos))
					{
						DrawCube(c);
						iterations++;
						if (iterations % batchSize == 0)
						{
							Voxel.EndDrawTile();
							Voxel.StartDrawTile(batchSize);
						}
					}
					Voxel.EndDrawTile();

					//if (OutBounds(q))
					//{
					//	break;
					//}

					//var found = DrawCube(Voxel.AlignPosition(q));
					//if (found) { break; }

				}
			}
			*/
		}

		public IEnumerable<Vector3> IterateChunk(int chunkSize, Vector3 chunkPos)
		{
			for (var x = 0; x < chunkSize; x++)
			{
				for (var y = 0; y < chunkSize; y++)
				{
					for (var z = 0; z < chunkSize; z++)
					{
						yield return new Vector3(chunkPos.X + x, chunkPos.Y + y, chunkPos.Z + z);
					}
				}

			}
		}

		public IEnumerable<(float, float, float)> IterateChunkOutwards(int radius)
		{

			yield return (0, 0, 0);
			for (var n = 1f; n <= radius; n += 1)
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
				inc += 0.01f;
			}
		}

		public bool DrawCube(Vector3 position)
		{
			var index = toIndex(position);
			if (index < 0 || index >= drawBuffer.Length || drawBuffer[index] == drawFlag)
			{
				return false;
			}
			drawBuffer[index] = drawFlag;

			var cube = data[index];
			if (cube != null)
			{
				DrawCube(position, cube);
				return true;
			}
			return false;
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
				var sideType = (CubeSideType)index;
				var (texture, region) = side.body.subTexture;


				if (texture.id == 0)
				{
					Voxel.DrawCube(position, Color.DARKBLUE);
					Voxel.DrawCubeWires(position, Color.GREEN);
				}
				else
				{
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.FRONT, sizeArg: side.body.scale);
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.BACK, sizeArg: side.body.scale);
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.RIGHT, sizeArg: side.body.scale);
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.LEFT, sizeArg: side.body.scale);
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.TOP, sizeArg: side.body.scale);
					Voxel.DrawTile(texture: ref texture, source: ref region, cubePos: position, side: CubeSideType.BOTTOM, sizeArg: side.body.scale);
				}
			}
		}

		public void InsertCube(Vector3 position, VoxelCube cube)
		{

		}
		public void InsertCube(Vector3 position, VoxelCubeSideItem side)
		{

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
			var subTexture = atlasManager.Lookup(tileName);
			System.Diagnostics.Debug.Assert(subTexture != null, "tile name must be valid");
			System.Diagnostics.Debug.Assert(subTexture.texture.id > 0, "tile has no texture");
			data[index] = new VoxelCube(subTexture);
		}
		public void RemoveCube(Vector3 position)
		{

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

	static class Program
	{
		public unsafe static int Main()
		{
			var (sw, sh) = (Config.windowWidth, Config.windowHeight);
			rl.InitWindow(sw, sh, "Voxel editor");
			rl.SetWindowPosition(rl.GetMonitorWidth(0) - sw - 50, rl.GetMonitorHeight(0) - sh - 50);

			var cam = new Camera3D();

			cam.Setup(Config.fov, new Vector3(0, Config.cameraHeight, 0));
			rl.DisableCursor();
			cam.MoveSpeed = new Vector3(Config.runSpeed);

			var rayCaster = new RayCaster(cam);

			var tree = rl.LoadTexture("assets/tree_single.png");
			var shader = rl.LoadShader("", "assets/discard_alpha.fs");

			var atlas = new Atlas(1, rl.LoadTexture("assets/atlas.png"), 32);

			rl.SetTargetFPS(Config.fps);


			var cubes1 = new System.Collections.Generic.List<Vector3>();
			var cubes2 = new System.Collections.Generic.List<Vector3>();

			var world = new VoxelWorld(new Vector3(-50, -20, -50), new Vector3(50, 20, 50));
			world.atlasManager = AtlasManager.Init();
			Console.WriteLine("world data size: {0}", world.data.Length);

			var r = new Random();
			foreach (var (n, a, b) in world.IterateOutwards(25))
			{
				world.InsertCube(new Vector3(a + r.Next(1, 2), a + r.Next(1, 2), b + n), "greenwall");
			}
			foreach (var (n, a, b) in world.IterateOutwards(25))
			{
				world.InsertCube(new Vector3(a + n + r.Next(1, 5), b + r.Next(1, 5), b + r.Next(1, 5)), "greenwall");
			}

			var rayCastedChunks = new Vector3[10];

			while (!rl.WindowShouldClose())
			{

				var camRay = rl.GetMouseRay(new Vector2(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2), cam.Camera);
				//camRay.direction.Y = 0;

				if (rl.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
				{

					cam.MoveSpeed = new Vector3(Config.walkSpeed);
				}
				else
				{
					cam.MoveSpeed = new Vector3(Config.runSpeed);

				}

				if (rl.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
				{
					var pos = Voxel.AlignPosition(camRay.position + camRay.direction * 1.5f);
					//world.InsertCube(pos, "brick");
					rayCastedChunks = world.GetRayCastedChunks(camRay, cam, world.renderDistance);
					rayCaster.SetOrigin(camRay, cam.Camera.up, world.renderDistance, world.chunkSize);
				}

				if (rl.IsKeyPressed(KeyboardKey.KEY_SPACE))
				{
					if (rl.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
					{
					}
					else
					{

					}

					cam.JumpHeight += 0.01f;
				}
				else
				{
					cam.Update();
				}



				// TODO: world data

				// TODO: if tile is already occupied, shift tile cursor towards camera
				// TODO: should only allow cube insertion on ground, or adjacent to other cubes,
				//       not on empty space 
				// TODO: tile collision
				// TODO: decouple movement from camera
				// cam.SetPosition(entity)
				// TODO:
				// cam.Jumper.Height
				// cam.Jumper.Update()
				// cam.Jumper.Perform()


				rl.BeginDrawing();
				rl.ClearBackground(Color.WHITE);

				cam.BeginMode3D();


				world.Draw(camRay, cam);
				rayCaster.Draw();


				DrawRayCastedChunks(world.chunkSize, rayCastedChunks);


				rl.BeginShaderMode(shader);

				var srcRec = new Rectangle(0, 0, tree.width, tree.height);
				var up = new Vector3(0, 1, 0);
				var size = new Vector2(1, 1);

				//Voxel.DrawCubeWires(Voxel.AlignPosition(camRay.position + camRay.direction * 1.5f), Color.RED);

				Debug.DrawOrigin();

				rl.EndShaderMode();

				cam.EndMode3D();
				rl.DrawFPS(rl.GetScreenWidth() - 100, 10);
				rl.DrawText(string.Format("iter={0}", world.iterations), 10, 500, 22, Color.RED);
				world.DrawInfo(world, camRay);


				rl.EndDrawing();
			}

			Raylib.CloseWindow();
			return 0;
		}

		// TODO: draw casted ray lines
		public static void DrawRayCastedChunks(int chunkSize, Vector3[] chunks)
		{
			foreach (var chunk in chunks)
			{
				var vs = Voxel.GetChunkVertices(chunkSize, chunk);
				var r = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.X)) / chunkSize) * 255);
				var g = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.Y)) / chunkSize) * 255);
				var b = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.Z)) / chunkSize) * 255);
				var c = new Color(r, g, b, 180);
				var mid = Vector3.One * (chunkSize / 2f);
				var p = chunk * chunkSize + mid;
				rl.DrawCube(p, chunkSize, chunkSize, chunkSize, c);
				rl.DrawCubeWires(p, chunkSize, chunkSize, chunkSize, Color.WHITE);
				//RaylibExt.DrawPlane(vs[0], vs[1], vs[2], vs[3], c);
				//RaylibExt.DrawPlane(vs[0], vs[1], vs[2], vs[3], c);
			}
		}
	}

}