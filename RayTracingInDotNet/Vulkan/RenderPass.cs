using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using VkRenderPass = Silk.NET.Vulkan.RenderPass;

namespace RayTracingInDotNet.Vulkan
{
	class RenderPass : IDisposable
	{
		private readonly Api _api;
		private readonly VkRenderPass _vkRenderPass;
		private bool _disposedValue;

		public unsafe RenderPass(Api api, SwapChain swapChain, DepthBuffer depthBuffer, AttachmentLoadOp colorBufferLoadOp, AttachmentLoadOp depthBufferLoadOp)
		{
			_api = api;

			var colorAttachment = new AttachmentDescription();
			colorAttachment.Format = swapChain.Format;
			colorAttachment.Samples = SampleCountFlags.SampleCount1Bit;
			colorAttachment.LoadOp = colorBufferLoadOp;
			colorAttachment.StoreOp = AttachmentStoreOp.Store;
			colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			colorAttachment.InitialLayout = colorBufferLoadOp == AttachmentLoadOp.Clear ? ImageLayout.Undefined : ImageLayout.PresentSrcKhr;
			colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;

			var depthAttachment = new AttachmentDescription();
			depthAttachment.Format = depthBuffer.Format;
			depthAttachment.Samples = SampleCountFlags.SampleCount1Bit;
			depthAttachment.LoadOp = depthBufferLoadOp;
			depthAttachment.StoreOp = AttachmentStoreOp.DontCare;
			depthAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
			depthAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
			depthAttachment.InitialLayout = depthBufferLoadOp == AttachmentLoadOp.Clear ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal;
			depthAttachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;

			var colorAttachmentRef = new AttachmentReference();
			colorAttachmentRef.Attachment = 0;
			colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;

			var depthAttachmentRef = new AttachmentReference();
			depthAttachmentRef.Attachment = 1;
			depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;

			var subpass = new SubpassDescription();
			subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
			subpass.ColorAttachmentCount = 1;
			subpass.PColorAttachments = (AttachmentReference*)Unsafe.AsPointer(ref colorAttachmentRef);
			subpass.PDepthStencilAttachment = (AttachmentReference*)Unsafe.AsPointer(ref depthAttachmentRef);

			var dependency = new SubpassDependency();
			dependency.SrcSubpass = Vk.SubpassExternal;
			dependency.DstSubpass = 0;
			dependency.SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
			dependency.SrcAccessMask = 0;
			dependency.DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;
			dependency.DstAccessMask = AccessFlags.AccessColorAttachmentReadBit | AccessFlags.AccessColorAttachmentWriteBit;

			Span<AttachmentDescription> attachments = stackalloc AttachmentDescription[]
			{
				colorAttachment,
				depthAttachment
			};

			var renderPassInfo = new RenderPassCreateInfo();
			renderPassInfo.SType = StructureType.RenderPassCreateInfo;
			renderPassInfo.AttachmentCount = (uint)attachments.Length;
			renderPassInfo.PAttachments = (AttachmentDescription*)Unsafe.AsPointer(ref attachments[0]);
			renderPassInfo.SubpassCount = 1;
			renderPassInfo.PSubpasses = (SubpassDescription*)Unsafe.AsPointer(ref subpass);
			renderPassInfo.DependencyCount = 1;
			renderPassInfo.PDependencies = (SubpassDependency*)Unsafe.AsPointer(ref dependency);

			Util.Verify(
				_api.Vk.CreateRenderPass(_api.Vk.CurrentDevice.Value, renderPassInfo, default, out _vkRenderPass),
				$"{nameof(RenderPass)}: Failed to create render pass");
		}

		public VkRenderPass VkRenderPass => _vkRenderPass;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyRenderPass(
					_api.Vk.CurrentDevice.Value, _vkRenderPass, default);
				_disposedValue = true;
			}
		}

		~RenderPass()
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
