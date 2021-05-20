using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Runtime.InteropServices;

namespace RayTracingInDotNet
{
	record Texture(int Width, int Height, int Channels, byte[] Pixels)
	{
		public static Texture LoadTexture(string filename)
		{
			// Load the texture in normal host memory.
			int width, height, channels;

			var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filename);
			width = image.Width;
			height = image.Height;
			channels = 4;

			if (!image.TryGetSinglePixelSpan(out Span<Rgba32> pixelSpan))
				throw new Exception($"{nameof(Texture)}: Unable to get image pixel span.");

			var pixels = MemoryMarshal.AsBytes(pixelSpan).ToArray();

			return new Texture(width, height, channels, pixels);
		}

		public static Texture LoadTexture(ReadOnlySpan<byte> data)
		{
			// Load the texture in normal host memory.
			int width, height, channels;

			var image = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
			width = image.Width;
			height = image.Height;
			channels = 4;

			if (!image.TryGetSinglePixelSpan(out Span<Rgba32> pixelSpan))
				throw new Exception($"{nameof(Texture)}: Unable to get image pixel span.");

			var pixels = MemoryMarshal.AsBytes(pixelSpan).ToArray();

			return new Texture(width, height, channels, pixels);
		}

		public static Texture LoadTexture(byte[] pixels, int width, int height)
		{
			int channels = 4;

			return new Texture(width, height, channels, pixels);
		}
	}
}
