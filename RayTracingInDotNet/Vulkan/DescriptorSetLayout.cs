using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VkDescriptorSetLayout = Silk.NET.Vulkan.DescriptorSetLayout;

namespace RayTracingInDotNet.Vulkan
{
	class DescriptorSetLayout : IDisposable
	{
		private readonly Api _api;
		private readonly VkDescriptorSetLayout _vkDescriptorSetLayout;
		private bool _disposedValue;

		public unsafe DescriptorSetLayout(Api api, DescriptorBinding[] descriptorBindings)
		{
			_api = api;

			Span<DescriptorSetLayoutBinding> layoutBindings = stackalloc DescriptorSetLayoutBinding[descriptorBindings.Length];

			for (int i = 0; i < descriptorBindings.Length; i++)
			{
				layoutBindings[i].Binding = descriptorBindings[i].Binding;
				layoutBindings[i].DescriptorCount = descriptorBindings[i].DescriptorCount;
				layoutBindings[i].DescriptorType = descriptorBindings[i].Type;
				layoutBindings[i].StageFlags = descriptorBindings[i].Stage;
			}

			var layoutInfo = new DescriptorSetLayoutCreateInfo();
			layoutInfo.SType = StructureType.DescriptorSetLayoutCreateInfo;
			layoutInfo.BindingCount = (uint)layoutBindings.Length;
			layoutInfo.PBindings = (DescriptorSetLayoutBinding*)Unsafe.AsPointer(ref layoutBindings[0]);

			Util.Verify(_api.Vk.CreateDescriptorSetLayout(_api.Device.VkDevice, layoutInfo, default, out _vkDescriptorSetLayout), $"{nameof(DescriptorSetLayout)}: Unable to create descriptor set layout");
		}

		public VkDescriptorSetLayout VkDescriptorSetLayout => _vkDescriptorSetLayout;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyDescriptorSetLayout(_api.Device.VkDevice, _vkDescriptorSetLayout, default);
				_disposedValue = true;
			}
		}

		~DescriptorSetLayout()
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
