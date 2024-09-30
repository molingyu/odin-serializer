namespace OdinSerializer.Utilities.Wrapper;

# if GODOT
public interface ISerializationCallbackReceiver
{
    public void OnAfterDeserialize();
    public void OnBeforeSerialize();
}
#endif