using RayTracingInDotNet.Scene;
using System;
using System.Numerics;

namespace RayTracingInDotNet
{
	delegate void DrawGuiDelegate();

	interface IRenderer : IDisposable
	{
		public void LoadScene(IScene scene, CameraInitialState cameraInitialState);
		public unsafe void DrawFrame(double delta, in Matrix4x4 modelView, CameraInitialState cameraInitialState);
		public event DrawGuiDelegate DrawGui;
		public void ResetAccumulation();
		public uint NumberOfSamples { get; }
		public uint TotalNumberOfSamples { get; }
	}
}
