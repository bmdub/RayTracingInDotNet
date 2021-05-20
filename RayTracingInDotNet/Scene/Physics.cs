using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RayTracingInDotNet.Scene
{
	[Scene("Physics")]
	class Physics : IScene
	{
		public List<Model> Models { get; private set; } = new List<Model>();
		public List<Texture> Textures { get; private set; } = new List<Texture>();

		private Simulation _simulation;
		private BufferPool _bufferPool;
		private SimpleThreadDispatcher _threadDispatcher;
		private Dictionary<BodyHandle, int> _bodyHandleToModelIndex = new Dictionary<BodyHandle, int>();
		private float _timeAccumulator = -1;

		public void Reset(CameraInitialState camera)
		{
			Models.Clear();
			Textures.Clear();

			camera.ModelView = Matrix4x4.CreateLookAt(new Vector3(-10, 5, 12) * 2, new Vector3(-5, 5, 0), new Vector3(0, 1, 0));
			camera.FieldOfView = 40;
			camera.Aperture = 0.0f;
			camera.FocusDistance = 10.0f;
			camera.ControlSpeed = 5.0f;
			camera.GammaCorrection = true;
			camera.SkyColor1 = new Vector4(.6f, .7f, .9f, 1);
			camera.SkyColor2 = new Vector4(.6f, .7f, .9f, 1);

			// Initialize physics
			_bufferPool = new BufferPool();
			var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
			_threadDispatcher = new SimpleThreadDispatcher(targetThreadCount);
			_simulation = Simulation.Create(_bufferPool, new DemoNarrowPhaseCallbacks(), new DemoPoseIntegratorCallbacks(new Vector3(0, -9.8f, 0)), new PositionFirstTimestepper());

			var random = new Random(42);

			// Add a floor
			const float groundDim = 400;

			Textures.Add(Texture.LoadTexture(@"./assets/textures/laminate.jpg"));
			var floor = Model.CreateGroundRect(new Vector3(0, 0, 0), groundDim, groundDim, Material.Dielectric(5.5f, Textures.Count - 1), groundDim / 16f);
			Models.Add(floor);

			_simulation.Statics.Add(new StaticDescription(new Vector3(0, -.5f, 0), new CollidableDescription(_simulation.Shapes.Add(new Box(groundDim, 1, groundDim)), 0.1f)));

			// Create pyramid
			const float boxDim = 1;
			const float marbleRadius = boxDim / 2f;

			var boxShape = new Box(boxDim, boxDim, boxDim);
			boxShape.ComputeInertia(.1f, out var boxInertia);
			var boxIndex = _simulation.Shapes.Add(boxShape);

			var marbleShape = new BepuPhysics.Collidables.Sphere(marbleRadius);

			var boxTemplate = Model.CreateBox(new Vector3(-(boxDim / 2)), new Vector3(boxDim / 2), default);

			const int pyramidSize = 20;
			const float offset = -(pyramidSize / 2f) * boxDim;
			for (int y = 0; ; y++)
			{
				int startIndex = y;
				int endIndex = pyramidSize - y;
				if (startIndex >= endIndex)
					break;

				for (int x = startIndex; x < endIndex; x++)
				{
					for (int z = startIndex; z < endIndex; z++)
					{
						var box = boxTemplate.Clone();
						box.SetMaterial(Material.Lambertian(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) / 1f));
						box.Transform = Matrix4x4.CreateTranslation(new Vector3(x * boxDim + offset, y * boxDim + (boxDim / 2), z * boxDim + offset));
						Models.Add(box);

						var quat = box.Transform.ToQuaternion();
						var bodyHandle = _simulation.Bodies.Add(
							BodyDescription.CreateDynamic(
								new RigidPose { Position = box.Transform.Translation, Orientation = new Quaternion(quat.X, quat.Y, quat.Z, quat.W) },
								boxInertia,
								new CollidableDescription(boxIndex, 0.1f),
								new BodyActivityDescription(0.01f)));
						_bodyHandleToModelIndex[bodyHandle] = Models.Count - 1;

						// Put some marbles on top of the pyramid
						if ((x == startIndex || x == endIndex - 1) ||
							(z == startIndex || z == endIndex - 1))
						{
							Models.Add(Model.CreateSphere(default, marbleRadius, Material.Metallic(
								   new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) / 2f + new Vector3(.5f),
									0),
								   true));

							Models[^1].Transform = Matrix4x4.CreateTranslation(new Vector3(x * boxDim + offset, (y + 1) * boxDim + (boxDim / 2), z * boxDim + offset));

							var marbleDescription = BodyDescription.CreateConvexDynamic(
								Models[^1].Transform.Translation, new BodyVelocity(default), marbleShape.Radius * marbleShape.Radius * marbleShape.Radius, _simulation.Shapes, marbleShape);
							var marbleHandle = _simulation.Bodies.Add(marbleDescription);
							_bodyHandleToModelIndex[marbleHandle] = Models.Count - 1;
						}
					}
				}
			}

			// Simulate some physics time up front to allow objects to settle (and get through pauses from simulation startup)
			for (int i = 0; i < 200; i++)
				_simulation.Timestep(1 / 120f, _threadDispatcher);

			// Create earth dozer
			const float earthRadius = 5;
			var earthShape = new BepuPhysics.Collidables.Sphere(earthRadius);

			Textures.Add(Texture.LoadTexture("./assets/textures/land_ocean_ice_cloud_2048.png"));
			Models.Add(Model.CreateSphere(default, earthRadius, Material.Lambertian(new Vector3(1), Textures.Count - 1), true));
			Models[^1].Transform = Matrix4x4.CreateTranslation(new Vector3(-100, earthRadius * 2 / 2f, 0));

			var earthDescription = BodyDescription.CreateConvexDynamic(
				Models[^1].Transform.Translation, new BodyVelocity(new Vector3(50, 0, 0)), earthShape.Radius * earthShape.Radius * earthShape.Radius * 2, _simulation.Shapes, earthShape);
			var earthHandle = _simulation.Bodies.Add(earthDescription);
			_bodyHandleToModelIndex[earthHandle] = Models.Count - 1;
		}

		public bool UpdateTransforms(double delta, UserSettings userSettings, Matrix4x4[] transforms)
		{
			// Do physics work
			delta *= userSettings.Speed / 100.0;

			if (_timeAccumulator == -1)
				_timeAccumulator = 0; // Ignore the first delta of the simulation, because it includes loading stuff in (which is a long time)
			else _timeAccumulator += (float)delta;
			var targetTimestepDuration = 1 / 120f;
			while (_timeAccumulator >= targetTimestepDuration)
			{
				_simulation.Timestep(targetTimestepDuration, _threadDispatcher);
				_timeAccumulator -= targetTimestepDuration;
			}

			// Transform any bodies that have moved per physics
			bool anyMovement = false;

			for (int i = 0; i < _simulation.Bodies.ActiveSet.Count; i++)
			{
				var bodyHandle = _simulation.Bodies.ActiveSet.IndexToHandle[i];
				var bodyRef = _simulation.Bodies.GetBodyReference(bodyHandle);
				var pose = bodyRef.Pose;
				var quat = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W);

				if (_bodyHandleToModelIndex.TryGetValue(bodyHandle, out var index) == false)
					continue;

				var newTransform = Matrix4x4.CreateFromQuaternion(quat) * Matrix4x4.CreateTranslation(pose.Position);

				if (newTransform == transforms[index])
					continue;

				transforms[index] = newTransform;

				anyMovement = true;
			}

			return anyMovement;
		}

		// Provided by BepuPhysics2 demo application
		unsafe struct DemoNarrowPhaseCallbacks : INarrowPhaseCallbacks
		{
			public SpringSettings ContactSpringiness;

			public void Initialize(Simulation simulation)
			{
				if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
					ContactSpringiness = new SpringSettings(30, 1);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
			{
				return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
			{
				return true;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
			{
				pairMaterial.FrictionCoefficient = 1f;
				pairMaterial.MaximumRecoveryVelocity = 2f;
				pairMaterial.SpringSettings = ContactSpringiness;
				return true;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
			{
				return true;
			}

			public void Dispose()
			{
			}
		}

		// Provided by BepuPhysics2 demo application
		public struct DemoPoseIntegratorCallbacks : IPoseIntegratorCallbacks
		{
			public Vector3 Gravity;
			public float LinearDamping;
			public float AngularDamping;

			Vector3 gravityDt;
			float linearDampingDt;
			float angularDampingDt;

			public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

			public void Initialize(Simulation simulation)
			{
			}

			public DemoPoseIntegratorCallbacks(Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f) : this()
			{
				Gravity = gravity;
				LinearDamping = linearDamping;
				AngularDamping = angularDamping;
			}

			public void PrepareForIntegration(float dt)
			{
				gravityDt = Gravity * dt;
				linearDampingDt = MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt);
				angularDampingDt = MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
			{
				if (localInertia.InverseMass > 0)
				{
					velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
					velocity.Angular = velocity.Angular * angularDampingDt;
				}
			}
		}

		// Provided by BepuPhysics2 demo application
		public class SimpleThreadDispatcher : IThreadDispatcher, IDisposable
		{
			int threadCount;
			public int ThreadCount => threadCount;
			struct Worker
			{
				public Thread Thread;
				public AutoResetEvent Signal;
			}

			Worker[] workers;
			AutoResetEvent finished;
			BufferPool[] bufferPools;

			public SimpleThreadDispatcher(int threadCount)
			{
				this.threadCount = threadCount;
				workers = new Worker[threadCount - 1];
				for (int i = 0; i < workers.Length; ++i)
				{
					workers[i] = new Worker { Thread = new Thread(WorkerLoop), Signal = new AutoResetEvent(false) };
					workers[i].Thread.IsBackground = true;
					workers[i].Thread.Start(workers[i].Signal);
				}
				finished = new AutoResetEvent(false);
				bufferPools = new BufferPool[threadCount];
				for (int i = 0; i < bufferPools.Length; ++i)
				{
					bufferPools[i] = new BufferPool();
				}
			}

			void DispatchThread(int workerIndex)
			{
				Debug.Assert(workerBody != null);
				workerBody(workerIndex);

				if (Interlocked.Increment(ref completedWorkerCounter) == threadCount)
				{
					finished.Set();
				}
			}

			volatile Action<int> workerBody;
			int workerIndex;
			int completedWorkerCounter;

			void WorkerLoop(object untypedSignal)
			{
				var signal = (AutoResetEvent)untypedSignal;
				while (true)
				{
					signal.WaitOne();
					if (disposed)
						return;
					DispatchThread(Interlocked.Increment(ref workerIndex) - 1);
				}
			}

			void SignalThreads()
			{
				for (int i = 0; i < workers.Length; ++i)
				{
					workers[i].Signal.Set();
				}
			}

			public void DispatchWorkers(Action<int> workerBody)
			{
				Debug.Assert(this.workerBody == null);
				workerIndex = 1;
				completedWorkerCounter = 0;
				this.workerBody = workerBody;
				SignalThreads();
				DispatchThread(0);
				finished.WaitOne();
				this.workerBody = null;
			}

			volatile bool disposed;
			public void Dispose()
			{
				if (!disposed)
				{
					disposed = true;
					SignalThreads();
					for (int i = 0; i < bufferPools.Length; ++i)
					{
						bufferPools[i].Clear();
					}
					foreach (var worker in workers)
					{
						worker.Thread.Join();
						worker.Signal.Dispose();
					}
				}
			}

			public BufferPool GetThreadMemoryPool(int workerIndex)
			{
				return bufferPools[workerIndex];
			}
		}
	}
}
