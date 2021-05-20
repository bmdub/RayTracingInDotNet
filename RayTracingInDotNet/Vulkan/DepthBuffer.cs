using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace RayTracingInDotNet.Vulkan
{
	class DepthBuffer : IDisposable
	{
		private readonly Api _api;
		private readonly Format _format;
		private readonly Image _image;
		private readonly DeviceMemory _imageMemory;
		private readonly ImageView _imageView;
		private bool _disposedValue;

		public DepthBuffer(Api api, CommandPool commandPool, in Extent2D extent)
		{
			_api = api;

			_format = FindDepthFormat();

			_image = new Image(_api, extent, _format, ImageTiling.Optimal, ImageUsageFlags.ImageUsageDepthStencilAttachmentBit);

			_imageMemory = _image.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);

			_imageView = new ImageView(_api, _image.VkImage, _format, ImageAspectFlags.ImageAspectDepthBit);

			_image.TransitionImageLayout(commandPool, ImageLayout.DepthStencilAttachmentOptimal);

			api.SetDebugName(_image.VkImage.Handle, $"Depth Buffer Image", ObjectType.Image);
			api.SetDebugName(_imageMemory.VkDeviceMemory.Handle, $"Depth Buffer Image Memory", ObjectType.DeviceMemory);
			api.SetDebugName(_imageView.VkImageView.Handle, $"Depth Buffer ImageView", ObjectType.ImageView);
		}

		public Format Format => _format;
		public ImageView ImageView => _imageView;

		public static bool HasStencilComponent(Format format) =>
			format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;

		private Format FindSupportedFormat(List<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
		{
			foreach (var format in candidates)
			{
				_api.Vk.GetPhysicalDeviceFormatProperties(_api.Device.PhysicalDevice, format, out var props);

				if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
					return format;

				if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
					return format;
			}

			throw new Exception($"{nameof(DepthBuffer)}: Could not find supported format");
		}

		private Format FindDepthFormat()
		{
			return FindSupportedFormat(
				new List<Format>() { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint },
				ImageTiling.Optimal,
				FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_image.Dispose();
					_imageMemory.Dispose();
					_imageView.Dispose();
				}

				_disposedValue = true;
			}
		}

		//~DepthBuffer()
		//{
			//Dispose(disposing: false);
		//}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
