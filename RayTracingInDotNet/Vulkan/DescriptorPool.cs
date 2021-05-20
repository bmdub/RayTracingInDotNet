using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VkDescriptorPool = Silk.NET.Vulkan.DescriptorPool;

namespace RayTracingInDotNet.Vulkan
{
	class DescriptorPool : IDisposable
	{
		private readonly Api _api;
		private readonly VkDescriptorPool _vkDescriptorPool;
		private bool _disposedValue;

		public unsafe DescriptorPool(Api api, Span<DescriptorBinding> descriptorBindings, ulong maxSets)
		{
			_api = api;

			Span<DescriptorPoolSize> poolSizes = stackalloc DescriptorPoolSize[descriptorBindings.Length];

			for (int i = 0; i < descriptorBindings.Length; i++)
				poolSizes[i] = new DescriptorPoolSize(descriptorBindings[i].Type, descriptorBindings[i].DescriptorCount * (uint)maxSets);

			var poolInfo = new DescriptorPoolCreateInfo();
			poolInfo.SType = StructureType.DescriptorPoolCreateInfo;
			poolInfo.PoolSizeCount = (uint)poolSizes.Length;
			poolInfo.PPoolSizes = (DescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes[0]);
			poolInfo.MaxSets = (uint)maxSets;
			poolInfo.Flags = DescriptorPoolCreateFlags.DescriptorPoolCreateFreeDescriptorSetBit;

			Util.Verify(_api.Vk.CreateDescriptorPool(_api.Device.VkDevice, poolInfo, default, out _vkDescriptorPool), $"{nameof(DescriptorPool)}: Unable to create descriptor pool");
		}

		public VkDescriptorPool VkDescriptorPool => _vkDescriptorPool;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyDescriptorPool(_api.Device.VkDevice, _vkDescriptorPool, default);
				_disposedValue = true;
			}
		}

		~DescriptorPool()
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
