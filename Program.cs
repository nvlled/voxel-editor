
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


    public class Player
    {
        public string equippedCube = "default";
        public Vector3 frontCube;
        public GameState state;
        public SubTexture2D[] equippedSubTexture;

        public delegate void PlayerAction(Vector3 frontCube, Ray ray);
        public event PlayerAction OnPrimaryAction;
        public event PlayerAction OnSecondaryAction;


        public Player()
        {
            OnPrimaryAction += InsertCube;
        }

        public void Equip(string tileName)
        {
            equippedCube = tileName;
            if (tileName != "")
            {
                equippedSubTexture = state.atlasManager.Lookup(equippedCube);
            }
        }

        public void TriggerPrimaryAction()
        {
            if (OnPrimaryAction != null)
            {
                OnPrimaryAction(frontCube, state.cam.ray);
            }
        }

        public void InsertCube(Vector3 frontCube, Ray ray)
        {
            if (equippedCube != "")
            {
                state.world.InsertCube(frontCube, equippedCube);
                equippedSubTexture = state.atlasManager.Lookup(equippedCube);
            }
        }

        public void Update()
        {
            var ray = state.cam.ray;
            frontCube = Voxel.AlignPosition(ray.position + ray.direction * 1.5f);
        }
    }

    public class GameState
    {
        public Player player;
        public VoxelWorld world;
        public AtlasManager atlasManager;
        public Camera3D cam;
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
        public record Any(int[] ids) : TileID();
        public record Sides(int top, int left, int right, int front, int back, int bottom) : TileID();
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
            size = R.GetFarsideDimension(cam.GetFOVX(), renderDistance);
            var center = ray.position + ray.direction * renderDistance;

            rays.Clear();
            foreach (var v in R.IterateOutwards((int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y), stepSize))
            {
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
        }
    }




    class Debug3D
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
        }
    }

    public class Entity
    {
        public Vector3 position;
        public Vector3 size;
    }



    static class Program
    {
        public unsafe static int Main()
        {

            System.Diagnostics.Debug.Assert(Config.chunkSize % 2 == 0, "chunk size must be even");

            var (sw, sh) = (Config.windowWidth, Config.windowHeight);
            rl.InitWindow(sw, sh, "Voxel editor");
            rl.SetWindowPosition(rl.GetMonitorWidth(0) - sw - 50, rl.GetMonitorHeight(0) - sh - 50);
            rl.SetTargetFPS(Config.fps);
            rl.DisableCursor();

            var cam = new Camera3D();
            var rayCaster = new RayCaster(cam);
            var tree = rl.LoadTexture("assets/tree_single.png");
            var shader = rl.LoadShader("", "assets/discard_alpha.fs");
            var atlas = new Atlas(1, rl.LoadTexture("assets/atlas.png"), 32);
            var world = new VoxelWorld(Config.minWorldBounds, Config.maxWorldBounds);
            var player = new Player();
            var state = new GameState();
            var atlasMan = AtlasManager.Init();

            world.atlasManager = atlasMan;
            state.player = player;
            state.world = world;
            state.cam = cam;
            state.atlasManager = atlasMan;
            player.state = state;

            var console = new GameConsole(state);
            var imctlr = new ImguiController();


            imctlr.Load(sw, sh);

            cam.Setup(Config.fov, new Vector3(0, Config.cameraHeight, 0));
            cam.MoveSpeed = new Vector3(Config.runSpeed);

            world.cam = cam;
            world.player = player;
            player.Equip(player.equippedCube);

            Console.WriteLine("world data size: {0}", world.data.Length);

            var r = new Random();

            var rayCastedChunks = new Vector3[0];
            byte savedDrawFlag = 0;

            console.active = false;
            imctlr.active = false;
            cam.active = true;

            var toggleConsole = () =>
            {
                console.active = !console.active;
                imctlr.active = console.active;
                cam.active = !console.active;

                if (!console.active)
                {
                    rl.DisableCursor();
                }
            };

            var handleKeys = () =>
            {
                if (rl.IsKeyPressed(KeyboardKey.KEY_F9))
                {
                    toggleConsole();
                }


                if (console.active)
                {
                    return;
                }


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
                    player.TriggerPrimaryAction();
                    rayCastedChunks = world.GetRayCastedChunks(world.cam.ray);
                    savedDrawFlag = world.drawFlag;
                }

                if (rl.IsKeyPressed(KeyboardKey.KEY_SPACE))
                {
                    cam.JumpHeight += 0.01f;
                }
            };


            while (!rl.WindowShouldClose())
            {
                if (console.active)
                {
                    imctlr.active = true;
                }
                else
                {
                }

                imctlr.Update(rl.GetFrameTime());

                var camRay = cam.ray;

                handleKeys();

                player.Update();
                world.Update();
                console.Update();
                cam.Update();

                // TODO:
                // cam.Jumper.Height
                // cam.Jumper.Update()
                // cam.Jumper.Perform()


                rl.BeginDrawing();
                rl.ClearBackground(Color.WHITE);

                cam.BeginMode3D();

                world.Draw(cam);
                rayCaster.Draw();


                DrawRayCastedChunks(world, rayCastedChunks, savedDrawFlag);


                rl.BeginShaderMode(shader);

                var srcRec = new Rectangle(0, 0, tree.width, tree.height);
                var up = new Vector3(0, 1, 0);
                var size = new Vector2(1, 1);

                if (player.equippedCube != "" && player.equippedSubTexture.Length > 0)
                {
                    Voxel.DrawCubeWires(player.frontCube, Color.BEIGE);
                }

                Debug3D.DrawOrigin();

                rl.EndShaderMode();

                console.Draw();

                cam.EndMode3D();
                rl.DrawFPS(rl.GetScreenWidth() - 100, 10);

                var crossHairSize = 5;
                rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, crossHairSize + 1, Color.WHITE);
                rl.DrawCircle(rl.GetScreenWidth() / 2, rl.GetScreenHeight() / 2, crossHairSize, Color.RED);

                world.DrawInfo(world);


                imctlr.Draw();
                rl.EndDrawing();
            }

            Raylib.CloseWindow();
            return 0;
        }

        public static void DrawRayCastedChunks(VoxelWorld world, Vector3[] chunks, byte savedDrawFlag)
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

        public static void FillChunk(VoxelWorld world, string tileName, Vector3 chunkPos)
        {
            foreach (var p in world.IterateChunk(world.chunkSize, chunkPos))
            {
                world.InsertCube(p, tileName);
            }
        }
        public static void InsertCorners(VoxelWorld world, string tileName, Vector3 chunkPos)
        {
            var chunkSize = world.chunkSize;
            var root = chunkSize * chunkPos;
            var end = root + Vector3.One * chunkSize;

            for (var n = 0; n < chunkSize; n++)
            {
                world.InsertCube(root + new Vector3(n, 0, 0), tileName);
                world.InsertCube(root + new Vector3(0, 0, n), tileName);
                world.InsertCube(root + new Vector3(n, chunkSize, 0), tileName);
                world.InsertCube(root + new Vector3(0, chunkSize, n), tileName);
                world.InsertCube(end - new Vector3(n, 0, 0), tileName);
                world.InsertCube(end - new Vector3(0, 0, n), tileName);
                world.InsertCube(end - new Vector3(n, chunkSize, 0), tileName);
                world.InsertCube(end - new Vector3(0, chunkSize, n), tileName);

                world.InsertCube(root + new Vector3(0, n, 0), tileName);
                world.InsertCube(root + new Vector3(chunkSize, n, 0), tileName);
                world.InsertCube(root + new Vector3(chunkSize, n, chunkSize), tileName);
                world.InsertCube(root + new Vector3(0, n, chunkSize), tileName);
                world.InsertCube(root + new Vector3(n, n, n), tileName);
            }
        }
    }

}