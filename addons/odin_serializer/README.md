# Odin Serializer for Godot — addon

This addon carries the `SerializedNode` and `SerializedResource` base classes as
**source files**. They must be compiled into your game's main assembly: Godot's C#
script system does not support script classes inheriting GodotObject types that live
in referenced assemblies (see godot#111881), so these classes cannot ship inside the
OdinSerializer NuGet package itself.

## Installation

1. Copy the `addons/odin_serializer` folder into your Godot project (so it sits at
   `res://addons/odin_serializer/`).
2. Reference the serializer runtime from your project's `.csproj`:
   ```xml
   <ItemGroup>
     <PackageReference Include="OdinSerializerForGodot" Version="0.1.0" />
   </ItemGroup>
   ```
   (or a `ProjectReference`/`Reference` to a locally built `OdinSerializer.dll`).
3. Enable the plugin in **Project > Project Settings > Plugins** (optional but
   recommended — it initializes the serialization system in the editor).

## Usage

Derive from `SerializedNode` or `SerializedResource` and mark members you want Odin to
serialize (anything Godot itself does not serialize, e.g. dictionaries, polymorphic
fields, delegates):

```csharp
using OdinSerializer;

public partial class Player : SerializedNode
{
    public System.Collections.Generic.Dictionary<string, int> Inventory = new();
}

// Before saving (editor tool button, or when writing a savegame):
player.Serialize();
// After loading (automatic in _Ready, or call manually):
player.Deserialize();
```

- `CaptureEngineProperties` (default `true`) also captures engine ClassDB properties
  (transform, visibility, exported members) through the generated engine property registry.
- Resources are handled by path (restored via `ResourceLoader`) or inline data
  (recreated through the ClassDB property system) automatically.
