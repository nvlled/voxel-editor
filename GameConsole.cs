


using System.Text.Json;
using System.Numerics;
using Raylib_cs;
using rl = Raylib_cs.Raylib;
using gl = Raylib_cs.Rlgl;
using rm = Raylib_cs.Raymath;

using Camera3D = VoxelEditor.FPCamera;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using NLua;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace VoxelEditor
{
    public interface BgCommand
    {
        public string ID();
        public void Start();
        public void Stop();
        public void Update();
        public void Draw() { }
    }


    class FloorMaker : BgCommand
    {
        public int radius = 3;
        public Vector3 pos;
        public string ID()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
    class ChunkScanner : BgCommand
    {
        public const string id = "chunk-scanner";
        public string ID() { return id; }

        Ray currentRay;
        Vector3 currentChunk;

        bool running = false;

        GameState state;
        GameConsole console;

        public ChunkScanner(GameConsole console, GameState state)
        {
            this.state = state;
            this.console = console;
        }


        public void Start()
        {
            if (running)
            {
                return;
            }
            running = true;
            state.player.OnPrimaryAction += OnPrimaryAction;
            state.player.OnSecondaryAction += OnSecondaryAction;

            state.player.OnPrimaryAction -= state.player.InsertCube;
        }

        public void OnPrimaryAction(Vector3 frontCube, Ray ray)
        {
            currentRay = ray;
            var chunkSize = state.world.chunkSize;
            var chunkPos = Voxel.GetChunkPosition(chunkSize, frontCube);

            currentChunk = chunkPos;

            console.OuputLine(string.Format("found chunk: {0}", currentChunk));
        }
        public void OnSecondaryAction(Vector3 frontCube, Ray ray)
        {
            // TODO:
        }

        public void Stop()
        {
            running = false;
            state.player.OnPrimaryAction -= OnPrimaryAction;
        }

        public void Update()
        {
            if (!running) { return; }

        }

        public void Draw()
        {
            if (!running) { return; }

            var chunkSize = state.world.chunkSize;
            var pos = currentChunk * chunkSize;
            var midPos = pos + Vector3.One * chunkSize / 2f;
            var endPos = pos + Vector3.One * chunkSize;
            var n = chunkSize;
            rl.DrawCubeWires(midPos + -R.Vector3Half, n, n, n, Color.YELLOW);
            rl.DrawCube(midPos, 0.5f, 0.5f, 0.5f, Color.YELLOW);
        }
    }


    class RandomWalker : BgCommand
    {
        public const string id = "random-walker";
        VoxelWorld world;
        public bool running = false;

        public float frequency = 2;
        public float elapsed = 0;

        public Vector3 pos;
        public string tileName;

        public RandomWalker(VoxelWorld world)
        {
            this.world = world;
        }

        public string ID()
        {
            return id;
        }
        public void Start()
        {
            running = true;
        }
        public void Stop()
        {
            running = false;
        }
        public void Update()
        {
            if (!running)
            {
                return;
            }

            elapsed += rl.GetFrameTime();

            if (elapsed < frequency)
            {
                return;
            }

            elapsed = 0;

            world.InsertCube(pos, tileName);
            switch (rl.GetRandomValue(1, 6))
            {
                case 1: pos += new Vector3(0, 0, 1); break;
                case 2: pos += new Vector3(0, 1, 0); break;
                case 3: pos += new Vector3(1, 0, 0); break;
                case 4: pos += new Vector3(0, 0, -1); break;
                case 5: pos += new Vector3(0, -1, 0); break;
                case 6: pos += new Vector3(-1, 0, 0); break;
            }
        }
    }

    unsafe public class GameConsole
    {
        VoxelWorld world;
        Player player;
        GameState state;

        const int MAX_OUTPUT_LINES = 20;

        public bool active = false;
        public int margin = 50;
        public Color bgColor = new Color(20, 20, 20, 180);
        public Color bgInput = new Color(35, 35, 35, 180);
        public Color fgColor = new Color(0, 200, 0, 255);
        public int fontSize = 24;
        public int charWidth = 0;
        int consoleWidth;
        int consoleHeight;

        public List<BgCommand> commands = new List<BgCommand>();

        public Lua lua = new Lua();

        StringBuilder inputBuilder = new StringBuilder();
        List<string> outputLines = new List<string>();

        public float backspaceHeld = 0;

        public List<string> history = new List<string>();
        public int historyIndex = 0;

        public GameConsole(GameState state)
        {
            world = state.world;
            player = state.player;
            this.state = state;

            charWidth = (rl.MeasureText("m", fontSize) + rl.MeasureText("w", fontSize)) / 2;
            consoleWidth = rl.GetScreenWidth() - margin;
            consoleHeight = rl.GetScreenHeight() - margin;

            AddDefaultCommands();

            /*
			commands:
			- newWorld(10, 10, 10)
			- save(filename)
			- load(filename)
			- getSaveFiles()
			- clear
			- equipTile
			- equipCube
			  show current equipped cube
			- createCube()
			- fillChunk
			*/

            // TODO: remove warning
            // TODO: see "using global"
            lua["clear"] = (Action)cls;
            lua["sample"] = sample;
            lua["insert"] = insert;
            lua["line"] = line;
            lua["castline"] = castline;
            lua["o"] = Vector3.Zero;
            lua["pyramid"] = pyramid;
            lua["cube"] = cube;
            lua["floor"] = floor;
            lua["wall"] = wall;
            lua["equip"] = equip;
            lua["trail"] = trail;
            lua["erays"] = erays;
            lua["rwalk"] = rwalk;
            lua["rwalkStop"] = rwalkStop;
            lua["scanChunk"] = scanChunk;

            lua["save"] = saveWorld;
            lua["load"] = loadWorld;
        }

        public void AddDefaultCommands()
        {
            BgCommand cmd = new ChunkScanner(this, state);
            cmd.Start();
            commands.Add(cmd);
        }

        public void Backspace()
        {
            if (inputBuilder.Length > 0)
            {
                inputBuilder.Remove(inputBuilder.Length - 1, 1);
            }
        }

        public bool isTypable(char ch)
        {
            return char.IsLetterOrDigit(ch)
                || char.IsPunctuation(ch)
                || char.IsWhiteSpace(ch)
                || ch == '+'
                || ch == '=';
        }

        public int InputTextCallback(ImGuiInputTextCallbackData* data)
        {

            if (history.Count() == 0)
            {
                return 0;
            }


            if ((*data).EventKey == ImGuiKey.UpArrow)
            {
                historyIndex--;
            }
            else if ((*data).EventKey == ImGuiKey.DownArrow)
            {
                historyIndex++;

                if (historyIndex >= history.Count())
                {
                    (*data).BufSize = 0;
                    (*data).BufTextLen = 0;
                    (*data).BufDirty = 1;
                    return 0;
                }
            }

            historyIndex = Math.Clamp(historyIndex, 0, history.Count() - 1);

            var command = history[historyIndex];
            var buf = Encoding.Default.GetBytes(command);
            fixed (byte* bp = buf)
            {
                (*data).Buf = bp;
                (*data).BufSize = buf.Length;
                (*data).BufTextLen = command.Count();
                (*data).BufDirty = 1;
            }

            return 0;
        }

        public void Update()
        {
            foreach (var cmd in commands)
            {
                cmd.Update();
            }

            if (!active)
            {
                return;
            }

            var ray = world.cam.ray;
            lua["cam"] = player.frontCube;

            UpdateUI();
        }

        public void UpdateUI()
        {
            ImGui.SetWindowSize(new Vector2(800, 500));
            ImGui.Begin("Console");

            var reserveHeight = ImGui.GetStyle().ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing();
            ImGui.BeginChild("scroll-region", new Vector2(0, -reserveHeight), false, ImGuiWindowFlags.HorizontalScrollbar);

            foreach (var line in outputLines)
            {
                ImGui.TextUnformatted(line);
            }
            ImGui.SetScrollHereY(1);
            ImGui.EndChild();

            ImGui.Separator();

            var input_text_flags = ImGuiInputTextFlags.EnterReturnsTrue
                | ImGuiInputTextFlags.CallbackCompletion
                | ImGuiInputTextFlags.CallbackHistory;

            string cmd = "";

            var executed = false;
            if (ImGui.InputText("", ref cmd, 1024, input_text_flags, InputTextCallback)
                && cmd.Length > 0)
            {
                history.Add(cmd);
                historyIndex = history.Count();
                Console.WriteLine("input: " + cmd);
                RunCommand(cmd);
                executed = true;
            }


            ImGui.SetItemDefaultFocus();
            if (executed)
            {
                ImGui.SetKeyboardFocusHere(-1);
            }

            ImGui.End();
        }


        public void RunCommand(string cmd)
        {
            string output = "> " + cmd + "\n";
            try
            {
                if (cmd[0] == '=')
                {
                    cmd = "return " + cmd.Substring(1);
                }

                var word = cmd.Split(' ').Where(s => s.Length > 0).FirstOrDefault("");
                if (
                word != "local" && word != "function" && word != "return"
                )
                {
                    cmd = "return " + cmd;
                }

                var returned = lua.DoString(cmd);
                if (returned.Length == 0)
                {
                    return;
                }
                output += string.Join(" ", returned);
            }
            catch (NLua.Exceptions.LuaScriptException e)
            {
                output += e.Message;
            }


            outputLines.Add(output);
            if (outputLines.Count() >= MAX_OUTPUT_LINES)
            {
                outputLines.RemoveAt(0);
            }

        }

        public void OuputLine(string line)
        {
            outputLines.Add(line);
        }

        public void Draw()
        {
            foreach (var cmd in commands)
            {
                cmd.Draw();
            }
        }

        // ----------------------------------------------------------------------------------

        public void equip(string tileName)
        {
            player.Equip(tileName);
        }
        public void trail() { }
        public void cube() { }
        public void floor() { }
        public void wall() { }
        public void erays() { }
        public string rwalk(float frequency = 1, string tileName = "default")
        {

            var rwalk = new RandomWalker(world);
            commands.Add(rwalk);

            rwalk.pos = player.frontCube;
            rwalk.frequency = frequency;
            rwalk.tileName = tileName;
            rwalk.elapsed = 0;
            rwalk.Start();

            return "rwalk id: " + commands.Count();
        }
        public void rwalkStop(int id = -1)
        {
            if (id < 0)
            {
                foreach (var cmd in commands.FindAll(c => c.ID() == RandomWalker.id))
                {
                    cmd.Stop();
                }
            }
            else if (id >= 0 && id < commands.Count())
            {
                if (commands[id] is RandomWalker)
                {
                    commands[id].Stop();
                }
                commands.RemoveAt(id);
            }
        }

        public void scanChunk()
        {
            foreach (var cmd in commands.FindAll(c => c.ID() == ChunkScanner.id))
            {
                cmd.Stop();
                return;
            }

            var newCmd = new ChunkScanner(this, state);
            commands.Add(newCmd);
            newCmd.Start();
        }

        public void saveWorld(string filename)
        {
            //new JsonSerializer().Serialize()
        }

        public void loadWorld(string filename) { }

        public void pyramid(int height, Vector3? centerBaseArg = null, string tileName = "default")
        {
            var centerBase = centerBaseArg.GetValueOrDefault(player.frontCube);
            var up = world.cam.Camera.up;
            var front = new Vector3(1, 0, 0);
            var right = new Vector3(0, 0, 1);
            for (var i = 0; i < height; i++)
            {
                foreach (var v in R.IterateOutwards(height - i, height - i, 1))
                {
                    var p = centerBase + up * i + front * v.Y + right * v.X;
                    world.InsertCube(p, tileName);
                }
            }
        }

        public void line(Vector3 p1, Vector3 p2, string tileName = "default")
        {
            var v = p1 - p2;
            var ray = new Ray(p2, Vector3.Normalize(v));
            for (var i = 0; i < v.Length(); i++)
            {
                var cubePos = ray.position + ray.direction * i;
                world.InsertCube(cubePos, tileName);
            }
        }

        public void castline(float length, string tileName = "default")
        {
            var ray = world.cam.ray;
            for (var i = 0; i < length; i++)
            {
                var cubePos = player.frontCube + ray.direction * i;
                world.InsertCube(cubePos, tileName);
            }
        }

        public void cls()
        {
            outputLines.Clear();
        }

        public string sample()
        {
            return "sample output";
        }

        public void insert(string tileName)
        {
            var camRay = world.cam.ray;
            world.InsertCube(camRay.position + camRay.direction * 0.5f, tileName);
        }
    }
}