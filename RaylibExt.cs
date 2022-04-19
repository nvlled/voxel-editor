
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;
using System.Text;
using System.Collections.Generic;
using Math = System.MathF;
using System;

namespace VoxelEditor
{
	unsafe class R
	{
		public static Vector3 Vector3Half = Vector3.One * 0.5f;
		public static Vector3 Vector3Oneth = Vector3.One * 0.1f;

		public static bool InCube(Vector3 cubePos, Vector3 pos, float cubeSize = 1)
		{
			var start = cubePos;
			var end = cubePos + Vector3.One * cubeSize;
			return pos.X >= start.X && pos.Y >= start.Y && pos.Z >= start.Z
				&& pos.X < end.X && pos.Y < end.Y && pos.Z < end.Z;
		}

		public static Vector3 Vector3Project(Vector3 v, Vector3 w)
		{
			return Vector3.Dot(v, w) / w.LengthSquared() * w;
		}

		public static Vector3 Vector3Abs(Vector3 v)
		{
			v.X = MathF.Abs(v.X);
			v.Y = MathF.Abs(v.Y);
			v.Z = MathF.Abs(v.Z);
			return v;
		}

		public static Vector3 Vector3Round(Vector3 v)
		{
			v.X = MathF.Round(v.X, 6);
			v.Y = MathF.Round(v.Y, 6);
			v.Z = MathF.Round(v.Z, 6);
			return v;
		}

		public static Vector3 ClampV(Vector3 v, Vector3 min, Vector3 max)
		{
			v.X = MathF.Min(MathF.Max(v.X, min.X), max.X);
			v.Y = MathF.Min(MathF.Max(v.Y, min.Y), max.Y);
			v.Z = MathF.Min(MathF.Max(v.Z, min.Z), max.Z);

			return v;
		}

		public static Vector3 CrossN(Vector3 v1, Vector3 v2)
		{
			return Vector3.Normalize(Vector3.Cross(v1, v2));
		}


		public static IEnumerable<Vector2> IterateOutwards(int width, int height, int stepSize)
		{
			var w = width / 2;
			var h = height / 2;
			yield return new Vector2(0, 0);

			for (var x = -w; x <= w; x += stepSize)
			{
				yield return new Vector2(x, 0);
				yield return new Vector2(x, h);
				yield return new Vector2(x, -h);
			}
			for (var y = -h; y <= h; y += stepSize)
			{
				yield return new Vector2(0, y);
				yield return new Vector2(w, y);
				yield return new Vector2(-w, y);
			}


			for (var x = -w + stepSize; x < w; x += stepSize)
			{
				for (var y = -h + stepSize; y < h; y += stepSize)
				{
					if (x == 0 || y == 0)
					{
						continue;
					}
					yield return new Vector2(x, y);
				}
			}

		}

		public static Vector2 GetFarsideDimension(float fov, float renderDistance)
		{
			return new Vector2(
			2 * MathF.Tan(rl.DEG2RAD * fov / 2) * renderDistance,
			2 * MathF.Tan(rl.DEG2RAD * fov / 2) * renderDistance
			);
		}


		public static (Vector3, Vector3) GetOrthogonalAxis(Vector3 direction, Vector3 worldUp)
		{
			var right = R.CrossN(direction, worldUp);
			var up = R.CrossN(right, direction);
			//var left = Vector3.Negate(right);
			//var down = Vector3.Negate(up);
			//var xStep = Vector3.Normalize(right - left);
			//var yStep = Vector3.Normalize(up - down);

			//return (xStep, yStep);
			return (right, up);
		}

		// Draws on both side
		public static void DrawPlane(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
		{
			var v1 = p2 - p1;
			var v2 = p3 - p2;
			var centerPos = p1 + v1 / 2 + v2 / 2;

			gl.rlCheckRenderBatchLimit(8);

			gl.rlPushMatrix();

			gl.rlBegin(DrawMode.QUADS);
			gl.rlColor4ub(color.r, color.g, color.b, color.a);

			gl.rlVertex3f(p1.X, p1.Y, p1.Z);
			gl.rlVertex3f(p2.X, p2.Y, p2.Z);
			gl.rlVertex3f(p3.X, p3.Y, p3.Z);
			gl.rlVertex3f(p4.X, p4.Y, p4.Z);

			gl.rlVertex3f(p4.X, p4.Y, p4.Z);
			gl.rlVertex3f(p3.X, p3.Y, p3.Z);
			gl.rlVertex3f(p2.X, p2.Y, p2.Z);
			gl.rlVertex3f(p1.X, p1.Y, p1.Z);

			gl.rlEnd();
			gl.rlPopMatrix();
		}

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