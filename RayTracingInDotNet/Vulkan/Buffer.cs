using Silk.NET.Vulkan;
using System;
using VkBuffer = Silk.NET.Vulkan.Buffer;

namespace RayTracingInDotNet.Vulkan
{
	class Buffer : IDisposable
	{
		private readonly Api _api;
		private readonly VkBuffer _vkBuffer;
		private bool _disposedValue;
		public unsafe Buffer(Api api, ulong size, BufferUsageFlags usage)
		{
			_api = api;

			var bufferInfo = new BufferCreateInfo();
			bufferInfo.SType = StructureType.BufferCreateInfo;
			bufferInfo.Size = size;
			bufferInfo.Usage = usage;
			bufferInfo.SharingMode = SharingMode.Exclusive;

			Util.Verify(api.Vk.CreateBuffer(_api.Device.VkDevice, bufferInfo, (AllocationCallbacks*)0, out _vkBuffer), $"{nameof(Buffer)}: Unable to create buffer");
		}

		public VkBuffer VkBuffer => _vkBuffer;

		public DeviceMemory AllocateMemory(MemoryPropertyFlags propertyFlags) =>
			 AllocateMemory(0, propertyFlags);

		public DeviceMemory AllocateMemory(MemoryAllocateFlags allocateFlags, MemoryPropertyFlags propertyFlags)
		{
			_api.Vk.GetBufferMemoryRequirements(_api.Device.VkDevice, _vkBuffer, out var requirements);

			var memory = new DeviceMemory(_api, requirements.Size, requirements.MemoryTypeBits, allocateFlags, propertyFlags);

			Util.Verify(_api.Vk.BindBufferMemory(_api.Device.VkDevice, _vkBuffer, memory.VkDeviceMemory, 0), $"{nameof(Buffer)}: Unable to bind buffer memory");

			return memory;
		}

		public unsafe ulong GetDeviceAddress()
		{
			var info = new BufferDeviceAddressInfo();
			info.SType = StructureType.BufferDeviceAddressInfo;
			info.PNext = null;
			info.Buffer = _vkBuffer;

			return _api.Vk.GetBufferDeviceAddress(_api.Device.VkDevice, info);
		}

		public void CopyFrom(in CommandPool commandPool, Buffer src, ulong size)
		{
			Util.Submit(_api, commandPool, commandBuffer =>
			{
				var copyRegion = new BufferCopy();
				copyRegion.SrcOffset = 0;
				copyRegion.DstOffset = 0;
				copyRegion.Size = size;

				_api.Vk.CmdCopyBuffer(commandBuffer, src._vkBuffer, _vkBuffer, 1, copyRegion);
			});
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyBuffer(_api.Device.VkDevice, _vkBuffer, (AllocationCallbacks*)0);
				_disposedValue = true;
			}
		}

		~Buffer()
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
