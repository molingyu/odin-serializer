//-----------------------------------------------------------------------
// <copyright file="Vector4Formatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(Vector2IntFormatter))]
[assembly: RegisterFormatter(typeof(Vector3IntFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Vector2I"/> type.
    /// </summary>
    /// <seealso cref="Sirenix.Serialization.MinimalBaseFormatter{UnityEngine.Vector2I}" />
    public class Vector2IntFormatter : MinimalBaseFormatter<Vector2I>
    {
        private static readonly Serializer<int> Serializer = OdinSerializer.Serializer.Get<int>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Vector2I value, IDataReader reader)
        {
            value.X = Serializer.ReadValue(reader);
            value.Y = Serializer.ReadValue(reader);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Vector2I value, IDataWriter writer)
        {
            Serializer.WriteValue(value.X, writer);
            Serializer.WriteValue(value.Y, writer);
        }
    }

    /// <summary>
    /// Custom formatter for the <see cref="Vector3I"/> type.
    /// </summary>
    /// <seealso cref="Sirenix.Serialization.MinimalBaseFormatter{UnityEngine.Vector3I}" />
    public class Vector3IntFormatter : MinimalBaseFormatter<Vector3I>
    {
        private static readonly Serializer<int> Serializer = OdinSerializer.Serializer.Get<int>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Vector3I value, IDataReader reader)
        {
            value.X = Serializer.ReadValue(reader);
            value.Y = Serializer.ReadValue(reader);
            value.Z = Serializer.ReadValue(reader);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Vector3I value, IDataWriter writer)
        {
            Serializer.WriteValue(value.X, writer);
            Serializer.WriteValue(value.Y, writer);
            Serializer.WriteValue(value.Z, writer);
        }
    }
}