
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
        public void AddLookup(string tileName, Atlas atlas, int[] ids, bool sides = false)
        {
            if (sides)
            {
                System.Diagnostics.Debug.Assert(ids.Length == 6);
                lookupTable[tileName] = (atlas, new TileID.Sides(
                    ids[0], ids[1], ids[2], ids[3], ids[4], ids[5]
                ));
            }
            else
            {
                lookupTable[tileName] = (atlas, new TileID.Any(ids));
            }
        }

        Rectangle GetRectangle(Atlas atlas, int id)
        {

            var i = id / atlas.cols;
            var j = id % atlas.cols;
            var x = j * atlas.tileSize;
            var y = i * atlas.tileSize;

            return new Rectangle(x, y, atlas.tileSize, atlas.tileSize);
        }

        public SubTexture2D[] Lookup(string tileName)
        {
            var sepIndex = tileName.IndexOf(":");
            var shuffle = false;
            if (sepIndex >= 0)
            {
                var flags = tileName.Substring(sepIndex);
                tileName = tileName.Substring(0, sepIndex);
                foreach (var ch in flags)
                {
                    switch (ch)
                    {
                        case '*': shuffle = true; break;
                    }
                }
            }

            var (atlas, tileID) = lookupTable[tileName];
            if (atlas.id == 0)
            {
                return null;
            }
            var result = tileID switch
            {
                TileID.One oneID => new SubTexture2D[] {
                new SubTexture2D(atlas.texture, GetRectangle(atlas, oneID.value))
                },

                TileID.Any anyID when shuffle =>
                    anyID.ids.Select(id =>
                        new SubTexture2D(atlas.texture, GetRectangle(atlas, id))
                    ).ToArray(),

                TileID.Any anyID => new SubTexture2D[] {
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, anyID.ids[rand.Next(0, anyID.ids.Length)])),
                },

                TileID.Sides sidesID => new SubTexture2D[] {
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.top)),
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.left)),
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.right)),
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.front)),
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.back)),
                    new SubTexture2D(atlas.texture, GetRectangle(atlas, sidesID.bottom)),
                },
                _ => new SubTexture2D[0],
            };

            if (shuffle)
            {
                Array.Sort(result, (a, b) => rl.GetRandomValue(-1, 1));
            }

            return result;
        }

        public static AtlasManager Init()
        {
            var atlasMan = new AtlasManager();
            var rogueAtlas = new Atlas(1, rl.LoadTexture("./assets/atlas.png"), 32);
            atlasMan.AddAtlas(rogueAtlas);

            atlasMan.AddLookup("greenwall-1", rogueAtlas, 2, 1);
            atlasMan.AddLookup("greenwall", rogueAtlas, new int[] { 192, 193, 194, 195 });
            atlasMan.AddLookup("mazewall", rogueAtlas, new int[] { 196, 197, 198, 199, 200 });
            atlasMan.AddLookup("marble", rogueAtlas, new int[] { 571, 572, 573, 574, 575, 576, 577 });
            atlasMan.AddLookup("brick", rogueAtlas, new int[] { 389, 391, 393 });
            atlasMan.AddLookup("stone", rogueAtlas, new int[] { 1045, 1046, 1047, 1048, 1049 });
            atlasMan.AddLookup("redstone", rogueAtlas, new int[] { 1057, 1058, 1059, 1060, 1061, 1062 });
            atlasMan.AddLookup("blue", rogueAtlas, new int[] { 1229, 1230, 1231, 1232, 1234, 1233 });
            atlasMan.AddLookup("water", rogueAtlas, new int[] { 1473, 1474, 1475, 1476, 1477 });
            atlasMan.AddLookup("acid", rogueAtlas, new int[] { 1421, 1422, 1423, 1424 });
            atlasMan.AddLookup("face", rogueAtlas, new int[] { 1026, 1027, 1028 });
            atlasMan.AddLookup("syms", rogueAtlas, new int[] { 1153, 1154, 1155, 1156, 1157, 1158, 1159, 1160, 1161 });
            atlasMan.AddLookup("bee", rogueAtlas, new int[] { 877, 878, 879, 880, 881, 882 });
            atlasMan.AddLookup("flesh", rogueAtlas, new int[] { 210, 207, 209, 206, 204, 205 }, true);

            atlasMan.lookupTable["default"] = atlasMan.lookupTable["syms"];


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