using CommandLine;
using CommandLine.Text;
using RayTracingInDotNet.Scene;
using System;

namespace RayTracingInDotNet
{
	class Options
	{
		public static Options Parse(string[] args)
		{
			var result = Parser.Default
				.ParseArguments<Options>(args);

			if (result.Tag == ParserResultType.NotParsed)
				throw new Exception(HelpText.AutoBuild(result, _ => _, _ => _));

			Options ops = null;
			result.WithParsed(options => ops = options);

			if (ops.SceneIndex >= Scenes.MetaData.Count)
				throw new Exception("scene index is too large");

			return ops;
		}

		// Application options.
		[Option("debug-logging",
#if DEBUG
			Default = true,
#else
			Default = false,
#endif
			Required = false, 
			HelpText = "Enable debug logging to console.")]
		public bool DebugLogging { get; set; }

		// Scene options.
		[Option("scene", Default = (uint)7, Required = false, HelpText = "The scene to start with.")]
		public uint SceneIndex { get; set; }

		// Renderer options.
		[Option("samples", Default = (uint)8, Required = false, HelpText = "The number of ray samples per pixel.")]
		public uint Samples { get; set; }
		[Option("bounces", Default = (uint)16, Required = false, HelpText = "The maximum number of bounces per ray.")]
		public uint Bounces { get; set; }
		[Option("max-samples", Default = (uint)64 * 1024, Required = false, HelpText = "The maximum number of accumulated ray samples per pixel.")]
		public uint MaxSamples { get; set; }

		// Window options
		[Option("width", Default = (uint)1280, Required = false, HelpText = "The framebuffer width.")]
		public uint Width { get; set; }
		[Option("height", Default = (uint)720, Required = false, HelpText = "The framebuffer height.")]
		public uint Height { get; set; }
		[Option("vsync", Default = true, Required = false, HelpText = "Sync to the monitor refresh rate.")]
		public bool VSync { get; set; }
		[Option("fullscreen", Default = false, Required = false, HelpText = "Toggle fullscreen vs windowed (default: windowed).")]
		public bool Fullscreen { get; set; }
	}
}
