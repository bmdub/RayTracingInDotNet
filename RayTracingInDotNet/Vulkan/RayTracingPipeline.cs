using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	class RayTracingPipeline : IDisposable
	{
		private readonly Api _api;
		private readonly SwapChain _swapChain;
		private readonly DescriptorSetManager _descriptorSetManager;
		private readonly PipelineLayout _pipelineLayout;
		private readonly uint _rayGenIndex;
		private readonly uint _missIndex;
		private readonly uint _triangleHitGroupIndex;
		private readonly uint _proceduralHitGroupIndex;
		private readonly Pipeline _pipeline;
		private bool _disposedValue;

		public unsafe RayTracingPipeline(
			Api api,
			SwapChain swapChain,
			TopLevelAccelerationStructure accelerationStructure,
			ImageView accumulationImageView,
			ImageView outputImageView,
			List<UniformBuffer> uniformBuffers,
			VulkanScene scene)
		{
			(_api, _swapChain) = (api, swapChain);

			// Create descriptor pool/sets.
			var descriptorBindings = new DescriptorBinding[]
			{
				// Top level acceleration structure.
				new DescriptorBinding(0, 1, DescriptorType.AccelerationStructureKhr, ShaderStageFlags.ShaderStageRaygenBitKhr),

				// Image accumulation & output
				new DescriptorBinding(1, 1, DescriptorType.StorageImage, ShaderStageFlags.ShaderStageRaygenBitKhr),
				new DescriptorBinding(2, 1, DescriptorType.StorageImage, ShaderStageFlags.ShaderStageRaygenBitKhr),

				// Camera information & co
				new DescriptorBinding(3, 1, DescriptorType.UniformBuffer, ShaderStageFlags.ShaderStageRaygenBitKhr | ShaderStageFlags.ShaderStageMissBitKhr),

				// Vertex buffer, Index buffer, Material buffer, Offset buffer, Transform buffer
				new DescriptorBinding(4, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr),
				new DescriptorBinding(5, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr),
				new DescriptorBinding(6, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr),
				new DescriptorBinding(7, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr),
				new DescriptorBinding(8, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr | ShaderStageFlags.ShaderStageIntersectionBitKhr),

				// Textures and image samplers
				new DescriptorBinding(9, (uint)scene.TextureSamplers.Count, DescriptorType.CombinedImageSampler, ShaderStageFlags.ShaderStageClosestHitBitKhr),

				// The Procedural buffer.
				new DescriptorBinding(10, 1, DescriptorType.StorageBuffer, ShaderStageFlags.ShaderStageClosestHitBitKhr | ShaderStageFlags.ShaderStageIntersectionBitKhr),
			};

			_descriptorSetManager = new DescriptorSetManager(_api, descriptorBindings, (ulong)uniformBuffers.Count);

			var descriptorSets = _descriptorSetManager.DescriptorSets;

			for (int i = 0; i != swapChain.VkImages.Count; i++)
			{
				// Top level acceleration structure.
				var _structureInfo = new WriteDescriptorSetAccelerationStructureKHR();
				_structureInfo.SType = StructureType.WriteDescriptorSetAccelerationStructureKhr;
				_structureInfo.PNext = null;
				_structureInfo.AccelerationStructureCount = 1;
				var accelerationStructurePinned = accelerationStructure.VkAccelerationStructure;
				_structureInfo.PAccelerationStructures = (AccelerationStructureKHR*)Unsafe.AsPointer(ref accelerationStructurePinned);

				// Accumulation image
				var _accumulationImageInfo = new DescriptorImageInfo();
				_accumulationImageInfo.ImageView = accumulationImageView.VkImageView;
				_accumulationImageInfo.ImageLayout = ImageLayout.General;

				// Output image
				var _outputImageInfo = new DescriptorImageInfo();
				_outputImageInfo.ImageView = outputImageView.VkImageView;
				_outputImageInfo.ImageLayout = ImageLayout.General;

				// Uniform buffer
				var _uniformBufferInfo = new DescriptorBufferInfo();
				_uniformBufferInfo.Buffer = uniformBuffers[i].Buffer.VkBuffer;
				_uniformBufferInfo.Range = Vk.WholeSize;

				// Vertex buffer
				var _vertexBufferInfo = new DescriptorBufferInfo();
				_vertexBufferInfo.Buffer = scene.VertexBuffer.VkBuffer;
				_vertexBufferInfo.Range = Vk.WholeSize;

				// Index buffer
				var _indexBufferInfo = new DescriptorBufferInfo();
				_indexBufferInfo.Buffer = scene.IndexBuffer.VkBuffer;
				_indexBufferInfo.Range = Vk.WholeSize;

				// Material buffer
				var _materialBufferInfo = new DescriptorBufferInfo();
				_materialBufferInfo.Buffer = scene.MaterialBuffer.VkBuffer;
				_materialBufferInfo.Range = Vk.WholeSize;

				// Offsets buffer
				var _offsetsBufferInfo = new DescriptorBufferInfo();
				_offsetsBufferInfo.Buffer = scene.OffsetsBuffer.VkBuffer;
				_offsetsBufferInfo.Range = Vk.WholeSize;

				// Transforms buffer
				var _transformsBufferInfo = new DescriptorBufferInfo();
				_transformsBufferInfo.Buffer = scene.TransformsBuffer.VkBuffer;
				_transformsBufferInfo.Range = Vk.WholeSize;

				// Image and texture samplers.
				var imageInfos = GC.AllocateArray<DescriptorImageInfo>(scene.TextureSamplers.Count, true);

				for (int t = 0; t != imageInfos.Length; ++t)
				{
					var imageInfo = imageInfos[t];
					imageInfo.ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
					imageInfo.ImageView = scene.TextureImageViews[t];
					imageInfo.Sampler = scene.TextureSamplers[t];
					imageInfos[t] = imageInfo;
				}

				int descriptorWriteCt = scene.HasProcedurals ? 11 : 10;
#pragma warning disable CA2014 // Do not use stackalloc in loops
				Span<WriteDescriptorSet> descriptorWrites = stackalloc WriteDescriptorSet[descriptorWriteCt];
#pragma warning restore CA2014 // Do not use stackalloc in loops

				descriptorSets.Bind(i, 0, (WriteDescriptorSetAccelerationStructureKHR*)Unsafe.AsPointer(ref _structureInfo), out descriptorWrites[0]);
				descriptorSets.Bind(i, 1, (DescriptorImageInfo*)Unsafe.AsPointer(ref _accumulationImageInfo), out descriptorWrites[1]);
				descriptorSets.Bind(i, 2, (DescriptorImageInfo*)Unsafe.AsPointer(ref _outputImageInfo), out descriptorWrites[2]);
				descriptorSets.Bind(i, 3, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _uniformBufferInfo), out descriptorWrites[3]);
				descriptorSets.Bind(i, 4, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _vertexBufferInfo), out descriptorWrites[4]);
				descriptorSets.Bind(i, 5, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _indexBufferInfo), out descriptorWrites[5]);
				descriptorSets.Bind(i, 6, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _materialBufferInfo), out descriptorWrites[6]);
				descriptorSets.Bind(i, 7, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _offsetsBufferInfo), out descriptorWrites[7]);
				descriptorSets.Bind(i, 8, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _transformsBufferInfo), out descriptorWrites[8]);
				descriptorSets.Bind(i, 9, (DescriptorImageInfo*)Unsafe.AsPointer(ref imageInfos[0]), out descriptorWrites[9], (uint)imageInfos.Length);

				// Procedural buffer (optional)
				var _proceduralBufferInfo = new DescriptorBufferInfo();
				_proceduralBufferInfo.Buffer = scene.ProceduralBuffer.VkBuffer;
				_proceduralBufferInfo.Range = Vk.WholeSize;

				if (scene.HasProcedurals)
					descriptorSets.Bind(i, 10, (DescriptorBufferInfo*)Unsafe.AsPointer(ref _proceduralBufferInfo), out descriptorWrites[10]);

				descriptorSets.UpdateDescriptors(descriptorWrites);
			}

			_pipelineLayout = new PipelineLayout(_api, _descriptorSetManager.DescriptorSetLayout);

			// Load shaders.
			var rayGenShader = new ShaderModule(_api, "./assets/shaders/RayTracing.rgen.spv");
			var missShader = new ShaderModule(_api, "./assets/shaders/RayTracing.rmiss.spv");
			var closestHitShader = new ShaderModule(_api, "./assets/shaders/RayTracing.rchit.spv");
			var proceduralClosestHitShader = new ShaderModule(_api, "./assets/shaders/RayTracing.Procedural.rchit.spv");
			var proceduralIntersectionShader = new ShaderModule(_api, "./assets/shaders/RayTracing.Procedural.rint.spv");

			Span<PipelineShaderStageCreateInfo> shaderStages = stackalloc PipelineShaderStageCreateInfo[]
			{
				rayGenShader.CreateShaderStage(ShaderStageFlags.ShaderStageRaygenBitKhr),
				missShader.CreateShaderStage(ShaderStageFlags.ShaderStageMissBitKhr),
				closestHitShader.CreateShaderStage(ShaderStageFlags.ShaderStageClosestHitBitKhr),
				proceduralClosestHitShader.CreateShaderStage(ShaderStageFlags.ShaderStageClosestHitBitKhr),
				proceduralIntersectionShader.CreateShaderStage(ShaderStageFlags.ShaderStageIntersectionBitKhr)
			};

			const uint VK_SHADER_UNUSED_KHR = (~0U);

			// Shader groups
			var rayGenGroupInfo = new RayTracingShaderGroupCreateInfoKHR();
			rayGenGroupInfo.SType = StructureType.RayTracingShaderGroupCreateInfoKhr;
			rayGenGroupInfo.PNext = null;
			rayGenGroupInfo.Type = RayTracingShaderGroupTypeKHR.RayTracingShaderGroupTypeGeneralKhr;
			rayGenGroupInfo.GeneralShader = 0;
			rayGenGroupInfo.ClosestHitShader = VK_SHADER_UNUSED_KHR;
			rayGenGroupInfo.AnyHitShader = VK_SHADER_UNUSED_KHR;
			rayGenGroupInfo.IntersectionShader = VK_SHADER_UNUSED_KHR;
			_rayGenIndex = 0;

			var missGroupInfo = new RayTracingShaderGroupCreateInfoKHR();
			missGroupInfo.SType = StructureType.RayTracingShaderGroupCreateInfoKhr;
			missGroupInfo.PNext = null;
			missGroupInfo.Type = RayTracingShaderGroupTypeKHR.RayTracingShaderGroupTypeGeneralKhr;
			missGroupInfo.GeneralShader = 1;
			missGroupInfo.ClosestHitShader = VK_SHADER_UNUSED_KHR;
			missGroupInfo.AnyHitShader = VK_SHADER_UNUSED_KHR;
			missGroupInfo.IntersectionShader = VK_SHADER_UNUSED_KHR;
			_missIndex = 1;

			var triangleHitGroupInfo = new RayTracingShaderGroupCreateInfoKHR();
			triangleHitGroupInfo.SType = StructureType.RayTracingShaderGroupCreateInfoKhr;
			triangleHitGroupInfo.PNext = null;
			triangleHitGroupInfo.Type = RayTracingShaderGroupTypeKHR.RayTracingShaderGroupTypeTrianglesHitGroupKhr;
			triangleHitGroupInfo.GeneralShader = VK_SHADER_UNUSED_KHR;
			triangleHitGroupInfo.ClosestHitShader = 2;
			triangleHitGroupInfo.AnyHitShader = VK_SHADER_UNUSED_KHR;
			triangleHitGroupInfo.IntersectionShader = VK_SHADER_UNUSED_KHR;
			_triangleHitGroupIndex = 2;

			var proceduralHitGroupInfo = new RayTracingShaderGroupCreateInfoKHR();
			proceduralHitGroupInfo.SType = StructureType.RayTracingShaderGroupCreateInfoKhr;
			proceduralHitGroupInfo.PNext = null;
			proceduralHitGroupInfo.Type = RayTracingShaderGroupTypeKHR.RayTracingShaderGroupTypeProceduralHitGroupKhr;
			proceduralHitGroupInfo.GeneralShader = VK_SHADER_UNUSED_KHR;
			proceduralHitGroupInfo.ClosestHitShader = 3;
			proceduralHitGroupInfo.AnyHitShader = VK_SHADER_UNUSED_KHR;
			proceduralHitGroupInfo.IntersectionShader = 4;
			_proceduralHitGroupIndex = 3;

			Span<RayTracingShaderGroupCreateInfoKHR> groups = stackalloc RayTracingShaderGroupCreateInfoKHR[]
			{
				rayGenGroupInfo,
				missGroupInfo,
				triangleHitGroupInfo,
				proceduralHitGroupInfo,
			};

			// Create graphic pipeline
			var pipelineInfo = new RayTracingPipelineCreateInfoKHR();
			pipelineInfo.SType = StructureType.RayTracingPipelineCreateInfoKhr;
			pipelineInfo.PNext = null;
			pipelineInfo.Flags = 0;
			pipelineInfo.StageCount = (uint)shaderStages.Length;
			pipelineInfo.PStages = (PipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref shaderStages[0]);
			pipelineInfo.GroupCount = (uint)groups.Length;
			pipelineInfo.PGroups = (RayTracingShaderGroupCreateInfoKHR*)Unsafe.AsPointer(ref groups[0]);
			pipelineInfo.MaxPipelineRayRecursionDepth = 1;
			pipelineInfo.Layout = _pipelineLayout.VkPipelineLayout;
			pipelineInfo.BasePipelineHandle = default;
			pipelineInfo.BasePipelineIndex = 0;

			Util.Verify(
				_api.KhrRayTracingPipeline.CreateRayTracingPipelines(_api.Device.VkDevice, default, default, 1, pipelineInfo, default, out _pipeline),
				$"{nameof(RayTracingPipeline)}: Unable to create ray tracing pipeline");

			rayGenShader.Dispose();
			missShader.Dispose();
			closestHitShader.Dispose();
			proceduralClosestHitShader.Dispose();
			proceduralIntersectionShader.Dispose();
		}

		public Pipeline VkPipeline => _pipeline;
		public PipelineLayout PipelineLayout => _pipelineLayout;
		public uint RayGenShaderIndex => _rayGenIndex;
		public uint MissShaderIndex => _missIndex;
		public uint TriangleHitGroupIndex => _triangleHitGroupIndex;
		public uint ProceduralHitGroupIndex => _proceduralHitGroupIndex;

		public ref DescriptorSet DescriptorSet(int index) =>
			ref _descriptorSetManager.DescriptorSets.GetAt(index);

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_pipelineLayout.Dispose();
					_descriptorSetManager.Dispose();
				}

				_api.Vk.DestroyPipeline(_api.Device.VkDevice, _pipeline, default);
				_disposedValue = true;
			}
		}

		~RayTracingPipeline()
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
