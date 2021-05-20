using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	class GraphicsPipeline : IDisposable
	{
		private readonly Api _api;
		private readonly SwapChain _swapChain;
		private readonly DescriptorSetManager _descriptorSetManager;
		private readonly PipelineLayout _pipelineLayout;
		private readonly RenderPass _renderPass;
		private readonly Pipeline _vkPipeline;
		private bool _disposedValue;

		public unsafe GraphicsPipeline(
			Api api,
			SwapChain swapChain,
			DepthBuffer depthBuffer,
			List<UniformBuffer> uniformBuffers,
			VulkanScene scene)
		{
			(_api, _swapChain) = (api, swapChain);

			VertexDesc.GetBindingDescription(out var bindingDescription);
			var attributeDescriptions = VertexDesc.GetAttributeDescriptions();
			Span<VertexInputAttributeDescription> attributeDescriptionsPinned = stackalloc VertexInputAttributeDescription[attributeDescriptions.Length];
			attributeDescriptions.CopyTo(attributeDescriptionsPinned);
			var vertexInputInfo = new PipelineVertexInputStateCreateInfo();
			vertexInputInfo.SType = StructureType.PipelineVertexInputStateCreateInfo;
			vertexInputInfo.VertexBindingDescriptionCount = 1;
			vertexInputInfo.PVertexBindingDescriptions = (VertexInputBindingDescription*)Unsafe.AsPointer(ref bindingDescription);
			vertexInputInfo.VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length;
			vertexInputInfo.PVertexAttributeDescriptions = (VertexInputAttributeDescription*)Unsafe.AsPointer(ref attributeDescriptionsPinned[0]);

			var inputAssembly = new PipelineInputAssemblyStateCreateInfo();
			inputAssembly.SType = StructureType.PipelineInputAssemblyStateCreateInfo;
			inputAssembly.Topology = PrimitiveTopology.TriangleList;
			inputAssembly.PrimitiveRestartEnable = new Silk.NET.Core.Bool32(false);

			var viewPort = new Viewport();
			viewPort.X = 0.0f;
			viewPort.Y = 0.0f;
			viewPort.Width = swapChain.Extent.Width;
			viewPort.Height = swapChain.Extent.Height;
			viewPort.MinDepth = 0.0f;
			viewPort.MaxDepth = 1.0f;

			var scissor = new Rect2D();
			scissor.Offset = new Offset2D(0, 0);
			scissor.Extent = swapChain.Extent;

			var viewportState = new PipelineViewportStateCreateInfo();
			viewportState.SType = StructureType.PipelineViewportStateCreateInfo;
			viewportState.ViewportCount = 1;
			viewportState.PViewports = (Viewport*)Unsafe.AsPointer(ref viewPort);
			viewportState.ScissorCount = 1;
			viewportState.PScissors = (Rect2D*)Unsafe.AsPointer(ref scissor);

			var rasterizer = new PipelineRasterizationStateCreateInfo();
			rasterizer.SType = StructureType.PipelineRasterizationStateCreateInfo;
			rasterizer.DepthClampEnable = new Silk.NET.Core.Bool32(false);
			rasterizer.RasterizerDiscardEnable = new Silk.NET.Core.Bool32(false);
			rasterizer.PolygonMode = PolygonMode.Fill;
			rasterizer.LineWidth = 1.0f;
			rasterizer.CullMode = CullModeFlags.CullModeBackBit;
			rasterizer.FrontFace = FrontFace.CounterClockwise;
			rasterizer.DepthBiasEnable = new Silk.NET.Core.Bool32(false);
			rasterizer.DepthBiasConstantFactor = 0.0f;
			rasterizer.DepthBiasClamp = 0.0f;
			rasterizer.DepthBiasSlopeFactor = 0.0f;

			var multisampling = new PipelineMultisampleStateCreateInfo();
			multisampling.SType = StructureType.PipelineMultisampleStateCreateInfo;
			multisampling.SampleShadingEnable = new Silk.NET.Core.Bool32(false);
			multisampling.RasterizationSamples = SampleCountFlags.SampleCount1Bit;
			multisampling.MinSampleShading = 1.0f;
			multisampling.PSampleMask = null;
			multisampling.AlphaToCoverageEnable = new Silk.NET.Core.Bool32(false);
			multisampling.AlphaToOneEnable = new Silk.NET.Core.Bool32(false);

			var depthStencil = new PipelineDepthStencilStateCreateInfo();
			depthStencil.SType = StructureType.PipelineDepthStencilStateCreateInfo;
			depthStencil.DepthTestEnable = new Silk.NET.Core.Bool32(true);
			depthStencil.DepthWriteEnable = new Silk.NET.Core.Bool32(true);
			depthStencil.DepthCompareOp = CompareOp.Less;
			depthStencil.DepthBoundsTestEnable = new Silk.NET.Core.Bool32(false);
			depthStencil.MinDepthBounds = 0.0f;
			depthStencil.MaxDepthBounds = 1.0f;
			depthStencil.StencilTestEnable = new Silk.NET.Core.Bool32(false);
			depthStencil.Front = new StencilOpState();
			depthStencil.Back = new StencilOpState();

			var colorBlendAttachment = new PipelineColorBlendAttachmentState();
			colorBlendAttachment.ColorWriteMask = ColorComponentFlags.ColorComponentRBit | ColorComponentFlags.ColorComponentGBit | ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit;
			colorBlendAttachment.BlendEnable = new Silk.NET.Core.Bool32(false);
			colorBlendAttachment.SrcColorBlendFactor = BlendFactor.One;
			colorBlendAttachment.DstColorBlendFactor = BlendFactor.Zero;
			colorBlendAttachment.ColorBlendOp = BlendOp.Add;
			colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
			colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.Zero;
			colorBlendAttachment.AlphaBlendOp = BlendOp.Add;

			var colorBlending = new PipelineColorBlendStateCreateInfo();
			colorBlending.SType = StructureType.PipelineColorBlendStateCreateInfo;
			colorBlending.LogicOpEnable = new Silk.NET.Core.Bool32(false);
			colorBlending.LogicOp = LogicOp.Copy;
			colorBlending.AttachmentCount = 1;
			colorBlending.PAttachments = (PipelineColorBlendAttachmentState*)Unsafe.AsPointer(ref colorBlendAttachment);
			colorBlending.BlendConstants[0] = 0.0f;
			colorBlending.BlendConstants[1] = 0.0f;
			colorBlending.BlendConstants[2] = 0.0f;
			colorBlending.BlendConstants[3] = 0.0f;

			// Create descriptor pool/sets.
			var descriptorBindings = new DescriptorBinding[]
			{
				new DescriptorBinding(0, 1, DescriptorType.UniformBuffer, ShaderStageFlags.ShaderStageVertexBit),
				new DescriptorBinding(1, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageVertexBit | ShaderStageFlags.ShaderStageFragmentBit),
				new DescriptorBinding(2, (uint)scene.TextureSamplers.Count, DescriptorType.CombinedImageSampler, ShaderStageFlags.ShaderStageFragmentBit)
			};

			_descriptorSetManager = new DescriptorSetManager(_api, descriptorBindings, (ulong)uniformBuffers.Count);

			var descriptorSets = _descriptorSetManager.DescriptorSets;

			for (int i = 0; i != swapChain.VkImages.Count; i++)
			{
				// Uniform buffer
				var uniformBufferInfo = new DescriptorBufferInfo();
				uniformBufferInfo.Buffer = uniformBuffers[i].Buffer.VkBuffer;
				uniformBufferInfo.Range = Vk.WholeSize;

				// Material buffer
				var materialBufferInfo = new DescriptorBufferInfo();
				materialBufferInfo.Buffer = scene.MaterialBuffer.VkBuffer;
				materialBufferInfo.Range = Vk.WholeSize;

				// Image and texture samplers
				DescriptorImageInfo[] imageInfos = new DescriptorImageInfo[scene.TextureSamplers.Count];

				for (int t = 0; t != imageInfos.Length; ++t)
				{
					var imageInfo = imageInfos[t];
					imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
					imageInfo.ImageView = scene.TextureImageViews[t];
					imageInfo.Sampler = scene.TextureSamplers[t];
					imageInfos[t] = imageInfo;
				}

#pragma warning disable CA2014 // Do not use stackalloc in loops
				Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[3];
#pragma warning restore CA2014 // Do not use stackalloc in loops

				fixed (DescriptorImageInfo* imageInfosPtr = &imageInfos[0])
				{
					descriptorSets.Bind(i, 0, (DescriptorBufferInfo*)Unsafe.AsPointer(ref uniformBufferInfo), out descriptorWrites[0]);
					descriptorSets.Bind(i, 1, (DescriptorBufferInfo*)Unsafe.AsPointer(ref materialBufferInfo), out descriptorWrites[1]);
					descriptorSets.Bind(i, 2, imageInfosPtr, out descriptorWrites[2], (uint)imageInfos.Length);

					descriptorSets.UpdateDescriptors(descriptorWrites);
				}
			}

			// Create pipeline layout and render pass.
			_pipelineLayout = new PipelineLayout(_api, _descriptorSetManager.DescriptorSetLayout);
			_renderPass = new RenderPass(_api, swapChain, depthBuffer, AttachmentLoadOp.Clear, AttachmentLoadOp.Clear);

			// Load shaders.
			var vertShader = new ShaderModule(_api, "./assets/shaders/Graphics.vert.spv");
			var fragShader = new ShaderModule(_api, "./assets/shaders/Graphics.frag.spv");

			Span<PipelineShaderStageCreateInfo> shaderStages = stackalloc PipelineShaderStageCreateInfo[]
			{
				vertShader.CreateShaderStage(ShaderStageFlags.ShaderStageVertexBit),
				fragShader.CreateShaderStage(ShaderStageFlags.ShaderStageFragmentBit)
			};

			// Create graphics pipeline
			var pipelineInfo = new GraphicsPipelineCreateInfo();
			pipelineInfo.SType = StructureType.GraphicsPipelineCreateInfo;
			pipelineInfo.StageCount = 2;
			pipelineInfo.PStages = (PipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref shaderStages[0]);
			pipelineInfo.PVertexInputState = (PipelineVertexInputStateCreateInfo*)Unsafe.AsPointer(ref vertexInputInfo);
			pipelineInfo.PInputAssemblyState = (PipelineInputAssemblyStateCreateInfo*)Unsafe.AsPointer(ref inputAssembly);
			pipelineInfo.PViewportState = (PipelineViewportStateCreateInfo*)Unsafe.AsPointer(ref viewportState);
			pipelineInfo.PRasterizationState = (PipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref rasterizer);
			pipelineInfo.PMultisampleState = (PipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref multisampling);
			pipelineInfo.PDepthStencilState = (PipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref depthStencil);
			pipelineInfo.PColorBlendState = (PipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref colorBlending);
			pipelineInfo.PDynamicState = null;
			pipelineInfo.BasePipelineHandle = default;
			pipelineInfo.BasePipelineIndex = -1;
			pipelineInfo.Layout = _pipelineLayout.VkPipelineLayout;
			pipelineInfo.RenderPass = _renderPass.VkRenderPass;
			pipelineInfo.Subpass = 0;

			Util.Verify(_api.Vk.CreateGraphicsPipelines(_api.Device.VkDevice, default, 1, pipelineInfo, default, out _vkPipeline), $"{nameof(Pipeline)}: Failed to create graphics pipeline");

			vertShader.Dispose();
			fragShader.Dispose();
		}

		public RenderPass RenderPass => _renderPass;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_renderPass.Dispose();
					_pipelineLayout.Dispose();
					_descriptorSetManager.Dispose();
				}

				_api.Vk.DestroyPipeline(_api.Device.VkDevice, _vkPipeline, default);
				_disposedValue = true;
			}
		}

		~GraphicsPipeline()
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
