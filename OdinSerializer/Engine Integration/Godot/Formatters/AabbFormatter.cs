//-----------------------------------------------------------------------
// <copyright file="AabbFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(AabbFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Aabb"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Aabb}" />
    public class AabbFormatter : MinimalBaseFormatter<Aabb>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Aabb value, IDataReader reader)
        {
            float posX = FloatSerializer.ReadValue(reader);
            float posY = FloatSerializer.ReadValue(reader);
            float posZ = FloatSerializer.ReadValue(reader);
            float sizeX = FloatSerializer.ReadValue(reader);
            float sizeY = FloatSerializer.ReadValue(reader);
            float sizeZ = FloatSerializer.ReadValue(reader);

            value.Position = new Vector3(posX, posY, posZ);
            value.Size = new Vector3(sizeX, sizeY, sizeZ);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Aabb value, IDataWriter writer)
        {
            FloatSerializer.WriteValue(value.Position.X, writer);
            FloatSerializer.WriteValue(value.Position.Y, writer);
            FloatSerializer.WriteValue(value.Position.Z, writer);
            FloatSerializer.WriteValue(value.Size.X, writer);
            FloatSerializer.WriteValue(value.Size.Y, writer);
            FloatSerializer.WriteValue(value.Size.Z, writer);
        }
    }
}
