//-----------------------------------------------------------------------
// <copyright file="GodotSerializationUtility.cs" company="Sirenix IVS">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Utilities;
    using Utilities.Wrapper;

    /// <summary>
    /// Provides utility methods for serializing and deserializing Godot objects with Odin Serializer.
    /// <para />
    /// This is the Godot counterpart of Unity's UnitySerializationUtility. It serializes all members of a
    /// Godot object that Godot itself does not serialize (IE, members not marked with <see cref="ExportAttribute"/>),
    /// such as dictionaries, polymorphic values, delegates and cyclic references.
    /// <para />
    /// Note that the <see cref="DataFormat.Nodes"/> format is not supported, as Godot provides no persistence
    /// mechanism for node lists; it always falls back to <see cref="DataFormat.Binary"/>.
    /// </summary>
    public static class GodotSerializationUtility
    {
        private static readonly Dictionary<MemberInfo, WeakValueGetter> GodotMemberGetters = new Dictionary<MemberInfo, WeakValueGetter>(ReferenceEqualityComparer<MemberInfo>.Default);
        private static readonly Dictionary<MemberInfo, WeakValueSetter> GodotMemberSetters = new Dictionary<MemberInfo, WeakValueSetter>(ReferenceEqualityComparer<MemberInfo>.Default);

        private static readonly ISerializationPolicy DefaultPolicy = SerializationPolicies.Unity;
        private static readonly ISerializationPolicy EverythingPolicy = SerializationPolicies.Everything;
        private static readonly ISerializationPolicy StrictPolicy = SerializationPolicies.Strict;

        private static readonly Dictionary<MemberInfo, CachedSerializationBackendResult> OdinWillSerializeCache_DefaultPolicy = new Dictionary<MemberInfo, CachedSerializationBackendResult>(ReferenceEqualityComparer<MemberInfo>.Default);
        private static readonly Dictionary<MemberInfo, CachedSerializationBackendResult> OdinWillSerializeCache_EverythingPolicy = new Dictionary<MemberInfo, CachedSerializationBackendResult>(ReferenceEqualityComparer<MemberInfo>.Default);
        private static readonly Dictionary<MemberInfo, CachedSerializationBackendResult> OdinWillSerializeCache_StrictPolicy = new Dictionary<MemberInfo, CachedSerializationBackendResult>(ReferenceEqualityComparer<MemberInfo>.Default);
        private static readonly Dictionary<ISerializationPolicy, Dictionary<MemberInfo, CachedSerializationBackendResult>> OdinWillSerializeCache_CustomPolicies = new Dictionary<ISerializationPolicy, Dictionary<MemberInfo, CachedSerializationBackendResult>>(ReferenceEqualityComparer<ISerializationPolicy>.Default);

        private struct CachedSerializationBackendResult
        {
            public bool HasCalculatedSerializeGodotFieldsTrueResult;
            public bool HasCalculatedSerializeGodotFieldsFalseResult;

            public bool SerializeGodotFieldsTrueResult;
            public bool SerializeGodotFieldsFalseResult;
        }

        /// <summary>
        /// Serializes a Godot object with Odin Serializer into a <see cref="SerializationData"/> struct.
        /// </summary>
        /// <param name="godotObject">The Godot object to serialize.</param>
        /// <param name="data">The data struct to serialize into.</param>
        /// <param name="serializeGodotFields">Whether to also serialize members that Godot itself will serialize (IE, members marked with <see cref="ExportAttribute"/>).</param>
        /// <param name="context">The serialization context to use, if any.</param>
        /// <param name="serializeEngineProperties">Whether to also serialize the object's engine-visible ClassDB properties (via <see cref="EngineObjectInlineSerializer"/>), for example node transforms. Requires the Godot native runtime.</param>
        public static void SerializeGodotObject(GodotObject godotObject, ref SerializationData data, bool serializeGodotFields = false, SerializationContext context = null, bool serializeEngineProperties = false)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            // Ensure there is no superfluous data left over after serialization
            // (We will reassign all necessary data.)
            data.Reset();

            DataFormat format;

            // Get the format to serialize as
            {
                if (godotObject is IOverridesSerializationFormat formatOverride)
                {
                    format = formatOverride.GetFormatToSerializeAs(true);
                }
                else if (GlobalSerializationConfig.HasInstanceLoaded)
                {
                    format = GlobalSerializationConfig.Instance.BuildSerializationFormat;
                }
                else
                {
                    format = DataFormat.Binary;
                }
            }

            // Get the policy to serialize with
            if (godotObject is IOverridesSerializationPolicy policyOverride)
            {
                if (context != null)
                {
                    context.Config.SerializationPolicy = policyOverride.SerializationPolicy ?? DefaultPolicy;
                }

                serializeGodotFields = policyOverride.OdinSerializesGodotFields;
            }

            if (format == DataFormat.Nodes)
            {
                DebugWrapper.LogWarning("The serialization format '" + format.ToString() + "' is not supported for Godot objects. Defaulting to the format '" + DataFormat.Binary.ToString() + "' instead.");
                format = DataFormat.Binary;
            }

            SerializeGodotObject(godotObject, ref data.SerializedBytes, ref data.ReferencedGodotObjects, format, serializeGodotFields, context, serializeEngineProperties);
            data.SerializedFormat = format;
        }

        /// <summary>
        /// Deserializes a Godot object with Odin Serializer from a <see cref="SerializationData"/> struct.
        /// </summary>
        /// <param name="godotObject">The Godot object to deserialize.</param>
        /// <param name="data">The data struct to deserialize from.</param>
        /// <param name="context">The deserialization context to use, if any.</param>
        public static void DeserializeGodotObject(GodotObject godotObject, ref SerializationData data, DeserializationContext context = null)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            DataFormat format = data.SerializedFormat;

            if (format == DataFormat.Nodes)
            {
                DebugWrapper.LogWarning("The serialization format '" + format.ToString() + "' is not supported for Godot objects. Defaulting to the format '" + DataFormat.Binary.ToString() + "' instead.");
                format = DataFormat.Binary;
            }

            if (format == DataFormat.JSON)
            {
                DeserializeGodotObject(godotObject, ref data.SerializedBytesString, ref data.ReferencedGodotObjects, format, context);
            }
            else
            {
                DeserializeGodotObject(godotObject, ref data.SerializedBytes, ref data.ReferencedGodotObjects, format, context);
            }
        }

        /// <summary>
        /// Serializes a Godot object with Odin Serializer into a base64 string.
        /// </summary>
        public static void SerializeGodotObject(GodotObject godotObject, ref string base64Bytes, ref List<GodotObject> referencedGodotObjects, DataFormat format, bool serializeGodotFields = false, SerializationContext context = null, bool serializeEngineProperties = false)
        {
            byte[] bytes = null;
            SerializeGodotObject(godotObject, ref bytes, ref referencedGodotObjects, format, serializeGodotFields, context, serializeEngineProperties);
            base64Bytes = Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Serializes a Godot object with Odin Serializer into a byte array.
        /// </summary>
        public static void SerializeGodotObject(GodotObject godotObject, ref byte[] bytes, ref List<GodotObject> referencedGodotObjects, DataFormat format, bool serializeGodotFields = false, SerializationContext context = null, bool serializeEngineProperties = false)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            if (format == DataFormat.Nodes)
            {
                DebugWrapper.LogError("The serialization data format '" + format.ToString() + "' is not supported by this method. You must create your own writer.");
                return;
            }

            if (referencedGodotObjects == null)
            {
                referencedGodotObjects = new List<GodotObject>();
            }
            else
            {
                referencedGodotObjects.Clear();
            }

            using (var stream = Cache<CachedMemoryStream>.Claim())
            using (var resolver = Cache<EngineReferenceResolver>.Claim())
            {
                resolver.Value.SetReferencedEngineObjects(referencedGodotObjects);

                if (context != null)
                {
                    context.IndexReferenceResolver = resolver.Value;
                    using (var writerCache = GetCachedGodotWriter(format, stream.Value.MemoryStream, context))
                    {
                        SerializeGodotObject(godotObject, writerCache.Value as IDataWriter, serializeGodotFields, serializeEngineProperties);
                    }
                }
                else
                {
                    using (var con = Cache<SerializationContext>.Claim())
                    {
                        con.Value.Config.SerializationPolicy = DefaultPolicy;

                        if (GlobalSerializationConfig.HasInstanceLoaded)
                        {
                            con.Value.Config.DebugContext.ErrorHandlingPolicy = GlobalSerializationConfig.Instance.ErrorHandlingPolicy;
                            con.Value.Config.DebugContext.LoggingPolicy = GlobalSerializationConfig.Instance.LoggingPolicy;
                            con.Value.Config.DebugContext.Logger = GlobalSerializationConfig.Instance.Logger;
                        }
                        else
                        {
                            con.Value.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.Resilient;
                            con.Value.Config.DebugContext.LoggingPolicy = LoggingPolicy.LogErrors;
                            con.Value.Config.DebugContext.Logger = DefaultLoggers.UnityLogger;
                        }

                        con.Value.IndexReferenceResolver = resolver.Value;

                        using (var writerCache = GetCachedGodotWriter(format, stream.Value.MemoryStream, con))
                        {
                            SerializeGodotObject(godotObject, writerCache.Value as IDataWriter, serializeGodotFields, serializeEngineProperties);
                        }
                    }
                }

                bytes = stream.Value.MemoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes a Godot object with Odin Serializer using the given writer.
        /// </summary>
        /// <param name="godotObject">The Godot object to serialize.</param>
        /// <param name="writer">The writer to serialize with.</param>
        /// <param name="serializeGodotFields">Whether to also serialize members that Godot itself will serialize.</param>
        /// <param name="serializeEngineProperties">Whether to also serialize the object's engine-visible ClassDB properties. Requires the Godot native runtime.</param>
        public static void SerializeGodotObject(GodotObject godotObject, IDataWriter writer, bool serializeGodotFields = false, bool serializeEngineProperties = false)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            try
            {
                writer.PrepareNewSerializationSession();

                var members = FormatterUtilities.GetSerializableMembers(godotObject.GetType(), writer.Context.Config.SerializationPolicy);
                object godotObjectInstance = godotObject;

                for (int i = 0; i < members.Length; i++)
                {
                    var member = members[i];
                    WeakValueGetter getter = null;

                    if (!OdinWillSerialize(member, serializeGodotFields, writer.Context.Config.SerializationPolicy) || (getter = GetCachedGodotMemberGetter(member)) == null)
                    {
                        continue;
                    }

                    var value = getter(ref godotObjectInstance);

                    bool isNull = object.ReferenceEquals(value, null);

                    // Never serialize serialization data. That way lies madness.
                    if (!isNull && value.GetType() == typeof(SerializationData))
                    {
                        continue;
                    }

                    Serializer serializer = Serializer.Get(FormatterUtilities.GetContainedType(member));

                    try
                    {
                        serializer.WriteValueWeak(member.Name, value, writer);
                    }
                    catch (Exception ex)
                    {
                        writer.Context.Config.DebugContext.LogException(ex);
                    }
                }

                if (serializeEngineProperties)
                {
                    EngineObjectInlineSerializer.SerializeEngineProperties(godotObject, writer);
                }

                writer.FlushToStream();
            }
            catch (SerializationAbortException ex)
            {
                throw new SerializationAbortException("Serialization of type '" + godotObject.GetType().GetNiceFullName() + "' aborted.", ex);
            }
            catch (Exception ex)
            {
                DebugWrapper.LogException(new Exception("Exception thrown while serializing type '" + godotObject.GetType().GetNiceFullName() + "': " + ex.Message, ex));
            }
        }

        /// <summary>
        /// Deserializes a Godot object with Odin Serializer from a base64 string.
        /// </summary>
        public static void DeserializeGodotObject(GodotObject godotObject, ref string base64Bytes, ref List<GodotObject> referencedGodotObjects, DataFormat format, DeserializationContext context = null)
        {
            if (base64Bytes == null)
            {
                throw new ArgumentNullException(nameof(base64Bytes));
            }

            byte[] bytes = Convert.FromBase64String(base64Bytes);
            DeserializeGodotObject(godotObject, ref bytes, ref referencedGodotObjects, format, context);
        }

        /// <summary>
        /// Deserializes a Godot object with Odin Serializer from a byte array.
        /// </summary>
        public static void DeserializeGodotObject(GodotObject godotObject, ref byte[] bytes, ref List<GodotObject> referencedGodotObjects, DataFormat format, DeserializationContext context = null)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (format == DataFormat.Nodes)
            {
                DebugWrapper.LogError("The serialization data format '" + format.ToString() + "' is not supported by this method. You must create your own reader.");
                return;
            }

            using (var stream = Cache<CachedMemoryStream>.Claim())
            using (var resolver = Cache<EngineReferenceResolver>.Claim())
            {
                resolver.Value.SetReferencedEngineObjects(referencedGodotObjects);

                stream.Value.MemoryStream.Write(bytes, 0, bytes.Length);
                stream.Value.MemoryStream.Position = 0;

                if (context != null)
                {
                    context.IndexReferenceResolver = resolver.Value;
                    using (var readerCache = GetCachedGodotReader(format, stream.Value.MemoryStream, context))
                    {
                        DeserializeGodotObject(godotObject, readerCache.Value as IDataReader);
                    }
                }
                else
                {
                    using (var con = Cache<DeserializationContext>.Claim())
                    {
                        con.Value.Config.SerializationPolicy = DefaultPolicy;

                        if (GlobalSerializationConfig.HasInstanceLoaded)
                        {
                            con.Value.Config.DebugContext.ErrorHandlingPolicy = GlobalSerializationConfig.Instance.ErrorHandlingPolicy;
                            con.Value.Config.DebugContext.LoggingPolicy = GlobalSerializationConfig.Instance.LoggingPolicy;
                            con.Value.Config.DebugContext.Logger = GlobalSerializationConfig.Instance.Logger;
                        }
                        else
                        {
                            con.Value.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.Resilient;
                            con.Value.Config.DebugContext.LoggingPolicy = LoggingPolicy.LogErrors;
                            con.Value.Config.DebugContext.Logger = DefaultLoggers.UnityLogger;
                        }

                        con.Value.IndexReferenceResolver = resolver.Value;

                        using (var readerCache = GetCachedGodotReader(format, stream.Value.MemoryStream, con))
                        {
                            DeserializeGodotObject(godotObject, readerCache.Value as IDataReader);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes a Godot object with Odin Serializer using the given reader.
        /// </summary>
        public static void DeserializeGodotObject(GodotObject godotObject, IDataReader reader)
        {
            if (godotObject == null)
            {
                throw new ArgumentNullException(nameof(godotObject));
            }

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (godotObject is IOverridesSerializationPolicy policyOverride && policyOverride.SerializationPolicy != null)
            {
                reader.Context.Config.SerializationPolicy = policyOverride.SerializationPolicy;
            }

            try
            {
                reader.PrepareNewSerializationSession();

                var members = FormatterUtilities.GetSerializableMembersMap(godotObject.GetType(), reader.Context.Config.SerializationPolicy);

                int count = 0;
                string name;
                EntryType entryType;
                object godotObjectInstance = godotObject;

                while ((entryType = reader.PeekEntry(out name)) != EntryType.EndOfNode && entryType != EntryType.EndOfArray && entryType != EntryType.EndOfStream)
                {
                    MemberInfo member = null;
                    WeakValueSetter setter = null;

                    bool skip = false;

                    if (entryType == EntryType.Invalid)
                    {
                        reader.Context.Config.DebugContext.LogError("Encountered invalid entry while reading serialization data for Godot object of type '" + godotObject.GetType().GetNiceFullName() + "'. Data dump: " + reader.GetDataDump());
                        skip = true;
                    }
                    else if (string.IsNullOrEmpty(name))
                    {
                        reader.Context.Config.DebugContext.LogError("Entry of type \"" + entryType + "\" in node \"" + reader.CurrentNodeName + "\" is missing a name.");
                        skip = true;
                    }
                    else if (EngineObjectInlineSerializer.TryDeserializePropertyEntry(godotObject, name, reader))
                    {
                        // Engine property entry consumed; nothing else to do
                        continue;
                    }
                    else if (members.TryGetValue(name, out member) == false || (setter = GetCachedGodotMemberSetter(member)) == null)
                    {
                        skip = true;
                    }

                    if (skip)
                    {
                        reader.SkipEntry();
                        continue;
                    }

                    {
                        Type expectedType = FormatterUtilities.GetContainedType(member);
                        Serializer serializer = Serializer.Get(expectedType);

                        try
                        {
                            object value = serializer.ReadValueWeak(reader);
                            setter(ref godotObjectInstance, value);
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
            catch (SerializationAbortException ex)
            {
                throw new SerializationAbortException("Deserialization of type '" + godotObject.GetType().GetNiceFullName() + "' aborted.", ex);
            }
            catch (Exception ex)
            {
                DebugWrapper.LogException(new Exception("Exception thrown while deserializing type '" + godotObject.GetType().GetNiceFullName() + "': " + ex.Message, ex));
            }
        }

        /// <summary>
        /// Checks whether Odin will serialize a given member.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <param name="serializeGodotFields">Whether to allow serialization of members that will also be serialized by Godot.</param>
        /// <param name="policy">The policy that Odin should be using for serialization of the given member. If this parameter is null, it defaults to <see cref="SerializationPolicies.Unity"/>.</param>
        /// <returns>True if Odin will serialize the member, otherwise false.</returns>
        public static bool OdinWillSerialize(MemberInfo member, bool serializeGodotFields, ISerializationPolicy policy = null)
        {
            Dictionary<MemberInfo, CachedSerializationBackendResult> cacheForPolicy;

            if (policy == null || object.ReferenceEquals(policy, DefaultPolicy))
            {
                cacheForPolicy = OdinWillSerializeCache_DefaultPolicy;
            }
            else if (object.ReferenceEquals(policy, EverythingPolicy))
            {
                cacheForPolicy = OdinWillSerializeCache_EverythingPolicy;
            }
            else if (object.ReferenceEquals(policy, StrictPolicy))
            {
                cacheForPolicy = OdinWillSerializeCache_StrictPolicy;
            }
            else
            {
                lock (OdinWillSerializeCache_CustomPolicies)
                {
                    if (!OdinWillSerializeCache_CustomPolicies.TryGetValue(policy, out cacheForPolicy))
                    {
                        cacheForPolicy = new Dictionary<MemberInfo, CachedSerializationBackendResult>(ReferenceEqualityComparer<MemberInfo>.Default);
                        OdinWillSerializeCache_CustomPolicies.Add(policy, cacheForPolicy);
                    }
                }
            }

            CachedSerializationBackendResult result;

            lock (cacheForPolicy)
            {
                if (!cacheForPolicy.TryGetValue(member, out result))
                {
                    result = default(CachedSerializationBackendResult);

                    if (serializeGodotFields)
                    {
                        result.SerializeGodotFieldsTrueResult = CalculateOdinWillSerialize(member, serializeGodotFields, policy ?? DefaultPolicy);
                        result.HasCalculatedSerializeGodotFieldsTrueResult = true;
                    }
                    else
                    {
                        result.SerializeGodotFieldsFalseResult = CalculateOdinWillSerialize(member, serializeGodotFields, policy ?? DefaultPolicy);
                        result.HasCalculatedSerializeGodotFieldsFalseResult = true;
                    }

                    cacheForPolicy.Add(member, result);
                }
                else
                {
                    if (serializeGodotFields && !result.HasCalculatedSerializeGodotFieldsTrueResult)
                    {
                        result.SerializeGodotFieldsTrueResult = CalculateOdinWillSerialize(member, serializeGodotFields, policy ?? DefaultPolicy);
                        result.HasCalculatedSerializeGodotFieldsTrueResult = true;

                        cacheForPolicy[member] = result;
                    }
                    else if (!serializeGodotFields && !result.HasCalculatedSerializeGodotFieldsFalseResult)
                    {
                        result.SerializeGodotFieldsFalseResult = CalculateOdinWillSerialize(member, serializeGodotFields, policy ?? DefaultPolicy);
                        result.HasCalculatedSerializeGodotFieldsFalseResult = true;

                        cacheForPolicy[member] = result;
                    }
                }

                return serializeGodotFields ? result.SerializeGodotFieldsTrueResult : result.SerializeGodotFieldsFalseResult;
            }
        }

        private static bool CalculateOdinWillSerialize(MemberInfo member, bool serializeGodotFields, ISerializationPolicy policy)
        {
            if (member.DeclaringType == typeof(GodotObject)) return false;
            if (!policy.ShouldSerializeMember(member)) return false;

            // Allow serialization of fields with [OdinSerialize], regardless of whether Godot
            // serializes the field or not
            if (member is FieldInfo && member.IsDefined(typeof(OdinSerializeAttribute), true))
            {
                return true;
            }

            // No need to check whether Godot serializes it or not, our answer will always be the same
            if (serializeGodotFields) return true;

            if (GuessIfGodotWillSerialize(member)) return false;

            return true;
        }

        /// <summary>
        /// Guesses whether Godot itself will serialize a given member. Godot only serializes members
        /// marked with <see cref="ExportAttribute"/>.
        /// </summary>
        /// <param name="member">The member to check.</param>
        /// <returns>True if Godot will likely serialize the member, otherwise false.</returns>
        public static bool GuessIfGodotWillSerialize(MemberInfo member)
        {
            return member.IsDefined(typeof(ExportAttribute), true);
        }

        internal static WeakValueGetter GetCachedGodotMemberGetter(MemberInfo member)
        {
            lock (GodotMemberGetters)
            {
                WeakValueGetter result;

                if (GodotMemberGetters.TryGetValue(member, out result) == false)
                {
                    if (member is FieldInfo)
                    {
                        result = EmitUtilities.CreateWeakInstanceFieldGetter(member.DeclaringType, member as FieldInfo);
                    }
                    else if (member is PropertyInfo)
                    {
                        result = EmitUtilities.CreateWeakInstancePropertyGetter(member.DeclaringType, member as PropertyInfo);
                    }
                    else
                    {
                        result = delegate (ref object instance)
                        {
                            return FormatterUtilities.GetMemberValue(member, instance);
                        };
                    }

                    GodotMemberGetters.Add(member, result);
                }

                return result;
            }
        }

        internal static WeakValueSetter GetCachedGodotMemberSetter(MemberInfo member)
        {
            lock (GodotMemberSetters)
            {
                WeakValueSetter result;

                if (GodotMemberSetters.TryGetValue(member, out result) == false)
                {
                    if (member is FieldInfo)
                    {
                        result = EmitUtilities.CreateWeakInstanceFieldSetter(member.DeclaringType, member as FieldInfo);
                    }
                    else if (member is PropertyInfo)
                    {
                        result = EmitUtilities.CreateWeakInstancePropertySetter(member.DeclaringType, member as PropertyInfo);
                    }
                    else
                    {
                        result = delegate (ref object instance, object value)
                        {
                            FormatterUtilities.SetMemberValue(member, instance, value);
                        };
                    }

                    GodotMemberSetters.Add(member, result);
                }

                return result;
            }
        }

        private static ICache GetCachedGodotWriter(DataFormat format, Stream stream, SerializationContext context)
        {
            ICache cache;

            switch (format)
            {
                case DataFormat.Binary:
                    {
                        var c = Cache<BinaryDataWriter>.Claim();
                        c.Value.Stream = stream;
                        cache = c;
                    }
                    break;
                case DataFormat.JSON:
                    {
                        var c = Cache<JsonDataWriter>.Claim();
                        c.Value.Stream = stream;
                        cache = c;
                    }
                    break;
                case DataFormat.Nodes:
                    throw new InvalidOperationException("Don't do this for nodes!");
                default:
                    throw new NotImplementedException(format.ToString());
            }

            (cache.Value as IDataWriter).Context = context;

            return cache;
        }

        private static ICache GetCachedGodotReader(DataFormat format, Stream stream, DeserializationContext context)
        {
            ICache cache;

            switch (format)
            {
                case DataFormat.Binary:
                    {
                        var c = Cache<BinaryDataReader>.Claim();
                        c.Value.Stream = stream;
                        cache = c;
                    }
                    break;
                case DataFormat.JSON:
                    {
                        var c = Cache<JsonDataReader>.Claim();
                        c.Value.Stream = stream;
                        cache = c;
                    }
                    break;
                case DataFormat.Nodes:
                    throw new InvalidOperationException("Don't do this for nodes!");
                default:
                    throw new NotImplementedException(format.ToString());
            }

            (cache.Value as IDataReader).Context = context;

            return cache;
        }
    }
}
