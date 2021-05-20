using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;

namespace RayTracingInDotNet.Vulkan
{
	class ShaderBindingTable : IDisposable
	{
		private readonly Buffer _buffer;
		private readonly DeviceMemory _bufferMemory;
		private bool _disposedValue;

		public record Entry(uint GroupIndex, byte[] InlineData);

		public unsafe ShaderBindingTable(
			Api api,
			RayTracingPipeline rayTracingPipeline,
			RayTracingProperties rayTracingProperties,
			List<Entry> rayGenPrograms,
			List<Entry> missPrograms,
			List<Entry> hitGroups)
		{
			RayGenEntrySize = GetEntrySize(rayTracingProperties, rayGenPrograms);
			MissEntrySize = GetEntrySize(rayTracingProperties, missPrograms);
			HitGroupEntrySize = GetEntrySize(rayTracingProperties, hitGroups);

			RayGenOffset = 0;
			MissOffset = (ulong)rayGenPrograms.Count * RayGenEntrySize;
			HitGroupOffset = MissOffset + (ulong)missPrograms.Count * MissEntrySize;

			RayGenSize = (ulong)rayGenPrograms.Count * RayGenEntrySize;
			MissSize = (ulong)missPrograms.Count * MissEntrySize;
			HitGroupSize = (ulong)hitGroups.Count * HitGroupEntrySize;

			// Compute the size of the table.
			ulong sbtSize =
				(ulong)rayGenPrograms.Count * RayGenEntrySize +
				(ulong)missPrograms.Count * MissEntrySize +
				(ulong)hitGroups.Count * HitGroupEntrySize;

			// Allocate buffer & memory.
			_buffer = new Buffer(api, sbtSize, BufferUsageFlags.BufferUsageShaderDeviceAddressBit | BufferUsageFlags.BufferUsageTransferSrcBit);
			_bufferMemory = _buffer.AllocateMemory(MemoryAllocateFlags.MemoryAllocateDeviceAddressBit, MemoryPropertyFlags.MemoryPropertyHostVisibleBit);

			// Generate the table.
			uint handleSize = rayTracingProperties.ShaderGroupHandleSize;
			ulong groupCount = (ulong)rayGenPrograms.Count + (ulong)missPrograms.Count + (ulong)hitGroups.Count;
			byte[] shaderHandleStorage = new byte[groupCount * handleSize];

			fixed (byte* shaderHandleStoragePtr = &shaderHandleStorage[0])
			{
				Util.Verify(api.KhrRayTracingPipeline.GetRayTracingShaderGroupHandles(
					api.Device.VkDevice,
					rayTracingPipeline.VkPipeline,
					0, 
					(uint)groupCount,
					(nuint)shaderHandleStorage.Length,
					shaderHandleStoragePtr
					), $"{nameof(ShaderBindingTable)}: Unable to get ray tracing shader group handles");

				// Copy the shader identifiers followed by their resource pointers or root constants: 
				// first the ray generation, then the miss shaders, and finally the set of hit groups.
				byte* pData = (byte*)_bufferMemory.Map(0, sbtSize);

				pData += CopyShaderData(pData, rayTracingProperties, rayGenPrograms, RayGenEntrySize, shaderHandleStoragePtr);
				pData += CopyShaderData(pData, rayTracingProperties, missPrograms, MissEntrySize, shaderHandleStoragePtr);
				CopyShaderData(pData, rayTracingProperties, hitGroups, HitGroupEntrySize, shaderHandleStoragePtr);
			}

			_bufferMemory.Unmap();
		}

		public Buffer Buffer => _buffer;
		public ulong RayGenDeviceAddress => _buffer.GetDeviceAddress() + RayGenOffset;
		public ulong MissDeviceAddress => _buffer.GetDeviceAddress() + MissOffset;
		public ulong HitGroupDeviceAddress => _buffer.GetDeviceAddress() + HitGroupOffset;

		public ulong RayGenOffset { get; init; }
		public ulong MissOffset { get; init; }
		public ulong HitGroupOffset { get; init; }

		public ulong RayGenSize { get; init; }
		public ulong MissSize { get; init; }
		public ulong HitGroupSize { get; init; }

		public ulong RayGenEntrySize { get; init; }
		public ulong MissEntrySize { get; init; }
		public ulong HitGroupEntrySize { get; init; }

		private ulong RoundUp(ulong size, ulong powerOf2Alignment)
		{
			return (size + powerOf2Alignment - 1) & ~(powerOf2Alignment - 1);
		}

		private ulong GetEntrySize(RayTracingProperties rayTracingProperties, List<Entry> entries)
		{
			// Find the maximum number of parameters used by a single entry
			ulong maxArgs = 0;

			foreach (var entry in entries)
				maxArgs = Math.Max(maxArgs, (ulong)entry.InlineData.Length);

			// A SBT entry is made of a program ID and a set of 4-byte parameters (see shaderRecordEXT).
			// Its size is ShaderGroupHandleSize (plus parameters) and must be aligned to ShaderGroupBaseAlignment.
			return RoundUp(rayTracingProperties.ShaderGroupHandleSize + maxArgs, rayTracingProperties.ShaderGroupBaseAlignment);
		}

		private unsafe ulong CopyShaderData(
			byte* dst,
			RayTracingProperties rayTracingProperties,
			List<Entry> entries,
			ulong entrySize,
			byte* shaderHandleStorage)
		{
			var handleSize = rayTracingProperties.ShaderGroupHandleSize;

			byte* pDst = dst;

			foreach (var entry in entries)
			{
				// Copy the shader identifier that was previously obtained with vkGetRayTracingShaderGroupHandlesKHR.
				Unsafe.CopyBlock(pDst, shaderHandleStorage + entry.GroupIndex * handleSize, handleSize);

				if (entry.InlineData.Length > 0)
					fixed (byte* inlinePtr = &entry.InlineData[0])
						Unsafe.CopyBlock(pDst + handleSize, inlinePtr, (uint)entry.InlineData.Length);

				pDst += entrySize;
			}

			return (ulong)entries.Count * entrySize;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_buffer.Dispose();
					_bufferMemory.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
