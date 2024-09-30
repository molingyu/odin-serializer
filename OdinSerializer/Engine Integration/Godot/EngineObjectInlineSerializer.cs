//-----------------------------------------------------------------------
// <copyright file="EngineObjectInlineSerializer.cs" company="Sirenix IVS">
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

namespace OdinSerializer
{
    using System;

    /// <summary>
    /// Serializes and deserializes the engine-visible (ClassDB) properties of Godot objects
    /// as Variants, using the same property metadata Godot itself uses for .tres/.tscn files
    /// (see <see cref="EngineSerializationData"/>). This gives engine-internal types (whose
    /// C# wrapper classes carry no [Export] markers) the same serialization fidelity the
    /// engine has. Requires the Godot native runtime.
    /// </summary>
    public static class EngineObjectInlineSerializer
    {
        /// <summary>
        /// The entry name prefix used for engine property entries, distinguishing them
        /// from managed member entries in the serialized stream.
        /// </summary>
        public const string PropertyEntryPrefix = "prop:";

        private static readonly Serializer<Variant> VariantSerializer = Serializer.Get<Variant>();

        /// <summary>
        /// Writes all storage properties of the given object as named Variant entries.
        /// </summary>
        /// <param name="value">The object whose engine properties to serialize.</param>
        /// <param name="writer">The writer to serialize with.</param>
        public static void SerializeEngineProperties(GodotObject value, IDataWriter writer)
        {
            foreach (var name in EngineSerializationData.GetStorageProperties(value))
            {
                try
                {
                    VariantSerializer.WriteValue(PropertyEntryPrefix + name, value.Get(name), writer);
                }
                catch (Exception ex)
                {
                    writer.Context.Config.DebugContext.LogException(ex);
                }
            }
        }

        /// <summary>
        /// If the given entry name is an engine property entry, reads the Variant value and
        /// assigns it to the object's property.
        /// </summary>
        /// <param name="value">The object to assign the property on.</param>
        /// <param name="entryName">The name of the current entry.</param>
        /// <param name="reader">The reader to deserialize with.</param>
        /// <returns><c>true</c> if the entry was an engine property entry and was consumed.</returns>
        public static bool TryDeserializePropertyEntry(GodotObject value, string entryName, IDataReader reader)
        {
            if (string.IsNullOrEmpty(entryName) || !entryName.StartsWith(PropertyEntryPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                Variant propValue = VariantSerializer.ReadValue(reader);
                value.Set(entryName.Substring(PropertyEntryPrefix.Length), propValue);
            }
            catch (Exception ex)
            {
                reader.Context.Config.DebugContext.LogException(ex);
            }

            return true;
        }
    }
}
