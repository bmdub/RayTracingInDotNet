using Silk.NET.Vulkan;
using System;
using VkImage = Silk.NET.Vulkan.Image;
using VkImageView = Silk.NET.Vulkan.ImageView;

namespace RayTracingInDotNet.Vulkan
{
	class ImageView : IDisposable
	{
		private readonly Api _api;
		private readonly VkImage _image;
		private readonly Format _format;
		private readonly VkImageView _vkImageView;
		private bool _disposedValue;

		public unsafe ImageView(Api api, in VkImage image, Format format, ImageAspectFlags aspectFlags)
		{
			(_api, _image, _format) = (api, image, format);

			var createInfo = new ImageViewCreateInfo();
			createInfo.SType = StructureType.ImageViewCreateInfo;
			createInfo.Image = image;
			createInfo.ViewType = ImageViewType.ImageViewType2D;
			createInfo.Format = format;
			createInfo.Components.R = ComponentSwizzle.Identity;
			createInfo.Components.G = ComponentSwizzle.Identity;
			createInfo.Components.B = ComponentSwizzle.Identity;
			createInfo.Components.A = ComponentSwizzle.Identity;
			createInfo.SubresourceRange.AspectMask = aspectFlags;
			createInfo.SubresourceRange.BaseMipLevel = 0;
			createInfo.SubresourceRange.LevelCount = 1;
			createInfo.SubresourceRange.BaseArrayLayer = 0;
			createInfo.SubresourceRange.LayerCount = 1;

			Util.Verify(_api.Vk.CreateImageView(_api.Device.VkDevice, createInfo, default, out _vkImageView), $"{nameof(ImageView)}: Unable to create image view");
		}

		public VkImageView VkImageView => _vkImageView;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyImageView(_api.Device.VkDevice, _vkImageView, default);
				_disposedValue = true;
			}
		}

		~ImageView()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
