#if TOOLS
using Godot;

namespace OdinSerializer
{
    /// <summary>
    /// Editor plugin entry for the Odin Serializer addon. It carries the
    /// <see cref="SerializedNode"/>/<see cref="SerializedResource"/> base classes and
    /// initializes the serialization system in the editor when enabled.
    /// <para />
    /// The serialization runtime itself lives in the OdinSerializerForGodot NuGet package,
    /// which projects using this addon must also reference.
    /// </summary>
    [Tool]
    public partial class OdinSerializerPlugin : EditorPlugin
    {
        public override void _EnterTree()
        {
            GodotSerializationInitializer.Initialize();
        }
    }
}
#endif
