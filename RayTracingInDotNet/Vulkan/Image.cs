using Silk.NET.Vulkan;
using System;
using VkImage = Silk.NET.Vulkan.Image;
using VkImageMemoryBarrier = Silk.NET.Vulkan.ImageMemoryBarrier;

namespace RayTracingInDotNet.Vulkan
{
	class Image : IDisposable
	{
		private readonly Api _api;
		private readonly Extent2D _extent;
		private readonly Format _format;
		private readonly VkImage _vkImage;
		private ImageLayout _imageLayout = ImageLayout.Undefined;
		private bool _disposedValue;

		public Image(Api api, in Extent2D extent, Format format) :
			this(api, extent, format, ImageTiling.Optimal, ImageUsageFlags.ImageUsageTransferDstBit | ImageUsageFlags.ImageUsageSampledBit)
		{
		}

		public unsafe Image(Api api, in Extent2D extent, Format format, ImageTiling tiling, ImageUsageFlags usage)
		{
			(_api, _extent, _format) = (api, extent, format);

			var imageInfo = new ImageCreateInfo();
			imageInfo.SType = StructureType.ImageCreateInfo;
			imageInfo.ImageType = ImageType.ImageType2D;
			imageInfo.Extent.Width = extent.Width;
			imageInfo.Extent.Height = extent.Height;
			imageInfo.Extent.Depth = 1;
			imageInfo.MipLevels = 1;
			imageInfo.ArrayLayers = 1;
			imageInfo.Format = format;
			imageInfo.Tiling = tiling;
			imageInfo.InitialLayout = _imageLayout;
			imageInfo.Usage = usage;
			imageInfo.SharingMode = SharingMode.Exclusive;
			imageInfo.Samples = SampleCountFlags.SampleCount1Bit;
			imageInfo.Flags = 0;

			Util.Verify(_api.Vk.CreateImage(_api.Device.VkDevice, imageInfo, default, out _vkImage), $"{nameof(Image)}: Unable to create an image");
		}

		public VkImage VkImage => _vkImage;
		public Extent2D Extent => _extent;
		public Format Format => _format;

		public DeviceMemory AllocateMemory(MemoryPropertyFlags properties)
		{
			_api.Vk.GetImageMemoryRequirements(_api.Device.VkDevice, _vkImage, out var requirements);

			var memory = new DeviceMemory(_api, requirements.Size, requirements.MemoryTypeBits, 0, properties);

			Util.Verify(_api.Vk.BindImageMemory(_api.Device.VkDevice, _vkImage, memory.VkDeviceMemory, 0), $"{nameof(Image)}: Unable to bind image memory");

			return memory;
		}

		public void CopyFrom(CommandPool commandPool, Buffer buffer)
		{
			Util.Submit(_api, commandPool, commandBuffer =>
			{
				var region = new BufferImageCopy();
				region.BufferOffset = 0;
				region.BufferRowLength = 0;
				region.BufferImageHeight = 0;
				region.ImageSubresource.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				region.ImageSubresource.MipLevel = 0;
				region.ImageSubresource.BaseArrayLayer = 0;
				region.ImageSubresource.LayerCount = 1;
				region.ImageOffset = new Offset3D(0, 0, 0);
				region.ImageExtent = new Extent3D(_extent.Width, _extent.Height, 1);

				_api.Vk.CmdCopyBufferToImage(commandBuffer, buffer.VkBuffer, _vkImage, ImageLayout.TransferDstOptimal, 1, region);
			});
		}

		public unsafe void TransitionImageLayout(CommandPool commandPool, ImageLayout newLayout)
		{
			const uint VK_QUEUE_FAMILY_IGNORED = (~0U);

			Util.Submit(_api, commandPool, commandBuffer =>
			{
				var barrier = new VkImageMemoryBarrier();
				barrier.SType = StructureType.ImageMemoryBarrier;
				barrier.OldLayout = _imageLayout;
				barrier.NewLayout = newLayout;
				barrier.SrcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
				barrier.DstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
				barrier.Image = _vkImage;
				barrier.SubresourceRange.BaseMipLevel = 0;
				barrier.SubresourceRange.LevelCount = 1;
				barrier.SubresourceRange.BaseArrayLayer = 0;
				barrier.SubresourceRange.LayerCount = 1;

				if(newLayout == ImageLayout.DepthStencilAttachmentOptimal)
				{
					barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectDepthBit;

					if (DepthBuffer.HasStencilComponent(_format))
						barrier.SubresourceRange.AspectMask |= ImageAspectFlags.ImageAspectStencilBit;
				}
				else
				{
					barrier.SubresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
				}

				PipelineStageFlags sourceStage;
				PipelineStageFlags destinationStage;

				if (_imageLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
				{
					barrier.SrcAccessMask = 0;
					barrier.DstAccessMask = AccessFlags.AccessTransferWriteBit;

					sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
					destinationStage = PipelineStageFlags.PipelineStageTransferBit;
				}
				else if (_imageLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
				{
					barrier.SrcAccessMask = AccessFlags.AccessTransferWriteBit;
					barrier.DstAccessMask = AccessFlags.AccessShaderReadBit;

					sourceStage = PipelineStageFlags.PipelineStageTransferBit;
					destinationStage = PipelineStageFlags.PipelineStageFragmentShaderBit;
				}
				else if (_imageLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
				{
					barrier.SrcAccessMask = 0;
					barrier.DstAccessMask = AccessFlags.AccessDepthStencilAttachmentReadBit | AccessFlags.AccessDepthStencilAttachmentWriteBit;

					sourceStage = PipelineStageFlags.PipelineStageTopOfPipeBit;
					destinationStage = PipelineStageFlags.PipelineStageEarlyFragmentTestsBit;
				}
				else
				{
					throw new Exception($"{nameof(Image)}: Unsupported layout transition");
				}

				_api.Vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, default, 0, default, 1, barrier);
			});

			_imageLayout = newLayout;
		}


		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyImage(_api.Device.VkDevice, _vkImage, default);
				_disposedValue = true;
			}
		}

		~Image()
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
