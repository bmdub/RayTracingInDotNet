# Ray Tracing In Dot Net

<img align="center" src="https://github.com/bmdub/RayTracingInDotNet/blob/master/gallery/Physics.png">

Ray Tracing In Dot Net is a real-time path tracing demo that performs hardware ray tracing via Vulkan's ray tracing extension.  It is a port of [Ray Tracing In Vulkan](https://github.com/GPSnoopy/RayTracingInVulkan) to .NET and C# using [Silk.Net](https://github.com/dotnet/Silk.NET), with some added features.


## Added Features

- Dynamic scenes (allow update transformations to models, rebuilding TLAS.)
- Added "Cascading Mirrors" demo scene.
- Added "Physics" demo scene using [Bepuphysics v2](https://github.com/bepu/bepuphysics2).
- Toggles for full screen mode and vsync.


## Requirements

- A GPU with ray tracing acceleration cores (NVIDIA RTX 2000 series / AMD RX 6000 series or better GPU.  Older GPUs _may_ work if the vender has enabled fallback shader-based ray tracing, albeit much more slowly.)

- Up-to-date display drivers for your GPU.


## Gallery

<img src="https://github.com/bmdub/RayTracingInDotNet/blob/master/gallery/Cornell Box.png" width="49%"></img>
<img src="https://github.com/bmdub/RayTracingInDotNet/blob/master/gallery/Cascading Mirrors.png" width="49%"></img>
<img src="https://github.com/bmdub/RayTracingInDotNet/blob/master/gallery/Ray Tracing in One Weekend.png" width="49%"></img>
<img src="https://github.com/bmdub/RayTracingInDotNet/blob/master/gallery/Lucy in One Weekend.png" width="49%"></img>


## Building

You will need the [Vulkan SDK](https://vulkan.lunarg.com/sdk/home) from the official site.


## Todo

- Needs a good denoiser (or _any_ denoiser, for that matter.)
- Optimize BLAS meshes by sharing identical ones across TLAS instances.
- Add more materials / make them more flexible.
- Allow a user to load external model files.
