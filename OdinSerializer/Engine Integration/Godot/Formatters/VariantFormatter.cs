using Godot;

namespace OdinSerializer;

public class VariantFormatter : BaseFormatter<Variant>
{
    protected override void DeserializeImplementation(ref Variant value, IDataReader reader)
    {
        throw new NotImplementedException();
    }

    protected override void SerializeImplementation(ref Variant value, IDataWriter writer)
    {
        throw new NotImplementedException();
    }
}