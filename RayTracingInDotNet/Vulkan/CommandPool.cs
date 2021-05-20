using Silk.NET.Vulkan;
using System;
using VkCommandPool = Silk.NET.Vulkan.CommandPool;

namespace RayTracingInDotNet.Vulkan
{
	class CommandPool : IDisposable
	{
		private readonly Api _api;
		private readonly VkCommandPool _vkCommandPool;
		private bool _disposedValue;

		public unsafe CommandPool(Api api, uint queueFamilyIndex, bool allowReset)
		{
			_api = api;

			var poolInfo = new CommandPoolCreateInfo();
			poolInfo.SType = StructureType.CommandPoolCreateInfo;
			poolInfo.QueueFamilyIndex = queueFamilyIndex;
			poolInfo.Flags = allowReset ? CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit : 0;

			Util.Verify(_api.Vk.CreateCommandPool(_api.Device.VkDevice, poolInfo, null, out _vkCommandPool), $"{nameof(CommandPool)}: Unable to create a command pool");
		}

		public VkCommandPool VkCommandPool => _vkCommandPool;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyCommandPool(_api.Device.VkDevice, _vkCommandPool, null);
				_disposedValue = true;
			}
		}

		~CommandPool()
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
