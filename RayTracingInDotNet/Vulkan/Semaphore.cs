using Silk.NET.Vulkan;
using System;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace RayTracingInDotNet.Vulkan
{
	class Semaphore : IDisposable
	{
		private readonly Api _api;
		private readonly VkSemaphore _vkSemaphore;
		private bool _disposedValue;

		public unsafe Semaphore(Api api)
		{
			_api = api;

			var semaphoreInfo = new SemaphoreCreateInfo();
			semaphoreInfo.SType = StructureType.SemaphoreCreateInfo;

			Util.Verify(_api.Vk.CreateSemaphore(_api.Device.VkDevice, semaphoreInfo, default, out _vkSemaphore), $"{nameof(Semaphore)}: Failed to create semaphore");
		}

		public VkSemaphore VkSemaphore => _vkSemaphore;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroySemaphore(_api.Device.VkDevice, _vkSemaphore, default);
				_disposedValue = true;
			}
		}

		~Semaphore()
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
