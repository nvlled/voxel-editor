
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;
using System.Text;
using System.Collections.Generic;
using Math = System.MathF;

namespace HelloWorld
{

	public class FPCamera
	{
		public enum CameraControls
		{
			MOVE_FRONT = 0,
			MOVE_BACK,
			MOVE_RIGHT,
			MOVE_LEFT,
			MOVE_UP,
			MOVE_DOWN,
			TURN_LEFT,
			TURN_RIGHT,
			TURN_UP,
			TURN_DOWN,
			SPRINT,
		}

		public Dictionary<CameraControls, KeyboardKey> ControlsKeys = new Dictionary<CameraControls, KeyboardKey>();

		public Vector3 MoveSpeed = new Vector3(1, 1, 1);
		public Vector2 TurnSpeed = new Vector2(90, 90);

		public float MouseSensitivity = 600;

		public float MinimumViewY = -65.0f;
		public float MaximumViewY = 89.0f;

		public float ViewBobbleFreq = 0.0f;
		public float ViewBobbleMagnatude = 0.02f;
		public float ViewBobbleWaverMagnitude = 0.002f;

		public delegate bool PositionCallback(FPCamera camera, Vector3 newPosition, Vector3 oldPosition);
		public PositionCallback ValidateCamPosition = null;

		public Camera3D Camera { get { return ViewCamera; } }

		public bool UseMouseX = true;
		public bool UseMouseY = true;

		public bool UseKeyboard = true;

		public bool UseController = true;
		public bool ControlerID = false;

		//clipping planes
		// note must use BeginMode3D and EndMode3D on the camera object for clipping planes to work
		public double NearPlane = 0.01;
		public double FarPlane = 1000;

		public bool HideCursor = true;

		protected bool Focused = true;
		protected Vector3 CameraPosition = new Vector3(0.0f, 0.0f, 0.0f);

		protected Camera3D ViewCamera = new Camera3D();
		protected Vector2 FOV = new Vector2(0.0f, 0.0f);

		protected Vector2 PreviousMousePosition = new Vector2(0.0f, 0.0f);

		protected float TargetDistance = 0;               // Camera distance from position to target
		protected float PlayerEyesPosition = 1.5f;       // Player eyes position from ground (in meters)
		protected Vector2 Angle = new Vector2(0, 0);                // Camera angle in plane XZ

		protected float CurrentBobble = 0;

		public float JumpHeight = 0;

		public FPCamera()
		{
			ControlsKeys.Add(CameraControls.MOVE_FRONT, KeyboardKey.KEY_W);
			ControlsKeys.Add(CameraControls.MOVE_BACK, KeyboardKey.KEY_S);
			ControlsKeys.Add(CameraControls.MOVE_LEFT, KeyboardKey.KEY_A);
			ControlsKeys.Add(CameraControls.MOVE_RIGHT, KeyboardKey.KEY_D);
			ControlsKeys.Add(CameraControls.MOVE_UP, KeyboardKey.KEY_E);
			ControlsKeys.Add(CameraControls.MOVE_DOWN, KeyboardKey.KEY_Q);

			ControlsKeys.Add(CameraControls.TURN_LEFT, KeyboardKey.KEY_LEFT);
			ControlsKeys.Add(CameraControls.TURN_RIGHT, KeyboardKey.KEY_RIGHT);
			ControlsKeys.Add(CameraControls.TURN_UP, KeyboardKey.KEY_UP);
			ControlsKeys.Add(CameraControls.TURN_DOWN, KeyboardKey.KEY_DOWN);
			ControlsKeys.Add(CameraControls.SPRINT, KeyboardKey.KEY_LEFT_SHIFT);

			PreviousMousePosition = Raylib.GetMousePosition();
		}

		public void Setup(float fovY, Vector3 position)
		{
			CameraPosition = new Vector3(position.X, position.Y, position.Z);
			ViewCamera.position = new Vector3(position.X, position.Y, position.Z);
			ViewCamera.position.Y += PlayerEyesPosition;
			ViewCamera.target = ViewCamera.position + new Vector3(0, 0, 1);
			ViewCamera.up = new Vector3(0, 1, 0);
			ViewCamera.fovy = fovY;
			ViewCamera.projection = CameraProjection.CAMERA_PERSPECTIVE;

			Focused = rl.IsWindowFocused();
			if (HideCursor && Focused && (UseMouseX || UseMouseY))
				rl.DisableCursor();

			TargetDistance = 2;

			ViewResized();
		}

		public void ViewResized()
		{
			float width = (float)rl.GetScreenWidth();
			float height = (float)rl.GetScreenHeight();

			FOV.Y = ViewCamera.fovy;

			if (height != 0)
				FOV.X = FOV.Y * (width / height);
		}

		protected float GetSpeedForAxis(CameraControls axis, float speed)
		{
			if (!UseKeyboard || !ControlsKeys.ContainsKey(axis))
				return 0;

			KeyboardKey key = ControlsKeys[axis];
			if (key == KeyboardKey.KEY_NULL)
				return 0;

			float factor = 1.0f;
			if (rl.IsKeyDown(ControlsKeys[CameraControls.SPRINT]))
				factor = 2;

			if (rl.IsKeyDown(key))
				return speed * rl.GetFrameTime() * factor;

			return 0.0f;
		}


		public void Update()
		{
			if (HideCursor && rl.IsWindowFocused() != Focused && (UseMouseX || UseMouseY))
			{
				Focused = rl.IsWindowFocused();
				if (Focused)
				{
					rl.DisableCursor();
				}
				else
				{
					rl.EnableCursor();
				}
			}

			Vector2 mousePositionDelta = rl.GetMousePosition() - PreviousMousePosition;
			PreviousMousePosition = rl.GetMousePosition();

			// Mouse movement detection
			float mouseWheelMove = rl.GetMouseWheelMove();

			// Keys input detection
			Dictionary<CameraControls, float> directions = new Dictionary<CameraControls, float>();
			directions[CameraControls.MOVE_FRONT] = GetSpeedForAxis(CameraControls.MOVE_FRONT, MoveSpeed.Z);
			directions[CameraControls.MOVE_BACK] = GetSpeedForAxis(CameraControls.MOVE_BACK, MoveSpeed.Z);
			directions[CameraControls.MOVE_RIGHT] = GetSpeedForAxis(CameraControls.MOVE_RIGHT, MoveSpeed.X);
			directions[CameraControls.MOVE_LEFT] = GetSpeedForAxis(CameraControls.MOVE_LEFT, MoveSpeed.X);
			directions[CameraControls.MOVE_UP] = GetSpeedForAxis(CameraControls.MOVE_UP, MoveSpeed.Y);
			directions[CameraControls.MOVE_DOWN] = GetSpeedForAxis(CameraControls.MOVE_DOWN, MoveSpeed.Y);


			Vector3 forward = ViewCamera.target - ViewCamera.position;
			forward.Y = 0;
			forward = Vector3.Normalize(forward);

			Vector3 right = new Vector3(forward.Z * -1.0f, 0, forward.X);

			Vector3 oldPosition = CameraPosition;

			CameraPosition += (forward * (directions[CameraControls.MOVE_FRONT] - directions[CameraControls.MOVE_BACK]));
			CameraPosition += (right * (directions[CameraControls.MOVE_RIGHT] - directions[CameraControls.MOVE_LEFT]));

			CameraPosition.Y += directions[CameraControls.MOVE_UP] - directions[CameraControls.MOVE_DOWN];

			// let someone modify the projected position
			if (ValidateCamPosition != null)
				ValidateCamPosition(this, CameraPosition, oldPosition);

			// Camera orientation calculation
			float turnRotation = GetSpeedForAxis(CameraControls.TURN_RIGHT, TurnSpeed.X) - GetSpeedForAxis(CameraControls.TURN_LEFT, TurnSpeed.X);
			float tiltRotation = GetSpeedForAxis(CameraControls.TURN_UP, TurnSpeed.Y) - GetSpeedForAxis(CameraControls.TURN_DOWN, TurnSpeed.Y);

			if (turnRotation != 0)
				Angle.X -= turnRotation * (Math.PI / 180.0f);
			else if (UseMouseX && Focused)
				Angle.X += (mousePositionDelta.X / -MouseSensitivity);

			if (tiltRotation != 0)
				Angle.Y += tiltRotation * (Math.PI / 180.0f);
			else if (UseMouseY && Focused)
				Angle.Y += (mousePositionDelta.Y / -MouseSensitivity);

			// Angle clamp
			if (Angle.Y < MinimumViewY * (Math.PI / 180.0f))
				Angle.Y = MinimumViewY * (Math.PI / 180.0f);
			else if (Angle.Y > MaximumViewY * (Math.PI / 180.0f))
				Angle.Y = MaximumViewY * (Math.PI / 180.0f);

			// Recalculate camera target considering translation and rotation
			Vector3 target = Raymath.Vector3Transform(new Vector3(0, 0, 1), Raymath.MatrixRotateXYZ(new Vector3(Angle.Y, -Angle.X, 0)));

			ViewCamera.position = CameraPosition;

			float eyeOfset = PlayerEyesPosition;

			if (ViewBobbleFreq > 0)
			{
				float swingDelta = Math.Max(Math.Abs(directions[CameraControls.MOVE_FRONT] - directions[CameraControls.MOVE_BACK]), Math.Abs(directions[CameraControls.MOVE_RIGHT] - directions[CameraControls.MOVE_LEFT]));

				// If movement detected (some key pressed), increase swinging
				CurrentBobble += swingDelta * ViewBobbleFreq;

				const float ViewBobbleDampen = 8.0f;

				eyeOfset -= Math.Sin(CurrentBobble / ViewBobbleDampen) * ViewBobbleMagnatude;

				ViewCamera.up.X = Math.Sin(CurrentBobble / (ViewBobbleDampen * 2)) * ViewBobbleWaverMagnitude;
				ViewCamera.up.Z = -Math.Sin(CurrentBobble / (ViewBobbleDampen * 2)) * ViewBobbleWaverMagnitude;
			}
			else
			{
				CurrentBobble = 0;
				ViewCamera.up.X = 0;
				ViewCamera.up.Z = 0;
			}

			//ViewCamera.position.Y = eyeOfset + JumpHeight;
			ViewCamera.position.Y += eyeOfset;

			ViewCamera.target = ViewCamera.position + target;
		}

		public float GetFOVX()
		{
			return FOV.X;
		}

		public Vector3 GetCameraPosition()
		{
			return CameraPosition;
		}

		public void SetCameraPosition(Vector3 pos)
		{
			CameraPosition = pos;
			Vector3 forward = ViewCamera.target - ViewCamera.position;
			ViewCamera.position = CameraPosition;
			ViewCamera.target = CameraPosition + forward;
		}

		public Vector2 GetViewAngles()
		{
			return Raymath.Vector2Scale(Angle, (float)(Math.PI / 180.0f));
		}

		// start drawing using the camera, with near/far plane support
		public void BeginMode3D()
		{
			float aspect = (float)rl.GetScreenWidth() / (float)rl.GetScreenHeight();

			gl.rlDrawRenderBatchActive();           // Draw Buffers (Only OpenGL 3+ and ES2)
			gl.rlMatrixMode(MatrixMode.PROJECTION); // Switch to projection matrix
			gl.rlPushMatrix();                      // Save previous matrix, which contains the settings for the 2d ortho projection
			gl.rlLoadIdentity();                    // Reset current matrix (projection)

			if (ViewCamera.projection == CameraProjection.CAMERA_PERSPECTIVE)
			{
				// Setup perspective projection
				double top = gl.RL_CULL_DISTANCE_NEAR * Math.Tan(ViewCamera.fovy * 0.5f * (Math.PI / 180.0f));
				double right = top * aspect;

				gl.rlFrustum(-right, right, -top, top, NearPlane, FarPlane);
			}
			else if (ViewCamera.projection == CameraProjection.CAMERA_ORTHOGRAPHIC)
			{
				// Setup orthographic projection
				double top = ViewCamera.fovy / 2.0;
				double right = top * aspect;

				gl.rlOrtho(-right, right, -top, top, NearPlane, FarPlane);
			}

			// NOTE: zNear and zFar values are important when computing depth buffer values

			gl.rlMatrixMode(MatrixMode.MODELVIEW); // Switch back to modelview matrix
			gl.rlLoadIdentity();                   // Reset current matrix (modelview)

			// Setup Camera view
			Matrix4x4 matView = Raymath.MatrixLookAt(ViewCamera.position, ViewCamera.target, ViewCamera.up);

			gl.rlMultMatrixf(matView);      // Multiply modelview matrix by view matrix (camera)

			gl.rlEnableDepthTest();    // Enable DEPTH_TEST for 3D
		}

		// end drawing with the camera
		public void EndMode3D()
		{
			rl.EndMode3D();
		}
	}

	unsafe class RaylibExt
	{
		public static float[] MatrixToBuffer(Matrix4x4 matrix)
		{
			float[] buffer = new float[16];

			buffer[0] = matrix.M11;
			buffer[1] = matrix.M21;
			buffer[2] = matrix.M31;
			buffer[3] = matrix.M41;

			buffer[4] = matrix.M12;
			buffer[5] = matrix.M22;
			buffer[6] = matrix.M32;
			buffer[7] = matrix.M42;

			buffer[8] = matrix.M13;
			buffer[9] = matrix.M23;
			buffer[10] = matrix.M33;
			buffer[11] = matrix.M43;

			buffer[12] = matrix.M14;
			buffer[13] = matrix.M24;
			buffer[14] = matrix.M34;
			buffer[15] = matrix.M44;

			return buffer;
		}
		public static void WithMatrix(System.Action fn)
		{
			gl.rlPushMatrix();
			fn();
			gl.rlPopMatrix();
		}

		public static void DrawTextCodepoint3D(Font font, int codepoint, Vector3 position, float fontSize, bool backface, Color tint)
		{

			// Character index position in sprite font
			// NOTE: In case a codepoint is not available in the font, index returned points to '?'
			int index = rl.GetGlyphIndex(font, codepoint);
			float scale = fontSize / (float)font.baseSize;

			// Character destination rectangle on screen
			// NOTE: We consider charsPadding on drawing
			position.X += (float)(font.glyphs[index].offsetX - font.glyphPadding) / (float)font.baseSize * scale;
			position.Z += (float)(font.glyphs[index].offsetY - font.glyphPadding) / (float)font.baseSize * scale;

			// Character source rectangle from font texture atlas
			// NOTE: We consider chars padding when drawing, it could be required for outline/glow shader effects
			var srcRec = new Rectangle
			{
				x = font.recs[index].x - (float)font.glyphPadding,
				y = font.recs[index].y - (float)font.glyphPadding,
				width = font.recs[index].width + 2.0f * font.glyphPadding,
				height = font.recs[index].height + 2.0f * font.glyphPadding
			};

			var width = (float)(font.recs[index].width + 2.0f * font.glyphPadding) / (float)font.baseSize * scale;
			var height = (float)(font.recs[index].height + 2.0f * font.glyphPadding) / (float)font.baseSize * scale;

			if (font.texture.id > 0)
			{
				const float x = 0.0f;
				const float y = 0.0f;
				const float z = 0.0f;

				// normalized texture coordinates of the glyph inside the font texture (0.0f -> 1.0f)
				var tx = srcRec.x / font.texture.width;
				var ty = srcRec.y / font.texture.height;
				var tw = (srcRec.x + srcRec.width) / font.texture.width;
				var th = (srcRec.y + srcRec.height) / font.texture.height;

				if (Text3DSettings.SHOW_LETTER_BOUNDRY)
				{
					var pos = new Vector3 { X = position.X + width / 2, Y = position.Y, Z = position.Z + height / 2 };
					var size = new Vector3 { X = width, Y = Text3DSettings.LETTER_BOUNDRY_SIZE, Z = height };
					rl.DrawCubeWiresV(pos, size, Text3DSettings.LETTER_BOUNDRY_COLOR);
				}

				gl.rlCheckRenderBatchLimit(4 + 4 * (backface ? 1 : 0));
				gl.rlSetTexture(font.texture.id);

				gl.rlPushMatrix();
				gl.rlTranslatef(position.X, position.Y, position.Z);

				gl.rlBegin(Raylib_cs.DrawMode.QUADS);
				gl.rlColor4ub(tint.r, tint.g, tint.b, tint.a);

				// Front Face
				gl.rlNormal3f(0.0f, 1.0f, 0.0f);                                   // Normal Pointing Up
				gl.rlTexCoord2f(tx, ty); gl.rlVertex3f(x, y, z);              // Top Left Of The Texture and Quad
				gl.rlTexCoord2f(tx, th); gl.rlVertex3f(x, y, z + height);     // Bottom Left Of The Texture and Quad
				gl.rlTexCoord2f(tw, th); gl.rlVertex3f(x + width, y, z + height);     // Bottom Right Of The Texture and Quad
				gl.rlTexCoord2f(tw, ty); gl.rlVertex3f(x + width, y, z);              // Top Right Of The Texture and Quad

				if (backface)
				{
					// Back Face
					gl.rlNormal3f(0.0f, -1.0f, 0.0f);                              // Normal Pointing Down
					gl.rlTexCoord2f(tx, ty); gl.rlVertex3f(x, y, z);          // Top Right Of The Texture and Quad
					gl.rlTexCoord2f(tw, ty); gl.rlVertex3f(x + width, y, z);          // Top Left Of The Texture and Quad
					gl.rlTexCoord2f(tw, th); gl.rlVertex3f(x + width, y, z + height); // Bottom Left Of The Texture and Quad
					gl.rlTexCoord2f(tx, th); gl.rlVertex3f(x, y, z + height); // Bottom Right Of The Texture and Quad
				}
				gl.rlEnd();
				gl.rlPopMatrix();

				gl.rlSetTexture(0);
			}
		}

		public static void DrawText3D(Font font, string text, Vector3 position, float fontSize, float fontSpacing, float lineSpacing, bool backface, Color tint)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(text);

			unsafe
			{
				fixed (byte* p = bytes)
				{
					sbyte* sp = (sbyte*)p;
					DrawText3D(font, sp, position, fontSize, fontSpacing, lineSpacing, backface, tint);
				}
			}
		}
		public static void DrawText3D(Font font, sbyte* text, Vector3 position, float fontSize, float fontSpacing, float lineSpacing, bool backface, Color tint)
		{
			var length = rl.TextLength(text);          // Total length in bytes of the text, scanned by codepoints in loop

			float textOffsetY = 0.0f;               // Offset between lines (on line break '\n')
			float textOffsetX = 0.0f;               // Offset X to next character to draw

			float scale = fontSize / (float)font.baseSize;

			for (int i = 0; i < length;)
			{
				// Get next codepoint from byte string and glyph index in font
				int codepointByteCount = 0;
				int codepoint = rl.GetCodepoint(&text[i], &codepointByteCount);
				int index = rl.GetGlyphIndex(font, codepoint);

				// NOTE: Normally we exit the decoding sequence as soon as a bad byte is found (and return 0x3f)
				// but we need to draw all of the bad bytes using the '?' symbol moving one byte
				if (codepoint == 0x3f) codepointByteCount = 1;

				if (codepoint == '\n')
				{
					// NOTE: Fixed line spacing of 1.5 line-height
					// TODO: Support custom line spacing defined by user
					textOffsetY += scale + lineSpacing / (float)font.baseSize * scale;
					textOffsetX = 0.0f;
				}
				else
				{
					if ((codepoint != ' ') && (codepoint != '\t'))
					{
						var v = new Vector3 { X = position.X + textOffsetX, Y = position.Y, Z = position.Z + textOffsetY };
						DrawTextCodepoint3D(font, codepoint, v, fontSize, backface, tint);
					}

					if (font.glyphs[index].advanceX == 0)
					{
						textOffsetX += (float)(font.recs[index].width + fontSpacing) / (float)font.baseSize * scale;
					}
					else
					{
						textOffsetX += (float)(font.glyphs[index].advanceX + fontSpacing) / (float)font.baseSize * scale;
					}
				}

				i += codepointByteCount;   // Move text bytes counter to next codepoint
			}
		}
	}
}