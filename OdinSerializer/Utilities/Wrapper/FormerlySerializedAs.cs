namespace OdinSerializer.Utilities.Wrapper;
#if GODOT
public class FormerlySerializedAsAttribute(string name) : Attribute
{
    public string oldName = name;
}
#endif