#if GODOT
namespace OdinSerializer.Utilities.Wrapper;

/// <summary>
/// Godot-side stand-in for Unity's SerializeField attribute. Marks a field for serialization
/// by Odin's default policies. Only compiled in Godot builds to avoid clashing with
/// UnityEngine.SerializeField in Unity builds.
/// </summary>
public class SerializeField : Attribute
{

}
#endif