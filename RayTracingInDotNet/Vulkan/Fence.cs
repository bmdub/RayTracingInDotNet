using Silk.NET.Vulkan;
using System;
using VkFence = Silk.NET.Vulkan.Fence;

namespace RayTracingInDotNet.Vulkan
{
	class Fence : IDisposable
	{
		private readonly Api _api;
		private readonly VkFence _vkFence;
		private bool _disposedValue;

		public unsafe Fence(Api api, bool signaled)
		{
			_api = api;

			var fenceInfo = new FenceCreateInfo();
			fenceInfo.SType = StructureType.FenceCreateInfo;
			fenceInfo.Flags = signaled ? FenceCreateFlags.FenceCreateSignaledBit : 0;

			Util.Verify(_api.Vk.CreateFence(_api.Device.VkDevice, fenceInfo, default, out _vkFence), $"{nameof(Fence)}: Unable to create a fence");
		}

		public VkFence VkFence => _vkFence;

		public void Reset() =>
			Util.Verify(_api.Vk.ResetFences(_api.Device.VkDevice, 1, _vkFence), $"{nameof(Fence)}: Unable to reset fence");

		public void Wait(ulong timeout) =>
			Util.Verify(_api.Vk.WaitForFences(_api.Device.VkDevice, 1, _vkFence, new Silk.NET.Core.Bool32(true), timeout), $"{nameof(Fence)}: Unable to wait for fence");

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyFence(_api.Device.VkDevice, _vkFence, default);
				_disposedValue = true;
			}
		}

		~Fence()
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
