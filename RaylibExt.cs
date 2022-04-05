
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;
using System.Text;

namespace HelloWorld
{
	unsafe class RaylibExt
	{
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