
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
	public record class SubTexture2D(Texture2D texture, Rectangle region);


	public record TileID
	{
		public record One(int value) : TileID();
		public record Many(int[] ids) : TileID();
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

			RaylibExt.WithMatrix(() =>
			{
				gl.rlRotatef(90, 1, 0, 0);
				gl.rlTranslatef(0, 0.0f, -1.0f);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "X", new Vector3(3, 0, 0), 24, 1, 1, true, xColor);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "Y", new Vector3(0, 0, -3), 24, 1, 1, true, yColor);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "Z", new Vector3(0, 3, 0), 24, 1, 1, true, zColor);
			});

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

	/*
	class VoxelCubeSideTexture
	{
		public byte atlasID;
		public ushort i;
		public ushort j;
	}
	*/

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
		public AtlasManager atlasManager;

		public VoxelCube[] data;
		public byte drawFlag = 1;
		public byte[] drawBuffer;

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
		}

		int toIndex(Vector3 p)
		{
			p = p - minBounds;
			var index = (int)(p.X * size.Y * size.Z + p.Y * size.Z + p.Z);
			System.Diagnostics.Debug.Assert(index >= 0);
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

		// !!!!!!!!!!
		// TODO: render only blocks that are in the direction of the camera
		// TODO: it's slow as fuck
		// 100x100x100 is actually pretty big already
		// but still iterating the whole world per frame is a big nope
		// !!!!!!!!!!
		// !!!!!!!!!!

		float temp = 0;
		(float, float) temp2 = (0, 0);

		// Hmmm, I guess this works? At least it remains
		// constant even if I make the world bigger.
		// is 2000 iteration per frame a lot?
		// actually, even a million loop per frame
		// seems to do just fine
		// yeah, this will do just fine
		// as long as it's not several billions per frame

		// Okay, there's one little problem.
		// Since the cubes are aligned, some
		// cubes won't get rendered at certain awkward angles
		// solution 0: cast the wall on the xz, xy, zy plane
		// solution 1: instead of a wall, cast a cube or sphere
		// solution 2: instead of using round, using both floor and ceil
		// solution 3: read about frustrum
		// #2 is probably more efficient, but no guarantees it will work
		// #1 excessive
		// #3, I could look at the camera code and how the frustrum
		// is computed. This is probably the sanest solution.
		// nope, #2 doesn't work
		// After having been haunted in my sleep, I
		// realized that none of this will work.
		// #0 still will miss a lot of cubes on certain (diagonal) angles
		// #3 frustrum and cone are about the same thing anyway
		// solution 4: do ray tracing, but with cubes instead of the pixels
		// With #4, I think I can even do a render distance of more than a hundred
		// and still perform well. Maybe? I at least I could stop
		// skip the ray once it hits an opaque cube, but it's not
		// that simple at oblique camera views.
		// 
		// 5x5=25 rays, and 100 render distance
		// 2500 iterations at most
		// or 49 rays, and 100 render distance
		// 4900
		// yeah this will work
		// *******33333**************
		// *******32223**************
		// *******32123**************
		// *******32223**************
		// *******33333**************

		// As an added possible optimization,
		// render the distant cubes every other frame?
		// Or not, that will probably make it blink

		// TODO:
		// hmm, it seems FPS drops a lot when I get close to a texture
		// it's definitely not a problem in the number of iterations
		// since it still lags even I just draw a single cube
		// My guess is that this has to do with copying the Texture2D around
		// Nope, it's not it
		// There's probably something that should not be computed
		// per frame, such as:
		// - IEnumerable
		// - quaternion
		// Aha, by process of elimination, I found the problem, maybe.
		// It seems calling DrawTiles too many times is the source.
		// I copied the code from DrawBillboard, which I imagine
		// doesn't perform well at scale. Looking closer,
		// I think the problem is one of these:
		/*
			gl.rlCheckRenderBatchLimit(4);
			gl.rlSetTexture(texture.id);
			gl.rlBegin(DrawMode.QUADS);
		*/
		// It appears that rl.DrawCube suffers from the same problem.
		// On the plus side, the ray casting works.

		// Okay... It can maintain 60 fps at a relatively big world.
		// It bothers me that it does 100k loops per frame,
		// but it seems to be fine it that 100k loop does nothing for the most part.
		// I can tweak it all month long to make it faster, but
		// I leave it for now. It's good enough, and I've got
		// more things to do.

		// TODO: commit, then cleanup, then commit again
		// TODO: show tile coordinates, and distance from origin
		// TODO: do not draw outside world boundary
		// TODO: tile switcher
		// TODO: draw line/grid at world boundary
		// TODO: save/load world state
		// TODO: collision detection

		public void Draw(Ray ray, Camera3D cam)
		{
			var camUp = cam.Camera.up;

			var right = Vector3.Cross(ray.direction, camUp);
			var left = Vector3.Negate(right);
			var up = Vector3.Cross(ray.direction, right);
			var down = Vector3.Negate(up);

			var xStep = Vector3.Normalize(right - left);
			var yStep = Vector3.Normalize(up - down);

			var renderDistance = 50;
			var v = ray.direction * renderDistance;
			var fovX = cam.GetFOVX();
			var fovY = cam.Camera.fovy;
			var fov = MathF.Max(fovX, fovY);

			//var m = v * rm.MatrixRotate(up, fovX);
			var v2 = rm.Vector3RotateByQuaternion(ray.direction, rm.QuaternionFromAxisAngle(up, fovY));
			var v3 = rm.Vector3RotateByQuaternion(ray.direction, rm.QuaternionFromAxisAngle(ray.direction, fovY));

			var radius = MathF.Ceiling(MathF.Abs((v3 - v).Length())) * 0.6f;
			var radiusY = radius;
			var radiusX = radius;


			iterations = 0;
			// TODO: check if iterateOutwards has excess coordinates
			foreach (var (n, a, b) in iterateOutwards((int)radius))
			{
				var p = ray.position + ray.direction * renderDistance + xStep * a + yStep * b;
				var u = Vector3.Normalize(p - ray.position);
				for (var d = 1f; d < renderDistance - n * 1.5; d += 1.0f)
				{

					var q = ray.position + u * d;
					var found = DrawCube(Voxel.AlignPosition(q));
					if (found) { break; }
					iterations++;
					//foreach (var cubePos in Voxel.AlignPositions(q))
					{
					}
				}
			}


			drawFlag = (byte)(drawFlag == 1 ? 2 : 1);
		}


		public IEnumerable<(float, float, float)> iterateOutwards(int radius)
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
		// 1, 0
		// 1, 1
		// 0, 1
		// -1, 0
		// 1, -1
		// 0, -1
		// -1,-1

		// **************************************************************
		// *******************2******************************************
		// ********************1*****************************************
		// *********************0****************************************
		// **************************************************************
		// **************************************************************



		/*
				public void Draw(Ray ray, Vector3 camUp)
				{
					var right = Vector3.Cross(ray.direction, camUp);
					var left = Vector3.Negate(right);
					var up = Vector3.Cross(ray.direction, right);
					var down = Vector3.Negate(up);

					var xStep = Vector3.Normalize(right - left);
					var yStep = Vector3.Normalize(up - down);

					//var left = Vector3.Cross(ray.direction, up);
					//var down = Vector3.Negate(up);
					//var right = Vector3.Negate(left);

					var maxRadius = 1;
					var renderDistance = 30;


					//var dirs = new Vector3[] { ray.direction, ray.direction + down, ray.direction + up, ray.direction + left, ray.direction + right };


					var numDraws = 0;
					var steps = 0;
					var pos = ray.position + ray.direction;
					while (steps++ < renderDistance)
					{
						// FIX: radius should be exponential so the cone would cover the screen size
						var radius = steps + maxRadius;
						for (var a = -radius; a <= radius; a++)
						{
							for (var b = -radius; b <= radius; b++)
							{
								foreach (var cubePos in Voxel.AlignPositions(pos + xStep * a + yStep * b))
								{

									//var cubePos = Voxel.AlignPosition(pos + xStep * a + yStep * b);
									var index = toIndex(cubePos); // TODO: put index on cube itself
									if (renderCache.Contains(index))
									{
										continue;
									}
									numDraws++;

									renderCache.Add(index);
									var cube = data[index];
									// TODO: use index to avoid  redundant redrawing
									if (cube != null)
									{
										var p = fromIndex(index);
										DrawCube(cubePos, cube);
									}
									if ((int)temp == steps)
									{
										//Voxel.DrawCubeWires(cubePos, Color.GREEN);
									}
								}



							}
						}
						pos += ray.direction * 0.60f;
						//pos = Voxel.AlignPosition(pos);

						if (temp > renderDistance)
						{
							temp = 0;
						}
						temp += rl.GetFrameTime() * 0.10f;
					}

					renderCache.Clear();
					//Console.WriteLine(numDraws);

				}
				*/

		public bool DrawCube(Vector3 position)
		{
			var index = toIndex(position);
			if (drawBuffer[index] == drawFlag)
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

		// TODO: do not insert cubes past world boundaries
		// TODO: draw lines  or planes at boundaries
		public void InsertCube(Vector3 position, string tileName)
		{
			var index = toIndex(position);
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

			//var source = subTexture.region;
			//var texture = subTexture.texture;

			// NOTE: Billboard size will maintain source rectangle aspect ratio, size will represent billboard width
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

			gl.rlCheckRenderBatchLimit(4);
			gl.rlSetTexture(texture.id);

			gl.rlBegin(DrawMode.QUADS);
			gl.rlColor4ub(color.r, color.g, color.b, color.a);

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
			gl.rlEnd();

			gl.rlSetTexture(0);
		}
	}

	static class Program
	{
		static void DrawTile(Camera3D camera, Texture2D texture, Rectangle source, Vector3 position, Vector3 up, Vector3 right, Vector2 size, Vector2? originArg = null, float rotation = 0, Color? colorArg = null)
		{
			// NOTE: Billboard size will maintain source rectangle aspect ratio, size will represent billboard width
			var sizeRatio = new Vector2(size.Y, size.X * (float)source.height / source.width);
			var origin = originArg.GetValueOrDefault(Vector2.Zero);

			//var matView = rm.MatrixLookAt(camera.position, camera.target, camera.up);

			// 11 12 13 14
			// 21 22 23 24
			// 31 32 33 34
			// 41 42 43 44
			//var right = new Vector3(matView.M11, matView.M21, matView.M31);
			//var up = { matView.m1, matView.m5, matView.m9 };

			var rightScaled = rm.Vector3Scale(right, sizeRatio.X / 2);
			var upScaled = rm.Vector3Scale(up, sizeRatio.Y / 2);

			var p1 = rm.Vector3Add(rightScaled, upScaled);
			var p2 = rm.Vector3Subtract(rightScaled, upScaled);

			var topLeft = rm.Vector3Scale(p2, -1);
			var topRight = p1;
			var bottomRight = p2;
			var bottomLeft = rm.Vector3Scale(p1, -1);


			// Translate points to the draw center (position)
			topLeft = rm.Vector3Add(topLeft, position);
			topRight = rm.Vector3Add(topRight, position);
			bottomRight = rm.Vector3Add(bottomRight, position);
			bottomLeft = rm.Vector3Add(bottomLeft, position);
			var color = colorArg.GetValueOrDefault(Color.WHITE);

			gl.rlCheckRenderBatchLimit(4);
			gl.rlSetTexture(texture.id);

			gl.rlBegin(DrawMode.QUADS);
			gl.rlColor4ub(color.r, color.g, color.b, color.a);

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
			gl.rlEnd();

			gl.rlSetTexture(0);
		}

		public unsafe static int Main()
		{
			var (sw, sh) = (1000, 800);
			rl.InitWindow(sw, sh, "aa");
			rl.SetWindowPosition(rl.GetMonitorWidth(0) - sw - 50, rl.GetMonitorHeight(0) - sh - 50);

			var cam = new Camera3D(
			//position: new Vector3(0, 1.5f, 0),
			//target: new Vector3(-0.5f, 0, -0.5f),
			//up: new Vector3(0, 1, 0),
			//40,
			//CameraProjection.CAMERA_PERSPECTIVE
			);

			cam.Setup(50, new Vector3(0, 1.5f, 0));
			rl.DisableCursor();
			cam.MoveSpeed = new Vector3(1.8f);


			var tree = rl.LoadTexture("assets/tree_single.png");
			var shader = rl.LoadShader("", "assets/discard_alpha.fs");

			var atlas = new Atlas(1, rl.LoadTexture("assets/atlas.png"), 32);

			//rl.SetCameraMode(cam, CameraMode.CAMERA_CUSTOM);
			rl.SetTargetFPS(60);


			var cubes1 = new System.Collections.Generic.List<Vector3>();
			var cubes2 = new System.Collections.Generic.List<Vector3>();

			var world = new VoxelWorld(new Vector3(-200, -200, -100), new Vector3(200, 200, 100));
			world.atlasManager = AtlasManager.Init();
			Console.WriteLine("world data size: {0}", world.data.Length);

			while (!rl.WindowShouldClose())
			{

				var camRay = rl.GetMouseRay(new Vector2(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2), cam.Camera);

				if (rl.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
				{
					var pos = Voxel.AlignPosition(camRay.position + camRay.direction * 1.5f);
					//cubes1.Add();
					world.InsertCube(pos, "brick");
				}

				if (rl.IsKeyPressed(KeyboardKey.KEY_SPACE))
				{
					if (rl.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
					{
						//cam.position.Y += -0.5f;
					}
					else
					{

					}

					cam.JumpHeight += 0.01f;
				}
				else
				{
					cam.Update();
					//rl.UpdateCamera(&cam);
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
				//rl.DrawText(string.Format("x={0} y={1} z={2}", (int)cam.position.X, (int)cam.position.Y, (int)cam.position.Z), 10, 10, 15, Color.BLACK);

				cam.BeginMode3D();



				rl.BeginShaderMode(shader);

				var srcRec = new Rectangle(0, 0, tree.width, tree.height);
				var up = new Vector3(0, 1, 0);
				var size = new Vector2(1, 1);

				/*
				foreach (var c in cubes1)
				{
					var region = atlas.GetRect(2, 8);
					var subTex = new SubTexture2D(atlas.texture, region);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.FRONT);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.RIGHT);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.LEFT);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.BACK);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.TOP);
					Voxel.DrawTile(subTexture: subTex, cubePos: c, side: CubeSideType.BOTTOM);
				}
				*/

				/*
								world.drawFlag = (byte)(world.drawFlag == 1 ? 2 : 1);
								var huh = 0;
								foreach (var g in world.fooblah(4))
								{
									//Voxel.DrawCube(new Vector3(-2, -2, -2), Color.RED);
									world.DrawCube(new Vector3(1, 0, 0));
									huh++;
								}
								Console.WriteLine("huh {0}", huh);
								*/

				//world.Draw(camRay, cam.Camera.up);
				Voxel.DrawCubeWires(Voxel.AlignPosition(camRay.position + camRay.direction * 1.5f), Color.RED);
				world.Draw(camRay, cam);

				//Voxel.DrawTile(Vector3.Zero, ref world.texture, ref world.region, side: CubeSideType.FRONT);
				//Voxel.DrawTile(Vector3.Zero, ref world.texture, ref world.region, side: CubeSideType.BACK);
				//Voxel.DrawTile(Vector3.Zero, ref world.texture, ref world.region, side: CubeSideType.TOP);
				//Voxel.DrawTile(Vector3.Zero, ref world.texture, ref world.region, side: CubeSideType.LEFT);
				//Voxel.DrawTile(Vector3.Zero, ref world.texture, ref world.region, side: CubeSideType.RIGHT);




				//Voxel.DrawCube(Vector3.Zero, new Color(0, 255, 255, 180));




				//gl.rlPushMatrix();
				//gl.rlTranslatef(-5, 0, 0);
				//rl.DrawCube(new Vector3(0, 0, 0), 1, 1, 1, Color.BROWN);
				//gl.rlPopMatrix();

				//rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, 5, Color.BLACK);
				//rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, 3, Color.RED);
				Debug.DrawOrigin();
				//rl.DrawText(Vector3.Normalize(ray.position).ToString(), 50, 50, 20, Color.BLACK);
				//rl.DrawText(Vector3.Normalize(ray.direction).ToString(), 50, 80, 20, Color.BLACK);
				//rl.DrawText(Vector3.Normalize(cam.target).ToString(), 50, 100, 20, Color.BLUE);

				rl.EndShaderMode();

				cam.EndMode3D();
				//rl.EndMode3D();
				rl.DrawFPS(rl.GetScreenWidth() - 100, 10);
				rl.DrawText(string.Format("iter={0}", world.iterations), 10, 500, 22, Color.RED);


				//rl.DrawTexture(atlas.texture, 50, 50, Color.WHITE);

				// crosshair
				//rl.DrawText(string.Format("cam ray {0}", camRay.direction), 20, 20, 30, Color.BLUE);
				//rl.DrawFPS(20, 120);
				rl.EndDrawing();
			}

			Raylib.CloseWindow();
			return 0;
		}
	}
}