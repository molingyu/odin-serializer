<br />
<p align="center">
    <img src="/Images/OdinSerializerLogo.png" alt="Odin Serializer">
</p>
<h3 align="center" style="text-align:center;">
	Fast, robust, powerful and extendible .NET serializer — now with Godot support
</h3>
<p align="center">
	This repository is a fork of <a href="https://github.com/sirenix/odin-serializer">sirenix/odin-serializer</a>
	that adds first-class <a href="https://godotengine.org/">Godot</a> support (C# / mono) on top of the original Unity support.
</p>
<hr>

## About this fork

OdinSerializer is the open-source version of the custom serializer built for and used by
[Odin - Inspector & Serializer](https://odininspector.com/), created by Sirenix IVS and
licensed under Apache 2.0. This fork, **OdinSerializerForGodot**, ports it to Godot while
keeping the original Unity build working. All credit for the core serializer goes to the
upstream authors; see [LICENSE](LICENSE) (Apache 2.0).

What the Godot support adds:

- Formatters for all Godot Variant-compatible types: vectors, Quaternion, Color, Rect2/Rect2I,
  Plane, Aabb, Basis, Transform2D/3D, Projection, StringName, NodePath, Callable, Signal
- Full `Variant` serialization, dispatched by concrete `Variant.Type`
- `Godot.Collections.Array`/`Dictionary` (generic and non-generic) and all packed array types
- Resource serialization: saved/imported assets are restored by path through `ResourceLoader`;
  runtime-created resources are recreated inline through Godot's own ClassDB property system
  (driven by a property registry generated from the GDExtension API — 620 engine classes)
- `SerializedNode`/`SerializedResource` base classes and `GodotSerializationUtility` for
  extending Godot object serialization, including optional engine property capture
- GodotObject references (nodes, scene objects) via external index references
- NUnit test suite plus an in-engine integration test project

## Getting started in a Godot project

Godot's C# script system does not support script classes inheriting GodotObject types that
live in referenced assemblies ([godot#111881](https://github.com/godotengine/godot/issues/111881)),
so the two base classes are distributed as source via a Godot addon, while the serialization
runtime ships as a NuGet package. Installation is therefore two steps:

1. Copy [`addons/odin_serializer`](addons/odin_serializer) into your project's `addons/`
   folder and enable the plugin in **Project > Project Settings > Plugins**.
2. Reference the runtime package from your `.csproj`:
   ```xml
   <ItemGroup>
     <PackageReference Include="OdinSerializerForGodot" Version="0.1.0" />
   </ItemGroup>
   ```
   (or a `ProjectReference` to a locally built `OdinSerializer.csproj`).

### Extending Godot object serialization

```csharp
using Godot;
using OdinSerializer;

public partial class Player : SerializedNode
{
    public System.Collections.Generic.Dictionary<string, int> Inventory = new(); // serialized by Odin
    public StandardMaterial3D RuntimeMaterial;                                   // recreated inline
    public Texture2D SavedTexture;                                               // restored by path
    public Node SomeNode;                                                        // external reference

    [Export] public int ExportedNumber { get; set; }                             // serialized by Godot and Odin
}

// Before saving the scene or writing a savegame:
player.Serialize();
// After loading (automatic in _Ready, or call manually):
player.Deserialize();
```

Set `CaptureEngineProperties` (default `true`) to also capture engine ClassDB properties
(transform, visibility, exported members) through the generated engine property registry.

### Serializing regular C# objects

You can also use OdinSerializer as a standalone serialization library via the
`SerializationUtility` class, for example to store data in a file or send it over the network:

```csharp
using OdinSerializer;

public static class Example
{
    public static void Save(MyData data, string filePath)
    {
        byte[] bytes = SerializationUtility.SerializeValue(data, DataFormat.Binary);
        File.WriteAllBytes(bytes, filePath);
    }

    public static MyData Load(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return SerializationUtility.DeserializeValue<MyData>(bytes, DataFormat.Binary);
    }
}
```

References to GodotObjects inside plain object graphs are written as external index
references; ask for the reference list when serializing and pass it back when deserializing:

```csharp
byte[] bytes = SerializationUtility.SerializeValue(data, DataFormat.Binary, out List<GodotObject> godotReferences);
MyData result = SerializationUtility.DeserializeValue<MyData>(bytes, DataFormat.Binary, godotReferences);
```

## Building

The project targets .NET 8 and builds with the regular `dotnet` CLI. The default
`Debug`/`Release` configurations map to the Godot build; Unity builds use the
`DebugUnity`/`ReleaseUnity` configurations.

```bash
dotnet build                          # Godot build (default configuration)
dotnet build -c ReleaseGodot          # Godot build, optimized
dotnet build -c ReleaseUnity          # Unity build
dotnet test OdinSerializer.Tests      # NUnit suite
dotnet pack -c ReleaseGodot           # produce the OdinSerializerForGodot NuGet package
```

### Regenerating the engine serialization data

The ClassDB property registry for engine types is generated from Godot's GDExtension API
dump by `Tools/EngineSerializationGenerator/generate.py`:

```bash
python generate.py --godot "path/to/Godot.exe"      # dump from a local editor, then generate
python generate.py --download godot-4.5-stable      # download the dump bundled with godot-cpp
python generate.py --input extension_api.json       # use an existing dump file
```

### Godot integration tests

An in-engine test project lives in a sibling directory
(`../OdinSerializerGodotTests`); it runs its suite headless and exits non-zero on failure:

```bash
dotnet build
godot --headless --path . --import   # first time only
godot --headless --path .
```

## Technical overview

A brief overview of the working principles of OdinSerializer.

### "Stack-only", forward-only

OdinSerializer is a forward-only serializer: it writes data immediately as it inspects the
object graph, and recreates the graph immediately as it parses. There is no intermediate
"meta-graph" data structure, so after warmup there are often literally zero superfluous GC
allocations, depending on the data format used.

### Data writers and readers

Data writers and readers implement `IDataReader`/`IDataWriter` and abstract strongly typed
C# primitives from the raw data format. OdinSerializer ships with readers and writers for
three formats: Json, Binary and Nodes (note: the Nodes format is Unity-editor specific and
falls back to Binary in Godot builds).

### Serializers, formatters and policies

Serializers are the hardcoded outward face of the system: one per atomic primitive, plus a
catch-all `ComplexTypeSerializer` that wraps formatters. Formatters translate a C# object
into the primitives it consists of and are the primary point of extension — declare one with
`[assembly: RegisterFormatter(typeof(MyFormatter))]` and it is picked up automatically.
Types without a custom formatter are serialized using an on-demand emitted formatter, or a
reflection-based fallback where emitting is unavailable; both use the serialization policy
set on the context (see `SerializationPolicies`) to select members.

### External references

External references let serialized data point at objects not stored in the data itself —
resolved by index, guid or string through the `IExternalIndexReferenceResolver`,
`IExternalGuidReferenceResolver` and `IExternalStringReferenceResolver` interfaces. In the
Godot integration, `EngineReferenceResolver` turns every encountered GodotObject (except
Resources, which have their own formatter) into an external index reference.

### How OdinSerializer works in Godot

The Godot integration mirrors the upstream Unity one:

- `GodotSerializationUtility` serializes/deserializes a Godot object's Odin-serializable
  members (everything Godot itself does not serialize), and optionally its engine ClassDB
  properties, into a `SerializationData` struct.
- `SerializedNode`/`SerializedResource` are convenience base classes using it; they persist
  the data in `[Export]`ed storage. They ship as source in the addon (see above).
- `ResourceFormatter` handles all `Resource`-derived values: those with a `ResourcePath` are
  written as a path and restored through `ResourceLoader` (engine cache preserves identity);
  runtime resources are recreated inline from their ClassDB storage properties, enumerated
  with the generated `EngineSerializationData` registry.

Note that AOT platform support (formatter pregeneration for exports such as iOS) has not yet
been ported from upstream.

## How to contribute

Contributions are taken under the Apache 2.0 license — feel free to submit pull requests.
Please follow the pre-existing coding style, and keep the Unity build compiling
(`dotnet build -c ReleaseUnity`) when changing shared code.

## Upstream

This project is forked from [sirenix/odin-serializer](https://github.com/sirenix/odin-serializer).
The original README, Unity-specific documentation and benchmarks can be found there.
All upstream code remains under the Apache 2.0 license; Godot support changes by this fork
are released under the same license.
