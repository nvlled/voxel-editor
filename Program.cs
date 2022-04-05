
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;

namespace HelloWorld
{

	public class Atlas
	{

		public Texture2D texture;
		public float tileSize;
		public Atlas(Texture2D texture, float tileSize)
		{
			this.texture = texture;
			this.tileSize = tileSize;
		}

		public void GetRect(int i, int j, out Rectangle result)
		{
			var size = tileSize;
			result.x = size * (float)j;
			result.y = size * (float)i;
			result.width = size;
			result.height = size;
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
		public static async void DrawOrigin()
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
				// TODO: rotate not working
				// never, it takes angles, not radians...
				gl.rlRotatef(90, 1, 0, 0);
				gl.rlTranslatef(0, 0.0f, -1.0f);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "X", new Vector3(3, 0, 0), 24, 1, 1, true, xColor);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "Y", new Vector3(0, 0, -3), 24, 1, 1, true, yColor);
				RaylibExt.DrawText3D(rl.GetFontDefault(), "Z", new Vector3(0, 3, 0), 24, 1, 1, true, zColor);
			});

		}
	}

	enum CubeSide
	{
		TOP,
		LEFT,
		RIGHT,
		FRONT,
		BACK,
		BOTTOM,
	}

	class SubTexture2D
	{
		public Texture2D texture;
		public Rectangle rect;
	}

	class Voxel
	{
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
			AlignPosition(pos);
			rl.DrawCube(new Vector3(pos.X + offset, pos.Y + offset, pos.Z + offset), 1, 1, 1, color);
		}
		public static void DrawCubeWires(Vector3 pos, Color color)
		{
			var offset = 0.0f;
			AlignPosition(pos);
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


		public static void DrawTile(Camera3D camera, Vector3 position, SubTexture2D subTexture, CubeSide side, Vector2 size, Vector2? originArg = null, float rotation = 0, Color? colorArg = null)
		{
			var up = Vector3.Zero;
			var right = Vector3.Zero;
			var zeroOffset = 0.0001f;
			var offset = -0.5f;
			switch (side)
			{
				case CubeSide.TOP:
					up = new Vector3(0, 0, -1);
					right = new Vector3(1, 0, 0);
					position.Z += 0;
					position.Y += 0.5f;
					position.X += 0;
					break;
				case CubeSide.BOTTOM:
					up = new Vector3(0, 0, -1);
					right = new Vector3(-1, 0, 0);
					position.Z += 0.0f;
					position.Y += 0.0f;
					position.X += 0.0f;
					break;
				case CubeSide.FRONT:
					up = new Vector3(0, 1, 0);
					right = new Vector3(1, 0, 0);
					position.Z += 0.5f;
					position.Y += 0;
					position.X += 0;
					break;
				case CubeSide.BACK:
					up = new Vector3(0, 1, 0);
					right = new Vector3(-1, 0, 0);
					position.Z += -0.5f;
					position.Y += 0;
					position.X += 0;
					break;
				case CubeSide.RIGHT:
					up = new Vector3(0, 1, 0);
					right = new Vector3(0, 0, 1);
					position.Z += 0;
					position.Y += 0;
					position.X += -0.5f;
					break;
				case CubeSide.LEFT:
					up = new Vector3(0, 1, 0);
					right = new Vector3(0, 0, -1);
					position.Z += 0;
					position.Y += 0;
					position.X += 0.5f;
					break;
			}

			var source = subTexture.rect;
			var texture = subTexture.texture;

			// NOTE: Billboard size will maintain source rectangle aspect ratio, size will represent billboard width
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

		public unsafe static void Main()
		{
			rl.InitWindow(1300, 800, "Hello World");

			var cam = new Camera3D(
			 position: new Vector3(0, 1.5f, 0),
			 target: new Vector3(-0.5f, 0, -0.5f),
			 up: new Vector3(0, 1, 0),
			 40,
			 CameraProjection.CAMERA_PERSPECTIVE
			);

			var tree = rl.LoadTexture("assets/tree_single.png");
			var shader = rl.LoadShader("", "assets/discard_alpha.fs");

			var atlas = new Atlas(rl.LoadTexture("assets/atlas.png"), 32);

			rl.SetCameraMode(cam, CameraMode.CAMERA_FIRST_PERSON);
			rl.SetTargetFPS(60);


			var cubes1 = new System.Collections.Generic.List<Vector3>();
			var cubes2 = new System.Collections.Generic.List<Vector3>();

			//gl.rlScalef(5.1f, 0.1f, 0.1f);
			//gl.rlMultMatrixf(new Matrix4x4(
			//	0.1f, 0.0f, 0.0f, 0.0f,
			//	0.0f, 0.1f, 0.0f, 0.0f,
			//	0.0f, 0.0f, 0.1f, 0.0f,
			//	0.0f, 0.0f, 0.0f, 1.0f
			//));

			while (!rl.WindowShouldClose())
			{

				var ray = rl.GetMouseRay(new Vector2(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2), cam);

				if (rl.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
				{
					cubes1.Add(Voxel.AlignPosition(ray.position + ray.direction * 1.5f));
					cubes2.Add(ray.position);
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
					cam.position += new Vector3(0, 1, 0);
				}
				else
				{
					rl.UpdateCamera(&cam);
				}



				// TODO: 
				// Two problems with the default camera implementation:
				// 1. it has a dumb swinging movement that makes me sick
				// 2. it fixes the Y-value at a constant position,
				//    thus preventing me from making jumping movements
				// 3. it even uses a global state, thus makes it
				//    it difficult to switch to several camera instances
				//  use raylib-extras/extras-cs ?

				// That all aside,
				// I should use CAMERA_CUSTOM mode for first-person view
				// I can't use the camera from raylibExtras since I would
				// end up re-implementing a lot of stuffs on my own,
				// plus I won't be able to call functions that use Camera in the raylib lib.

				// TODO: world data
				// TODO: if tile is already occupied, shift tile cursor towards camera
				// TODO: should only allow cube insertion on ground, or adjacent to other cubes,
				//       not on empty space 
				// TODO: tile collision


				rl.BeginDrawing();
				rl.ClearBackground(Color.WHITE);
				rl.DrawText(string.Format("x={0} y={1} z={2}", (int)cam.position.X, (int)cam.position.Y, (int)cam.position.Z), 10, 10, 15, Color.BLACK);


				rl.BeginMode3D(cam);
				Debug.DrawOrigin();



				rl.BeginShaderMode(shader);

				var srcRec = new Rectangle(0, 0, tree.width, tree.height);
				var up = new Vector3(0, 1, 0);
				var size = new Vector2(1, 1);

				//Voxel.DrawSurroundingCubeWires(Voxel.AlignPosition(cam.position), Color.GREEN);
				//Voxel.DrawCube(new Vector3(-5, 0, 0), Color.BLUE);

				//gl.rlPushMatrix();
				//gl.rlRotatef(a++, 1, 0, 0);

				//gl.rlPushMatrix();
				//gl.rlScalef(0.1f, 0.1f, 0.1f);
				foreach (var c in cubes1)
				{
					Voxel.DrawCube(c, Color.BLACK);
					Voxel.DrawCubeWires(c, new Color(0, 255, 255, 255));
				}
				foreach (var c in cubes2)
				{
					//Voxel.DrawCube(c, Color.PURPLE);
				}
				Voxel.DrawCubeWires(Voxel.AlignPosition(ray.position + ray.direction * 1.5f), Color.BLACK);

				//rl.DrawCube(Voxel.AlignPosition(Voxel.AlignPosition(new Vector3(-6, 0, 0)) + (ray.direction * 1)), 1.0f, 1.0f, 1.0f, Color.YELLOW);
				//Voxel.DrawCube(cam.position + (ray.direction * 2), Color.GREEN);

				//rl.DrawRay(new Ray(cam.position + Vector3.Normalize(Vector3.Zero) * -0.5f, Vector3.Normalize(Vector3.Zero) * 2), Color.BLUE);

				Voxel.DrawCube(Vector3.Zero, new Color(0, 255, 255, 180));

				var subTex = new SubTexture2D { rect = new Rectangle(), texture = atlas.texture };
				atlas.GetRect(1, 1, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.FRONT, size: size);
				atlas.GetRect(2, 2, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.RIGHT, size: size);
				atlas.GetRect(1, 5, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.LEFT, size: size);
				atlas.GetRect(1, 4, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.BACK, size: size);
				atlas.GetRect(1, 3, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.TOP, size: size);
				atlas.GetRect(2, 1, out subTex.rect);
				Voxel.DrawTile(camera: cam, subTexture: subTex, position: Vector3.Zero, side: CubeSide.BOTTOM, size: size);
				//gl.rlPopMatrix();



				gl.rlPushMatrix();
				gl.rlTranslatef(-5, 0, 0);
				rl.DrawCube(new Vector3(0, 0, 0), 1, 1, 1, Color.BROWN);
				gl.rlPopMatrix();

				rl.EndShaderMode();

				rl.EndMode3D();




				// crosshair
				rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, 5, Color.BLACK);
				rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, 3, Color.RED);
				rl.DrawText(Vector3.Normalize(ray.position).ToString(), 50, 50, 20, Color.BLACK);
				rl.DrawText(Vector3.Normalize(ray.direction).ToString(), 50, 80, 20, Color.BLACK);
				rl.DrawText(Vector3.Normalize(cam.target).ToString(), 50, 100, 20, Color.BLUE);
				rl.DrawFPS(rl.GetScreenWidth() - 100, 10);

				rl.EndDrawing();
			}

			Raylib.CloseWindow();
		}
	}
}