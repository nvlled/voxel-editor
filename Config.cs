
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
		public static int renderDistance = 50;
		public static int chunkSize = 20;
		public static float fov = 65;
		public static int fps = 60;
		public static int windowWidth = 800;
		public static int windowHeight = 640;
		public static float cameraHeight = 1.5f;
		public static float runSpeed = 5.0f;
		public static float walkSpeed = 1.5f;

		public static Vector3 minWorldBounds = new Vector3(-50, -30, -50);
		public static Vector3 maxWorldBounds = new Vector3(50, 30, 50);

		public static bool drawAll = false;
	}
}