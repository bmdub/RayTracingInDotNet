using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace RayTracingInDotNet.Vulkan
{
	class DescriptorSetManager : IDisposable
	{
		private readonly DescriptorPool _descriptorPool;
		private readonly DescriptorSetLayout _descriptorSetLayout;
		private readonly DescriptorSets _descriptorSets;
		private bool disposedValue;

		public DescriptorSetManager(Api api, DescriptorBinding[] descriptorBindings, ulong maxSets)
		{
			// Sanity check to avoid binding different resources to the same binding point.
			Dictionary<uint, DescriptorType> bindingTypes = new Dictionary<uint, DescriptorType>(descriptorBindings.Length);

			foreach (var binding in descriptorBindings)
				if (bindingTypes.TryAdd(binding.Binding, binding.Type) == false)
					throw new Exception($"{nameof(DescriptorSetManager)}: Binding collision");

			_descriptorPool = new DescriptorPool(api, descriptorBindings, maxSets);
			_descriptorSetLayout = new DescriptorSetLayout(api, descriptorBindings);
			_descriptorSets = new DescriptorSets(api, _descriptorPool, _descriptorSetLayout, bindingTypes, maxSets);
		}

		public DescriptorSetLayout DescriptorSetLayout => _descriptorSetLayout;
		public DescriptorSets DescriptorSets => _descriptorSets;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_descriptorSets.Dispose();
					_descriptorSetLayout.Dispose();
					_descriptorPool.Dispose();
				}

				disposedValue = true;
			}
		}

		~DescriptorSetManager()
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
