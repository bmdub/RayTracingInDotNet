using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VkDescriptorSetLayout = Silk.NET.Vulkan.DescriptorSetLayout;

namespace RayTracingInDotNet.Vulkan
{
	class DescriptorSets : IDisposable
	{
		private readonly Api _api;
		private readonly DescriptorPool _descriptorPool;
		private readonly Dictionary<uint, DescriptorType> _bindingTypes;
		private readonly DescriptorSet[] _vkDescriptorSets;
		private bool _disposedValue;

		public unsafe DescriptorSets(Api api, DescriptorPool descriptorPool, DescriptorSetLayout layout, Dictionary<uint, DescriptorType> bindingTypes, ulong size)
		{
			(_api, _descriptorPool, _bindingTypes) = (api, descriptorPool, bindingTypes);

			Span<VkDescriptorSetLayout> layouts = stackalloc VkDescriptorSetLayout[(int)size];
			for (int i = 0; i < layouts.Length; i++)
				layouts[i] = layout.VkDescriptorSetLayout;

			var allocInfo = new DescriptorSetAllocateInfo();
			allocInfo.SType = StructureType.DescriptorSetAllocateInfo;
			allocInfo.DescriptorPool = descriptorPool.VkDescriptorPool;
			allocInfo.DescriptorSetCount = (uint)size;
			allocInfo.PSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref layouts[0]);

			_vkDescriptorSets = new DescriptorSet[(int)size];

			fixed (DescriptorSet* pDescriptorSets = &_vkDescriptorSets[0])
			{
				Util.Verify(
				_api.Vk.AllocateDescriptorSets(_api.Device.VkDevice, allocInfo, pDescriptorSets),
					$"{nameof(descriptorPool)}: Cannot allocate descriptor sets");
			}
		}

		public ref DescriptorSet GetAt(int index) => ref _vkDescriptorSets[index];

		public unsafe void Bind(int index, uint binding, DescriptorBufferInfo* bufferInfo, out WriteDescriptorSet descriptorWrite, uint count = 1)
		{
			descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.SType = StructureType.WriteDescriptorSet;
			descriptorWrite.DstSet = _vkDescriptorSets[index];
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorType = GetBindingType(binding);
			descriptorWrite.DescriptorCount = count;
			descriptorWrite.PBufferInfo = bufferInfo;
		}

		public unsafe void Bind(int index, uint binding, DescriptorImageInfo* imageInfo, out WriteDescriptorSet descriptorWrite, uint count = 1)
		{
			descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.SType = StructureType.WriteDescriptorSet;
			descriptorWrite.DstSet = _vkDescriptorSets[index];
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorType = GetBindingType(binding);
			descriptorWrite.DescriptorCount = count;
			descriptorWrite.PImageInfo = imageInfo;
		}

		public unsafe void Bind(int index, uint binding, WriteDescriptorSetAccelerationStructureKHR* structureInfo, out WriteDescriptorSet descriptorWrite, uint count = 1)
		{
			descriptorWrite = new WriteDescriptorSet();
			descriptorWrite.SType = StructureType.WriteDescriptorSet;
			descriptorWrite.DstSet = _vkDescriptorSets[index];
			descriptorWrite.DstBinding = binding;
			descriptorWrite.DstArrayElement = 0;
			descriptorWrite.DescriptorType = GetBindingType(binding);
			descriptorWrite.DescriptorCount = count;
			descriptorWrite.PNext = structureInfo;
		}

		public unsafe void UpdateDescriptors(ReadOnlySpan<WriteDescriptorSet> descriptorWrites)
		{
			_api.Vk.UpdateDescriptorSets(_api.Vk.CurrentDevice.Value, (uint)descriptorWrites.Length, descriptorWrites, 0, (CopyDescriptorSet*)0);
		}

		public DescriptorType GetBindingType(uint binding)
		{
			if (_bindingTypes.TryGetValue(binding, out var value) == false)
				throw new Exception($"{nameof(DescriptorSets)}: Binding not found in descriptor sets");

			return value;
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				if (_vkDescriptorSets.Length > 0)
				{
					fixed (DescriptorSet* pSets = &_vkDescriptorSets[0])
					{
						Util.Verify(
							_api.Vk.FreeDescriptorSets(_api.Device.VkDevice, _descriptorPool.VkDescriptorPool, (uint)_vkDescriptorSets.Length, pSets),
								$"{nameof(DescriptorSets)}: Unable to free descriptor sets");
					}
				}
				_disposedValue = true;
			}
		}

		~DescriptorSets()
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
