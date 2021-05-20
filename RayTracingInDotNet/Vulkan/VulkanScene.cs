using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.Vulkan;
using VkImageView = Silk.NET.Vulkan.ImageView;
using VkSampler = Silk.NET.Vulkan.Sampler;

namespace RayTracingInDotNet.Vulkan
{
	class VulkanScene : IDisposable
	{
		private readonly Api _api;
		private readonly List<Model> _models;
		private readonly List<Texture> _textures;
		private readonly Buffer _vertexBuffer;
		private readonly DeviceMemory _vertexBufferMemory;
		private readonly Buffer _indexBuffer;
		private readonly DeviceMemory _indexBufferMemory;
		private readonly Buffer _materialBuffer;
		private readonly DeviceMemory _materialBufferMemory;
		private readonly Buffer _offsetBuffer;
		private readonly DeviceMemory _offsetBufferMemory;
		private readonly Buffer _transformBuffer;
		private readonly DeviceMemory _transformBufferMemory;
		private readonly Buffer _aabbBuffer;
		private readonly DeviceMemory _aabbBufferMemory;
		private readonly Buffer _proceduralBuffer;
		private readonly DeviceMemory _proceduralBufferMemory;
		private readonly List<TextureImage> _textureImages;
		private readonly List<VkImageView> _textureImageViews;
		private readonly List<VkSampler> _textureSamplers;
		private bool _disposedValue;

		public VulkanScene(Api api, CommandPool commandPool, List<Model> models, List<Texture> textures)
		{
			(_api, _models, _textures) = (api, models, textures);

			// Note: From scene to acceleration structure, it seems that vertex data etc.
			// gets copied 3 times.  May have room for optimization.

			// Concatenate all the models
			var vertices = new List<Vertex>();
			var indices = new List<uint>();
			var materials = new List<Material>();
			var procedurals = new List<Vector4>();
			var aabbs = new List<AabbPositionsKHR>();
			var offsets = new List<(uint X, uint Y)>();
			var transforms = new List<Matrix4x4>();

			foreach (var model in _models)
			{
				// Remember the index, vertex offsets.
				var indexOffset = indices.Count;
				var vertexOffset = vertices.Count;
				var materialOffset = materials.Count;

				offsets.Add(((uint)indexOffset, (uint)vertexOffset));
				transforms.Add(model.Transform);

				// Copy model data one after the other.
				vertices.AddRange(model.Vertices);
				indices.AddRange(model.Indices);
				materials.AddRange(model.Materials);

				// Adjust the material id.
				for (int i = (int)vertexOffset; i != vertices.Count; i++)
				{
					var temp = vertices[i];
					temp.MaterialIndex += materialOffset;
					vertices[i] = temp;
				}

				// Add optional procedurals.
				var sphere = model.Procedural as Sphere;
				if (sphere != null)
				{
					var aabb = sphere.BoundingBox;

					aabbs.Add(new AabbPositionsKHR(aabb.Item1.X, aabb.Item1.Y, aabb.Item1.Z, aabb.Item2.X, aabb.Item2.Y, aabb.Item2.Z));
					procedurals.Add(new Vector4(sphere.Center, sphere.Radius));
				}
				else
				{
					aabbs.Add(default);
					procedurals.Add(default);
				}
			}

			Transforms = transforms.ToArray();

			BufferUtil.CreateDeviceBuffer(api, commandPool, "Vertices", BufferUsageFlags.BufferUsageVertexBufferBit | BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, vertices.ToArray(), out _vertexBuffer, out _vertexBufferMemory);
			BufferUtil.CreateDeviceBuffer(api, commandPool, "Indices", BufferUsageFlags.BufferUsageIndexBufferBit | BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, indices.ToArray(), out _indexBuffer, out _indexBufferMemory);
			BufferUtil.CreateDeviceBuffer(api, commandPool, "Materials", BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, materials.ToArray(), out _materialBuffer, out _materialBufferMemory);
			BufferUtil.CreateDeviceBuffer(api, commandPool, "Offsets", BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, offsets.ToArray(), out _offsetBuffer, out _offsetBufferMemory);
			BufferUtil.CreateDeviceBuffer(api, commandPool, "Transforms", BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, Transforms, out _transformBuffer, out _transformBufferMemory);

			BufferUtil.CreateDeviceBuffer(api, commandPool, "AABBs", BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, aabbs.ToArray(), out _aabbBuffer, out _aabbBufferMemory);
			BufferUtil.CreateDeviceBuffer(api, commandPool, "Procedurals", BufferUsageFlags.BufferUsageStorageBufferBit | BufferUsageFlags.BufferUsageShaderDeviceAddressBit, procedurals.ToArray(), out _proceduralBuffer, out _proceduralBufferMemory);

			if (procedurals.Count > 0)
				HasProcedurals = true;

			// Upload all textures
			_textureImages = new List<TextureImage>(_textures.Count);
			_textureImageViews = new List<VkImageView>(_textures.Count);
			_textureSamplers = new List<VkSampler>(_textures.Count);

			for (int i = 0; i != _textures.Count; i++)
			{
				_textureImages.Add(new TextureImage(api, commandPool, _textures[i]));
				_textureImageViews.Add(_textureImages[i].ImageView.VkImageView);
				_textureSamplers.Add(_textureImages[i].Sampler.VkSampler);
			}
		}

		public List<Model> Models => _models;
		public Buffer AabbBuffer => _aabbBuffer;
		public Buffer VertexBuffer => _vertexBuffer;
		public Buffer IndexBuffer => _indexBuffer;
		public Buffer MaterialBuffer => _materialBuffer;
		public Buffer OffsetsBuffer => _offsetBuffer;
		public Buffer TransformsBuffer => _transformBuffer;
		public Buffer ProceduralBuffer => _proceduralBuffer;
		public List<VkImageView> TextureImageViews => _textureImageViews;
		public List<VkSampler> TextureSamplers => _textureSamplers;
		public Matrix4x4[] Transforms { get; init; }
		public readonly bool HasProcedurals;

		public void UpdateTransforms(Matrix4x4[] transforms, CommandPool commandPool)
		{
			BufferUtil.CopyFromStagingBuffer(_api, commandPool, _transformBuffer, transforms);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_textureSamplers.Clear();
					_textureImageViews.Clear();
					foreach (var item in _textureImages) 
						item.Dispose();
					 _textureImages.Clear();
					_proceduralBuffer.Dispose();
					_proceduralBufferMemory.Dispose(); 
					_aabbBuffer.Dispose();
					_aabbBufferMemory.Dispose(); 
					_offsetBuffer.Dispose();
					_offsetBufferMemory.Dispose(); 
					_transformBuffer.Dispose();
					_transformBufferMemory.Dispose();
					_materialBuffer.Dispose();
					_materialBufferMemory.Dispose(); 
					_indexBuffer.Dispose();
					_indexBufferMemory.Dispose(); 
					_vertexBuffer.Dispose();
					_vertexBufferMemory.Dispose(); 
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
