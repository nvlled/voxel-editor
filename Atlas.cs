
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
}