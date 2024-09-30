//-----------------------------------------------------------------------
// <copyright file="ResourceFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatterLocator(typeof(ResourceFormatterLocator), 0)]

namespace OdinSerializer
{
    using System;
    using System.Reflection;
    using Utilities;
    using Utilities.Wrapper;

    /// <summary>
    /// Locates <see cref="ResourceFormatter{T}"/> for all Godot <see cref="Resource"/>-derived types.
    /// </summary>
    internal class ResourceFormatterLocator : IFormatterLocator
    {
        public bool TryGetFormatter(Type type, FormatterLocationStep step, ISerializationPolicy policy, bool allowWeakFallbackFormatters, out IFormatter formatter)
        {
            if (!typeof(Resource).IsAssignableFrom(type))
            {
                formatter = null;
                return false;
            }

            formatter = (IFormatter)Activator.CreateInstance(typeof(ResourceFormatter<>).MakeGenericType(type));
            return true;
        }
    }

    /// <summary>
    /// Formatter for Godot <see cref="Resource"/>-derived types (Texture2D, Material, Mesh, custom resources, etc.).
    /// <para />
    /// Resources that have a <see cref="Resource.ResourcePath"/> are serialized as their path only.
    /// On deserialization the path is resolved through <see cref="GodotResourceLoader"/>:
    /// engine paths (res://, uid://) and .tres/.res files go through <see cref="ResourceLoader"/>
    /// (engine cache preserves reference identity), while external media files (images, audio,
    /// fonts — for example in mod directories) are loaded at runtime through the engine's
    /// dedicated external loading interfaces.
    /// <para />
    /// Resources without a path (created at runtime) are serialized inline and recreated on deserialization.
    /// Inline serialization captures two kinds of state:
    /// <para />1) Engine-visible ClassDB properties via <see cref="EngineObjectInlineSerializer"/>, driven by
    /// property lists generated from the GDExtension API (see <see cref="EngineSerializationData"/>) and
    /// filtered by <see cref="PropertyUsageFlags.Storage"/>. This is the same mechanism Godot itself uses
    /// for .tres files, so it covers both engine-internal types (whose C# wrapper classes carry no
    /// [Export] markers) and script [Export] members.
    /// <para />2) Odin-only managed members that the engine property system does not see (non-exported fields
    /// and properties, such as dictionaries and polymorphic values).
    /// <para />
    /// Note that engine state which is not exposed as ClassDB properties cannot be captured, and that the
    /// inline path requires the Godot native runtime.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    public class ResourceFormatter<T> : MinimalBaseFormatter<T> where T : Resource
    {
        private static readonly Serializer<string> StringSerializer = Serializer.Get<string>();

        /// <summary>
        /// The member selection policy used for Odin-only managed members that the engine
        /// property system does not see: non-exported members marked with [OdinSerialize]
        /// or [SerializeField], plus non-exported public fields.
        /// </summary>
        private static readonly ISerializationPolicy ResourcePolicy = new CustomSerializationPolicy(
            "OdinSerializerPolicies.GodotResource",
            true,
            member =>
            {
                if (member is PropertyInfo property)
                {
                    if (property.GetGetMethod(true) == null || property.GetSetMethod(true) == null)
                    {
                        return false;
                    }
                }
                else if (!(member is FieldInfo))
                {
                    return false;
                }

                if (member.IsDefined<NonSerializedAttribute>(true) && !member.IsDefined<OdinSerializeAttribute>(true))
                {
                    return false;
                }

                // [Export] members are covered by the ClassDB property path below
                if (member.IsDefined<ExportAttribute>(true))
                {
                    return false;
                }

                if (member.IsDefined<OdinSerializeAttribute>(true))
                {
                    return true;
                }

                if (member is FieldInfo field && field.IsPublic)
                {
                    return true;
                }

                return member.IsDefined<SerializeField>(false);
            });

        /// <summary>
        /// Returns null; the instance is created during <see cref="Read"/> (either loaded from
        /// its path or recreated from inline data).
        /// </summary>
        /// <returns>null.</returns>
        protected override T GetUninitializedObject()
        {
            return null;
        }

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref T value, IDataReader reader)
        {
            bool hasPath;
            reader.ReadBoolean(out hasPath);

            if (hasPath)
            {
                string path = StringSerializer.ReadValue(reader);
                value = GodotResourceLoader.Load<T>(path);

                if (value == null)
                {
                    reader.Context.Config.DebugContext.LogWarning("Failed to load Godot resource of type '" + typeof(T).GetNiceName() + "' at path '" + path + "'.");
                }
                else
                {
                    this.RegisterReferenceID(value, reader);
                }

                return;
            }

            try
            {
                value = Activator.CreateInstance<T>();
            }
            catch (Exception ex)
            {
                reader.Context.Config.DebugContext.LogException(new Exception("Failed to create an instance of resource type '" + typeof(T).GetNiceName() + "' for inline deserialization.", ex));
                return;
            }

            this.RegisterReferenceID(value, reader);
            DeserializeInlineData(value, reader);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref T value, IDataWriter writer)
        {
            string path = value.ResourcePath;

            if (!string.IsNullOrEmpty(path))
            {
                writer.WriteBoolean(null, true);
                StringSerializer.WriteValue(path, writer);
                return;
            }

            writer.WriteBoolean(null, false);
            EngineObjectInlineSerializer.SerializeEngineProperties(value, writer);
            SerializeManagedMembers(value, writer);
        }

        /// <summary>
        /// Serializes Odin-only managed members that the engine property system does not see.
        /// </summary>
        private static void SerializeManagedMembers(T value, IDataWriter writer)
        {
            var members = FormatterUtilities.GetSerializableMembers(value.GetType(), ResourcePolicy);
            object instance = value;

            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                var getter = GodotSerializationUtility.GetCachedGodotMemberGetter(member);

                if (getter == null)
                {
                    continue;
                }

                object memberValue = getter(ref instance);
                Serializer serializer = Serializer.Get(FormatterUtilities.GetContainedType(member));

                try
                {
                    serializer.WriteValueWeak(member.Name, memberValue, writer);
                }
                catch (Exception ex)
                {
                    writer.Context.Config.DebugContext.LogException(ex);
                }
            }
        }

        private void DeserializeInlineData(T value, IDataReader reader)
        {
            var members = FormatterUtilities.GetSerializableMembersMap(value.GetType(), ResourcePolicy);
            object instance = value;

            int count = 0;
            string name;
            EntryType entryType;

            while ((entryType = reader.PeekEntry(out name)) != EntryType.EndOfNode && entryType != EntryType.EndOfArray && entryType != EntryType.EndOfStream)
            {
                if (EngineObjectInlineSerializer.TryDeserializePropertyEntry(value, name, reader))
                {
                    // Engine property entry consumed
                }
                else
                {
                    MemberInfo member;
                    WeakValueSetter setter;

                    if (entryType == EntryType.Invalid || string.IsNullOrEmpty(name) || members.TryGetValue(name, out member) == false || (setter = GodotSerializationUtility.GetCachedGodotMemberSetter(member)) == null)
                    {
                        reader.SkipEntry();
                        continue;
                    }

                    try
                    {
                        object memberValue = Serializer.Get(FormatterUtilities.GetContainedType(member)).ReadValueWeak(reader);
                        setter(ref instance, memberValue);
                    }
                    catch (Exception ex)
                    {
                        reader.Context.Config.DebugContext.LogException(ex);
                    }
                }

                count++;

                if (count > 1000)
                {
                    reader.Context.Config.DebugContext.LogError("Breaking out of infinite reading loop! (Read more than a thousand entries for one type!)");
                    break;
                }
            }
        }
    }
}
