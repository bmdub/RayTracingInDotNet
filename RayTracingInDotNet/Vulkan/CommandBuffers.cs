using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	class CommandBuffers : IDisposable
	{
		private readonly Api _api;
		private readonly CommandPool _commandPool;
		private readonly CommandBuffer[] _commandBuffers;
		private bool _disposedValue;

		public unsafe CommandBuffers(Api api, CommandPool commandPool, uint count)
		{
			(_api, _commandPool) = (api, commandPool);

			var allocInfo = new CommandBufferAllocateInfo();
			allocInfo.SType = StructureType.CommandBufferAllocateInfo;
			allocInfo.CommandPool = _commandPool.VkCommandPool;
			allocInfo.Level = CommandBufferLevel.Primary;
			allocInfo.CommandBufferCount = count;

			_commandBuffers = GC.AllocateArray<CommandBuffer>((int)count, true);
			Util.Verify(_api.Vk.AllocateCommandBuffers(_api.Device.VkDevice, &allocInfo, (CommandBuffer*)Unsafe.AsPointer(ref _commandBuffers[0])), $"{nameof(CommandBuffers)}: Unable to allocate command buffers");
		}

		public ref CommandBuffer this[int index]
		{
			get => ref _commandBuffers[index];
		}

		public unsafe CommandBuffer* Ptr => 
			(CommandBuffer*)Unsafe.AsPointer(ref _commandBuffers[0]);

		public unsafe ref CommandBuffer Begin(ulong i)
		{
			var beginInfo = new CommandBufferBeginInfo();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageSimultaneousUseBit;
			beginInfo.PInheritanceInfo = null; 

			Util.Verify(_api.Vk.BeginCommandBuffer(_commandBuffers[i], beginInfo), $"{nameof(CommandBuffers)}: Unable to begin recording command buffer");

			return ref _commandBuffers[i];
		}

		public void End(ulong i) =>
			Util.Verify(_api.Vk.EndCommandBuffer(_commandBuffers[i]), $"{nameof(CommandBuffers)}: Unable to end recording command buffer");

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.FreeCommandBuffers(_api.Device.VkDevice, _commandPool.VkCommandPool, (uint)_commandBuffers.Length, _commandBuffers.AsSpan());
				_disposedValue = true;
			}
		}

		~CommandBuffers()
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
