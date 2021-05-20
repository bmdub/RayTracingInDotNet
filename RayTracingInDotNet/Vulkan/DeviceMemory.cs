using Silk.NET.Vulkan;
using System;
using VkDeviceMemory = Silk.NET.Vulkan.DeviceMemory;

namespace RayTracingInDotNet.Vulkan
{
	class DeviceMemory : IDisposable
	{
		private readonly Api _api;
		private readonly VkDeviceMemory _vkDeviceMemory;
		private bool _disposedValue;

		public unsafe DeviceMemory(Api api, ulong size, uint memoryTypeBits, MemoryAllocateFlags allocateFlags, MemoryPropertyFlags propertyFlags)
		{
			_api = api;

			var flagsInfo = new MemoryAllocateFlagsInfo();
			flagsInfo.SType = StructureType.MemoryAllocateFlagsInfo;
			flagsInfo.PNext = null;
			flagsInfo.Flags = allocateFlags;

			var allocInfo = new MemoryAllocateInfo();
			allocInfo.SType = StructureType.MemoryAllocateInfo;
			allocInfo.PNext = &flagsInfo;
			allocInfo.AllocationSize = size;
			allocInfo.MemoryTypeIndex = FindMemoryType(memoryTypeBits, propertyFlags);

			Util.Verify(_api.Vk.AllocateMemory(_api.Device.VkDevice, allocInfo, null, out _vkDeviceMemory), $"{nameof(DeviceMemory)}: Unable to allocate memory.");
		}

		public VkDeviceMemory VkDeviceMemory => _vkDeviceMemory;

		public unsafe void* Map(ulong offset, ulong size)
		{
			void* data;
			Util.Verify(_api.Vk.MapMemory(_api.Device.VkDevice, _vkDeviceMemory, offset, size, 0, &data), $"{nameof(DeviceMemory)}: Unable to map memory.");

			return data;
		}

		public void Unmap() =>
			_api.Vk.UnmapMemory(_api.Device.VkDevice, _vkDeviceMemory);

		private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags propertyFlags)
		{
			_api.Vk.GetPhysicalDeviceMemoryProperties(_api.Device.PhysicalDevice, out var memProperties);

			for (int i = 0; i != memProperties.MemoryTypeCount; i++)
			{
				if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & propertyFlags) == propertyFlags)
				{
					return (uint)i;
				}
			}

			throw new Exception($"{nameof(DeviceMemory)}: Unable to find suitable memory type.");
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.FreeMemory(_api.Device.VkDevice, _vkDeviceMemory, (AllocationCallbacks*)0);
				_disposedValue = true;
			}
		}

		~DeviceMemory()
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
