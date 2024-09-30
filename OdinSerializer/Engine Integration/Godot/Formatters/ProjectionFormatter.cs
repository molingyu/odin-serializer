//-----------------------------------------------------------------------
// <copyright file="ProjectionFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(ProjectionFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Projection"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Projection}" />
    public class ProjectionFormatter : MinimalBaseFormatter<Projection>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Projection value, IDataReader reader)
        {
            value.X = ReadVector4(reader);
            value.Y = ReadVector4(reader);
            value.Z = ReadVector4(reader);
            value.W = ReadVector4(reader);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Projection value, IDataWriter writer)
        {
            WriteVector4(value.X, writer);
            WriteVector4(value.Y, writer);
            WriteVector4(value.Z, writer);
            WriteVector4(value.W, writer);
        }

        private static Vector4 ReadVector4(IDataReader reader)
        {
            return new Vector4(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
        }

        private static void WriteVector4(Vector4 vector, IDataWriter writer)
        {
            FloatSerializer.WriteValue(vector.X, writer);
            FloatSerializer.WriteValue(vector.Y, writer);
            FloatSerializer.WriteValue(vector.Z, writer);
            FloatSerializer.WriteValue(vector.W, writer);
        }
    }
}
