//-----------------------------------------------------------------------
// <copyright file="VariantFormatter.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using Godot;
using OdinSerializer;

[assembly: RegisterFormatter(typeof(VariantFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Variant"/> type. Writes the concrete
    /// <see cref="Variant.Type"/> tag, then dispatches to the serializer of the concrete
    /// contained type. Godot object values are written as external references and require
    /// an external index reference resolver in the serialization context (as set up by
    /// <see cref="SerializationUtility"/> and <see cref="GodotSerializationUtility"/>).
    /// <para />
    /// Note that <see cref="Variant"/> requires the Godot native runtime; this formatter
    /// can only be used inside a Godot process.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Variant}" />
    public class VariantFormatter : MinimalBaseFormatter<Variant>
    {
        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Variant value, IDataReader reader)
        {
            int typeInt;
            reader.ReadInt32(out typeInt);
            var type = (Variant.Type)typeInt;

            switch (type)
            {
                case Variant.Type.Nil: value = default; break;
                case Variant.Type.Bool: value = Variant.From(Serializer.Get<bool>().ReadValue(reader)); break;
                case Variant.Type.Int: value = Variant.From(Serializer.Get<long>().ReadValue(reader)); break;
                case Variant.Type.Float: value = Variant.From(Serializer.Get<double>().ReadValue(reader)); break;
                case Variant.Type.String: value = Variant.From(Serializer.Get<string>().ReadValue(reader)); break;
                case Variant.Type.Vector2: value = Variant.From(Serializer.Get<Vector2>().ReadValue(reader)); break;
                case Variant.Type.Vector2I: value = Variant.From(Serializer.Get<Vector2I>().ReadValue(reader)); break;
                case Variant.Type.Rect2: value = Variant.From(Serializer.Get<Rect2>().ReadValue(reader)); break;
                case Variant.Type.Rect2I: value = Variant.From(Serializer.Get<Rect2I>().ReadValue(reader)); break;
                case Variant.Type.Vector3: value = Variant.From(Serializer.Get<Vector3>().ReadValue(reader)); break;
                case Variant.Type.Vector3I: value = Variant.From(Serializer.Get<Vector3I>().ReadValue(reader)); break;
                case Variant.Type.Transform2D: value = Variant.From(Serializer.Get<Transform2D>().ReadValue(reader)); break;
                case Variant.Type.Vector4: value = Variant.From(Serializer.Get<Vector4>().ReadValue(reader)); break;
                case Variant.Type.Vector4I: value = Variant.From(Serializer.Get<Vector4I>().ReadValue(reader)); break;
                case Variant.Type.Plane: value = Variant.From(Serializer.Get<Plane>().ReadValue(reader)); break;
                case Variant.Type.Quaternion: value = Variant.From(Serializer.Get<Quaternion>().ReadValue(reader)); break;
                case Variant.Type.Aabb: value = Variant.From(Serializer.Get<Aabb>().ReadValue(reader)); break;
                case Variant.Type.Basis: value = Variant.From(Serializer.Get<Basis>().ReadValue(reader)); break;
                case Variant.Type.Transform3D: value = Variant.From(Serializer.Get<Transform3D>().ReadValue(reader)); break;
                case Variant.Type.Projection: value = Variant.From(Serializer.Get<Projection>().ReadValue(reader)); break;
                case Variant.Type.Color: value = Variant.From(Serializer.Get<Color>().ReadValue(reader)); break;
                case Variant.Type.StringName: value = Variant.From(Serializer.Get<StringName>().ReadValue(reader)); break;
                case Variant.Type.NodePath: value = Variant.From(Serializer.Get<NodePath>().ReadValue(reader)); break;
                case Variant.Type.Rid:
                    // RIDs are session-scoped handles and GodotSharp 4.2 exposes no public API to
                    // reconstruct one from its ID, so they cannot be meaningfully deserialized.
                    reader.Context.Config.DebugContext.LogWarning("Variant of type Rid cannot be deserialized (RIDs are session-scoped); deserializing as nil.");
                    value = default;
                    break;
                case Variant.Type.Object: value = Variant.From(Serializer.Get<GodotObject>().ReadValue(reader)); break;
                case Variant.Type.Callable: value = Variant.From(Serializer.Get<Callable>().ReadValue(reader)); break;
                case Variant.Type.Signal: value = Variant.From(Serializer.Get<Signal>().ReadValue(reader)); break;
                case Variant.Type.Dictionary: value = Variant.From(Serializer.Get<Godot.Collections.Dictionary>().ReadValue(reader)); break;
                case Variant.Type.Array: value = Variant.From(Serializer.Get<Godot.Collections.Array>().ReadValue(reader)); break;
                case Variant.Type.PackedByteArray: value = Variant.From(Serializer.Get<byte[]>().ReadValue(reader)); break;
                case Variant.Type.PackedInt32Array: value = Variant.From(Serializer.Get<int[]>().ReadValue(reader)); break;
                case Variant.Type.PackedInt64Array: value = Variant.From(Serializer.Get<long[]>().ReadValue(reader)); break;
                case Variant.Type.PackedFloat32Array: value = Variant.From(Serializer.Get<float[]>().ReadValue(reader)); break;
                case Variant.Type.PackedFloat64Array: value = Variant.From(Serializer.Get<double[]>().ReadValue(reader)); break;
                case Variant.Type.PackedStringArray: value = Variant.From(Serializer.Get<string[]>().ReadValue(reader)); break;
                case Variant.Type.PackedVector2Array: value = Variant.From(Serializer.Get<Vector2[]>().ReadValue(reader)); break;
                case Variant.Type.PackedVector3Array: value = Variant.From(Serializer.Get<Vector3[]>().ReadValue(reader)); break;
                case Variant.Type.PackedColorArray: value = Variant.From(Serializer.Get<Color[]>().ReadValue(reader)); break;
                case Variant.Type.PackedVector4Array: value = Variant.From(Serializer.Get<Vector4[]>().ReadValue(reader)); break;
                default:
                    reader.Context.Config.DebugContext.LogWarning("Unsupported Variant type '" + type + "'; deserializing as nil.");
                    value = default;
                    break;
            }
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Variant value, IDataWriter writer)
        {
            var type = value.VariantType;
            writer.WriteInt32(null, (int)type);

            switch (type)
            {
                case Variant.Type.Nil: break;
                case Variant.Type.Bool: Serializer.Get<bool>().WriteValue(value.AsBool(), writer); break;
                case Variant.Type.Int: Serializer.Get<long>().WriteValue(value.AsInt64(), writer); break;
                case Variant.Type.Float: Serializer.Get<double>().WriteValue(value.AsDouble(), writer); break;
                case Variant.Type.String: Serializer.Get<string>().WriteValue(value.AsString(), writer); break;
                case Variant.Type.Vector2: Serializer.Get<Vector2>().WriteValue(value.AsVector2(), writer); break;
                case Variant.Type.Vector2I: Serializer.Get<Vector2I>().WriteValue(value.AsVector2I(), writer); break;
                case Variant.Type.Rect2: Serializer.Get<Rect2>().WriteValue(value.AsRect2(), writer); break;
                case Variant.Type.Rect2I: Serializer.Get<Rect2I>().WriteValue(value.AsRect2I(), writer); break;
                case Variant.Type.Vector3: Serializer.Get<Vector3>().WriteValue(value.AsVector3(), writer); break;
                case Variant.Type.Vector3I: Serializer.Get<Vector3I>().WriteValue(value.AsVector3I(), writer); break;
                case Variant.Type.Transform2D: Serializer.Get<Transform2D>().WriteValue(value.AsTransform2D(), writer); break;
                case Variant.Type.Vector4: Serializer.Get<Vector4>().WriteValue(value.AsVector4(), writer); break;
                case Variant.Type.Vector4I: Serializer.Get<Vector4I>().WriteValue(value.AsVector4I(), writer); break;
                case Variant.Type.Plane: Serializer.Get<Plane>().WriteValue(value.AsPlane(), writer); break;
                case Variant.Type.Quaternion: Serializer.Get<Quaternion>().WriteValue(value.AsQuaternion(), writer); break;
                case Variant.Type.Aabb: Serializer.Get<Aabb>().WriteValue(value.AsAabb(), writer); break;
                case Variant.Type.Basis: Serializer.Get<Basis>().WriteValue(value.AsBasis(), writer); break;
                case Variant.Type.Transform3D: Serializer.Get<Transform3D>().WriteValue(value.AsTransform3D(), writer); break;
                case Variant.Type.Projection: Serializer.Get<Projection>().WriteValue(value.AsProjection(), writer); break;
                case Variant.Type.Color: Serializer.Get<Color>().WriteValue(value.AsColor(), writer); break;
                case Variant.Type.StringName: Serializer.Get<StringName>().WriteValue(value.AsStringName(), writer); break;
                case Variant.Type.NodePath: Serializer.Get<NodePath>().WriteValue(value.AsNodePath(), writer); break;
                case Variant.Type.Rid:
                    writer.Context.Config.DebugContext.LogWarning("Variant of type Rid cannot be serialized (RIDs are session-scoped); serializing as nil.");
                    break;
                case Variant.Type.Object: Serializer.Get<GodotObject>().WriteValue(value.AsGodotObject(), writer); break;
                case Variant.Type.Callable: Serializer.Get<Callable>().WriteValue(value.AsCallable(), writer); break;
                case Variant.Type.Signal: Serializer.Get<Signal>().WriteValue(value.AsSignal(), writer); break;
                case Variant.Type.Dictionary: Serializer.Get<Godot.Collections.Dictionary>().WriteValue(value.AsGodotDictionary(), writer); break;
                case Variant.Type.Array: Serializer.Get<Godot.Collections.Array>().WriteValue(value.AsGodotArray(), writer); break;
                case Variant.Type.PackedByteArray: Serializer.Get<byte[]>().WriteValue(value.AsByteArray(), writer); break;
                case Variant.Type.PackedInt32Array: Serializer.Get<int[]>().WriteValue(value.AsInt32Array(), writer); break;
                case Variant.Type.PackedInt64Array: Serializer.Get<long[]>().WriteValue(value.AsInt64Array(), writer); break;
                case Variant.Type.PackedFloat32Array: Serializer.Get<float[]>().WriteValue(value.AsFloat32Array(), writer); break;
                case Variant.Type.PackedFloat64Array: Serializer.Get<double[]>().WriteValue(value.AsFloat64Array(), writer); break;
                case Variant.Type.PackedStringArray: Serializer.Get<string[]>().WriteValue(value.AsStringArray(), writer); break;
                case Variant.Type.PackedVector2Array: Serializer.Get<Vector2[]>().WriteValue(value.AsVector2Array(), writer); break;
                case Variant.Type.PackedVector3Array: Serializer.Get<Vector3[]>().WriteValue(value.AsVector3Array(), writer); break;
                case Variant.Type.PackedColorArray: Serializer.Get<Color[]>().WriteValue(value.AsColorArray(), writer); break;
                case Variant.Type.PackedVector4Array: Serializer.Get<Vector4[]>().WriteValue(value.AsVector4Array(), writer); break;
                default:
                    writer.Context.Config.DebugContext.LogWarning("Unsupported Variant type '" + type + "'; serializing as nil.");
                    break;
            }
        }
    }
}
