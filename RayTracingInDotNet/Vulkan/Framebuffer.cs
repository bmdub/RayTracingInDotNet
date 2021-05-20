using Silk.NET.Vulkan;
using System;
using VkFrameBuffer = Silk.NET.Vulkan.Framebuffer;

namespace RayTracingInDotNet.Vulkan
{
	class Framebuffer : IDisposable
	{
		private readonly Api _api;
		private readonly ImageView _imageView;
		private readonly RenderPass _renderPass;
		private readonly VkFrameBuffer _vkFramebuffer;
		private bool _disposedValue;

		public unsafe Framebuffer(Api api, SwapChain swapChain, DepthBuffer depthBuffer, ImageView imageView, RenderPass renderPass)
		{
			(_api, _imageView, _renderPass) = (api, imageView, renderPass);

			var attachments = new Silk.NET.Vulkan.ImageView[]
			{
				imageView.VkImageView,
				depthBuffer.ImageView.VkImageView
			};

			fixed (Silk.NET.Vulkan.ImageView* attachmentsPtr = &attachments[0])
			{
				var framebufferInfo = new FramebufferCreateInfo();
				framebufferInfo.SType = StructureType.FramebufferCreateInfo;
				framebufferInfo.RenderPass = renderPass.VkRenderPass;
				framebufferInfo.AttachmentCount = (uint)attachments.Length;
				framebufferInfo.PAttachments = attachmentsPtr;
				framebufferInfo.Width = swapChain.Extent.Width;
				framebufferInfo.Height = swapChain.Extent.Height;
				framebufferInfo.Layers = 1;

				Util.Verify(_api.Vk.CreateFramebuffer(
					_api.Vk.CurrentDevice.Value, framebufferInfo, default, out _vkFramebuffer), $"{nameof(Framebuffer)}: Failed to create framebuffer");
			}
		}

		public VkFrameBuffer VkFrameBuffer => _vkFramebuffer;

		protected unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyFramebuffer(
					_api.Vk.CurrentDevice.Value, _vkFramebuffer, default);
				_disposedValue = true;
			}
		}

		~Framebuffer()
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
