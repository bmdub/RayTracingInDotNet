using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	class UniformBuffer : IDisposable
	{
		private readonly Buffer _buffer;
		private readonly DeviceMemory _memory;
		private bool disposedValue;

		public UniformBuffer(Api api)
		{
			var bufferSize = Unsafe.SizeOf<UniformBufferObject>();

			_buffer = new Buffer(api, (ulong)bufferSize, BufferUsageFlags.BufferUsageUniformBufferBit);
			_memory = _buffer.AllocateMemory(MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit);
		}

		public Buffer Buffer => _buffer;

		public unsafe void SetValue(ref UniformBufferObject ubo)
		{
			var data = _memory.Map(0, (ulong)Unsafe.SizeOf<UniformBufferObject>());
			Unsafe.CopyBlock(data, Unsafe.AsPointer(ref ubo), (uint)Unsafe.SizeOf<UniformBufferObject>());
			_memory.Unmap();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_buffer.Dispose();
					_memory.Dispose();
				}

				disposedValue = true;
			}
		}

		// ~UniformBuffer()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
