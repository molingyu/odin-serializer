//-----------------------------------------------------------------------
// <copyright file="PlaneFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(PlaneFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Plane"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Plane}" />
    public class PlaneFormatter : MinimalBaseFormatter<Plane>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Plane value, IDataReader reader)
        {
            float normalX = FloatSerializer.ReadValue(reader);
            float normalY = FloatSerializer.ReadValue(reader);
            float normalZ = FloatSerializer.ReadValue(reader);
            float d = FloatSerializer.ReadValue(reader);

            value.Normal = new Vector3(normalX, normalY, normalZ);
            value.D = d;
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Plane value, IDataWriter writer)
        {
            FloatSerializer.WriteValue(value.Normal.X, writer);
            FloatSerializer.WriteValue(value.Normal.Y, writer);
            FloatSerializer.WriteValue(value.Normal.Z, writer);
            FloatSerializer.WriteValue(value.D, writer);
        }
    }
}
