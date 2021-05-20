using System;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace RayTracingInDotNet.Vulkan
{
	class TextureImage : IDisposable
	{
		private readonly Image _image;
		private readonly DeviceMemory _imageMemory;
		private readonly ImageView _imageView;
		private readonly Sampler _sampler;
		private bool _disposedValue;

		public unsafe TextureImage(Api api, CommandPool commandPool, Texture texture)
		{
			// Create a host staging buffer and copy the image into it.
			ulong imageSize = (ulong)texture.Width * (ulong)texture.Height * 4;

			var stagingBuffer = new Buffer(api, imageSize, BufferUsageFlags.BufferUsageTransferSrcBit);
			var stagingBufferMemory = stagingBuffer.AllocateMemory(MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit);

			var data = stagingBufferMemory.Map(0, imageSize);
			fixed (byte* ptr = &texture.Pixels[0])
			{
				Unsafe.CopyBlock(data, ptr, (uint)imageSize);
			}
			stagingBufferMemory.Unmap();

			// Create the device side image, memory, view and sampler.
			_image = new Image(api, new Extent2D((uint)texture.Width, (uint)texture.Height), Format.R8G8B8A8Unorm);
			_imageMemory = _image.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_imageView = new ImageView(api, _image.VkImage, _image.Format, ImageAspectFlags.ImageAspectColorBit);
			_sampler = new Sampler(api, new SamplerConfig());

			// Transfer the data to device side.
			_image.TransitionImageLayout(commandPool, ImageLayout.TransferDstOptimal);
			_image.CopyFrom(commandPool, stagingBuffer);
			_image.TransitionImageLayout(commandPool, ImageLayout.ShaderReadOnlyOptimal);

			// Delete the buffer before the memory
			stagingBuffer.Dispose();
			stagingBufferMemory.Dispose();
		}

		public ImageView ImageView => _imageView;
		public Sampler Sampler => _sampler;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_sampler.Dispose();
					_imageView.Dispose();
					_image.Dispose();
					_imageMemory.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
