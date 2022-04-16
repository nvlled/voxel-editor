
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
	public class Config
	{
		public static int renderDistance = 100;
		public static int chunkSize = 8;
		public static float fov = 50;
		public static int fps = 60;
		public static int windowWidth = 1000;
		public static int windowHeight = 800;
		public static float cameraHeight = 1.5f;
		public static float runSpeed = 5.0f;
		public static float walkSpeed = 1.5f;
	}


	public static class Text3DSettings
	{

		public const float LETTER_BOUNDRY_SIZE = 0.25f;
		public const float TEXT_MAX_LAYERS = 32;
		public static Color LETTER_BOUNDRY_COLOR = Color.VIOLET;

		public static bool SHOW_LETTER_BOUNDRY = false;
		public static bool SHOW_TEXT_BOUNDRY = false;
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

			var world = new VoxelWorld(new Vector3(-50, -50, -50), new Vector3(50, 50, 50));
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
			world.InsertCube(new Vector3(0, 0, 0), "greenwall");
			world.InsertCube(new Vector3(1, 0, 0), "greenwall");
			world.InsertCube(new Vector3(0, 1, 0), "greenwall");
			world.InsertCube(new Vector3(0, 0, 2), "greenwall");
			world.InsertCube(new Vector3(-1, 0, 0), "sand");
			world.InsertCube(new Vector3(-2, 0, 0), "sand");
			world.InsertCube(new Vector3(-3, 0, 0), "sand");
			world.InsertCube(new Vector3(-4, 0, 0), "sand");

			world.InsertCube(new Vector3(0, -1, 0), "sand");
			world.InsertCube(new Vector3(0, 0, -1), "sand");
			//world.InsertCube(new Vector3(2, 0, 0), "sand");
			//world.InsertCube(new Vector3(0, 3, 0), "mazewall");

			var rayCastedChunks = new Vector3[0];
			var savedDrawFlag = DrawFlag.Init;

			scratch(world);

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
					//rayCastedChunks = world.GetRayCastedChunks(camRay, cam);
					//savedDrawFlag = world.drawFlag;

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

				if (rl.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
				{
					//world.InsertCube(pos, "brick");
					rayCastedChunks = world.GetRayCastedChunks(camRay, cam);
					savedDrawFlag = world.drawFlag;

				}


				DrawRayCastedChunks(world, rayCastedChunks, savedDrawFlag);


				rl.BeginShaderMode(shader);

				var srcRec = new Rectangle(0, 0, tree.width, tree.height);
				var up = new Vector3(0, 1, 0);
				var size = new Vector2(1, 1);

				//Voxel.DrawCubeWires(Voxel.AlignPosition(camRay.position + camRay.direction * 1.5f), Color.RED);

				Debug.DrawOrigin();

				rl.EndShaderMode();

				cam.EndMode3D();
				rl.DrawFPS(rl.GetScreenWidth() - 100, 10);
				//rl.DrawText(string.Format("iter={0}", world.iterations), 10, 500, 22, Color.RED);
				world.DrawInfo(world, camRay);


				rl.EndDrawing();
			}

			Raylib.CloseWindow();
			return 0;
		}

		public static void DrawRayCastedChunks(VoxelWorld world, Vector3[] chunks, DrawFlag savedDrawFlag)
		{
			var chunkSize = world.chunkSize;
			foreach (var chunk in chunks)
			{

				var vs = Voxel.GetChunkVertices(chunkSize, chunk);
				var r = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.X)) / chunkSize) * 255);
				var g = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.Y)) / chunkSize) * 255);
				var b = (int)((MathF.Min(chunkSize, MathF.Abs(chunk.Z)) / chunkSize) * 255);
				var c = new Color(r, g, b, 60);
				var mid = Vector3.One * (chunkSize / 2f);
				var p = chunk * chunkSize + mid + Vector3.One * -0.5f;
				//if (chunkData != null && chunkData.drawFlag == savedDrawFlag && chunkData.cubeCount > 0)
				{
					//rl.DrawCube(p, chunkSize, chunkSize, chunkSize, c);
					rl.DrawCubeWires(p, chunkSize, chunkSize, chunkSize, Color.WHITE);
					R.DrawText3D(rl.GetFontDefault(), "*" + chunk.ToString(), p - mid, 18, 1, 2, true, Color.GREEN);
				}
				//RaylibExt.DrawPlane(vs[0], vs[1], vs[2], vs[3], c);
				//RaylibExt.DrawPlane(vs[0], vs[1], vs[2], vs[3], c);
			}
		}
		public static void scratch(VoxelWorld world)
		{
			var vs = new Vector3[] {
				new Vector3(0, 0, 1),
				new Vector3(0, 0, -1),
				new Vector3(0, 0, 0),
				new Vector3(-0, 0, 0),
				new Vector3(-1, 0, 0),
				new Vector3(1, 0, 0),
				new Vector3(world.size.X + 10, 0, 0),
			};
			foreach (var v in vs)
			{
				Console.WriteLine("pos={0} i={1}", v, world.toChunkIndex(v));
			}
		}
	}

}