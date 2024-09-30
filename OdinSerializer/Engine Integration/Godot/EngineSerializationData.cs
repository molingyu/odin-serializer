//-----------------------------------------------------------------------
// <copyright file="EngineSerializationData.cs" company="Sirenix IVS">
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

    /// <summary>
    /// Provides ClassDB property metadata for engine-internal Godot types, generated from the
    /// GDExtension API dump (see Tools/EngineSerializationGenerator/generate.py). The generated
    /// property lists are a superset of the serializable properties; the runtime additionally
    /// filters them by <see cref="PropertyUsageFlags.Storage"/> via one
    /// <see cref="GodotObject.GetPropertyList"/> call per type, which is then cached.
    /// </summary>
    public static partial class EngineSerializationData
    {
        private static readonly Dictionary<string, string[]> GeneratedProperties = BuildGeneratedMap();
        private static readonly Dictionary<Type, string[]> StoragePropertiesCache = new Dictionary<Type, string[]>();

        private static readonly string[] DenylistedProperties = { "resource_path", "script" };

        /// <summary>
        /// Names of the [Export] storage members declared on SerializedNode and
        /// SerializedResource (identical on both). They show up as script variables
        /// in the property list but must never be captured as engine properties — serializing the
        /// serialization data into itself would corrupt the data. The managed member path of
        /// GodotSerializationUtility/ResourceFormatter already skips them as well.
        /// Note: these are string literals because SerializedNode/SerializedResource are not
        /// compiled into this assembly (they are source-included by the consuming Godot project).
        /// </summary>
        private static readonly HashSet<string> SerializationStorageMemberNames = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "SerializedBytes",
            "SerializedBytesString",
            "SerializedFormat",
            "ReferencedGodotObjects",
            "CaptureEngineProperties",
        };

        private static Dictionary<string, string[]> BuildGeneratedMap()
        {
            var map = new Dictionary<string, string[]>(620);
            RegisterGeneratedTypes(map);
            return map;
        }

        static partial void RegisterGeneratedTypes(Dictionary<string, string[]> map);

        /// <summary>
        /// Tries to get the generated ClassDB property name list for an engine class
        /// (for example "Sprite2D" or "ImageTexture").
        /// </summary>
        /// <param name="engineClassName">The Godot engine class name.</param>
        /// <param name="properties">The generated property names, if the class is known.</param>
        /// <returns><c>true</c> if the engine class has generated serialization data.</returns>
        public static bool TryGetGeneratedProperties(string engineClassName, out string[] properties)
        {
            return GeneratedProperties.TryGetValue(engineClassName, out properties);
        }

        /// <summary>
        /// Gets the final list of ClassDB property names to serialize for the given object:
        /// the generated property set of its engine class filtered by
        /// <see cref="PropertyUsageFlags.Storage"/>, plus script variables ([Export] members)
        /// of custom derived classes. The result is computed once per type and cached.
        /// Requires the Godot native runtime.
        /// </summary>
        /// <param name="value">The object to get the serializable properties of.</param>
        /// <returns>The ClassDB names of the properties to serialize.</returns>
        public static string[] GetStorageProperties(GodotObject value)
        {
            var type = value.GetType();

            lock (StoragePropertiesCache)
            {
                if (StoragePropertiesCache.TryGetValue(type, out var cached))
                {
                    return cached;
                }
            }

            var result = ComputeStorageProperties(value);

            lock (StoragePropertiesCache)
            {
                StoragePropertiesCache[type] = result;
            }

            return result;
        }

        private static string[] ComputeStorageProperties(GodotObject value)
        {
            // One GetPropertyList call per type: build the name -> usage map used both for
            // storage filtering of the generated candidates and for script variable lookup.
            var usageByName = new Dictionary<string, PropertyUsageFlags>();

            foreach (var propInfo in value.GetPropertyList())
            {
                usageByName[propInfo["name"].AsString()] = (PropertyUsageFlags)propInfo["usage"].AsInt64();
            }

            var runtimeType = value.GetType();
            var engineType = GetEngineType(runtimeType);

            List<string> result;

            if (engineType != null && GeneratedProperties.TryGetValue(engineType.Name, out var candidates))
            {
                result = new List<string>(candidates.Length + 8);

                foreach (var name in candidates)
                {
                    if (usageByName.TryGetValue(name, out var usage) && (usage & PropertyUsageFlags.Storage) != 0)
                    {
                        result.Add(name);
                    }
                }

                // Custom derived classes: their [Export] members show up as script variables
                if (engineType != runtimeType)
                {
                    foreach (var pair in usageByName)
                    {
                        if ((pair.Value & PropertyUsageFlags.Storage) != 0 && (pair.Value & PropertyUsageFlags.ScriptVariable) != 0 && !SerializationStorageMemberNames.Contains(pair.Key) && !result.Contains(pair.Key))
                        {
                            result.Add(pair.Key);
                        }
                    }
                }
            }
            else
            {
                // Fallback for types with no generated data: all storage properties.
                result = new List<string>(usageByName.Count);

                foreach (var pair in usageByName)
                {
                    if ((pair.Value & PropertyUsageFlags.Storage) != 0)
                    {
                        result.Add(pair.Key);
                    }
                }
            }

            foreach (var denied in DenylistedProperties)
            {
                result.Remove(denied);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Finds the deepest base type that is defined in the GodotSharp assembly,
        /// which corresponds to the engine class of the object.
        /// </summary>
        private static Type GetEngineType(Type type)
        {
            var engineAssembly = typeof(GodotObject).Assembly;
            var current = type;

            while (current != null && current.Assembly != engineAssembly)
            {
                current = current.BaseType;
            }

            return current;
        }
    }
}
