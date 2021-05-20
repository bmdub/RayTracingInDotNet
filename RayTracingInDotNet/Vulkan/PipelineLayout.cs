using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using VkPipelineLayout = Silk.NET.Vulkan.PipelineLayout;
using VkDescriptorSetLayout = Silk.NET.Vulkan.DescriptorSetLayout;

namespace RayTracingInDotNet.Vulkan
{
	class PipelineLayout : IDisposable
	{
		private readonly Api _api;
		private readonly VkPipelineLayout _vkPipelineLayout;
		private bool _disposedValue;

		public unsafe PipelineLayout(Api api, DescriptorSetLayout descriptorSetLayout)
		{
			_api = api;

			var descriptorSetLayouts = descriptorSetLayout.VkDescriptorSetLayout;

			var pipelineLayoutInfo = new PipelineLayoutCreateInfo();
			pipelineLayoutInfo.SType = StructureType.PipelineLayoutCreateInfo;
			pipelineLayoutInfo.SetLayoutCount = 1;
			pipelineLayoutInfo.PSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref descriptorSetLayouts);
			pipelineLayoutInfo.PushConstantRangeCount = 0;
			pipelineLayoutInfo.PPushConstantRanges = (PushConstantRange*)0;

			Util.Verify(_api.Vk.CreatePipelineLayout(_api.Device.VkDevice, pipelineLayoutInfo, default, out _vkPipelineLayout), $"{nameof(PipelineLayout)}: Failed to create pipeline layout");
		}

		public VkPipelineLayout VkPipelineLayout => _vkPipelineLayout;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyPipelineLayout(_api.Device.VkDevice, _vkPipelineLayout, default);
				_disposedValue = true;
			}
		}

		~PipelineLayout()
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
