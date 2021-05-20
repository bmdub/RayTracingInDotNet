using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInDotNet.Vulkan
{
	static class BufferUtil
	{
		public static unsafe void CopyFromStagingBuffer<T>(Api api, in CommandPool commandPool, Buffer dstBuffer, T[] content) where T : unmanaged
		{
			var contentSize = (uint)Unsafe.SizeOf<T>() * (uint)content.Length;

			// Create a temporary host-visible staging buffer.
			var stagingBuffer = new Buffer(api, contentSize, BufferUsageFlags.BufferUsageTransferSrcBit);
			var stagingBufferMemory = stagingBuffer.AllocateMemory(MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit);
			
			// Copy the host data into the staging buffer.
			var data = stagingBufferMemory.Map(0, contentSize);

			fixed(T* pContent = &content[0])
				Unsafe.CopyBlock(data, (void*)pContent, contentSize);

			stagingBufferMemory.Unmap();
			
			// Copy the staging buffer to the device buffer.
			dstBuffer.CopyFrom(commandPool, stagingBuffer, contentSize);

			stagingBuffer.Dispose();
			stagingBufferMemory.Dispose();
		}

		public static void CreateDeviceBuffer<T>(
			Api api,
			in CommandPool commandPool,
			string name,
			BufferUsageFlags usage,
			T[] content,
			out Buffer buffer,
			out DeviceMemory memory)
			 where T : unmanaged
		{
			var contentSize = (uint)Unsafe.SizeOf<T>() * (uint)content.Length;
			MemoryAllocateFlags allocateFlags = (usage & BufferUsageFlags.BufferUsageShaderDeviceAddressBit) != 0 ? MemoryAllocateFlags.MemoryAllocateDeviceAddressBit : 0;

			buffer = new Buffer(api, contentSize, BufferUsageFlags.BufferUsageTransferDstBit | usage);
			memory = buffer.AllocateMemory(allocateFlags, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);

			api.SetDebugName(buffer.VkBuffer.Handle, $"{name} Buffer", ObjectType.Buffer);
			api.SetDebugName(memory.VkDeviceMemory.Handle, $"{name} Memory", ObjectType.DeviceMemory);

			CopyFromStagingBuffer(api, commandPool, buffer, content);
		}
	}
}
