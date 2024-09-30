//-----------------------------------------------------------------------
// <copyright file="Transform3DFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(Transform3DFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Transform3D"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Transform3D}" />
    public class Transform3DFormatter : MinimalBaseFormatter<Transform3D>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Transform3D value, IDataReader reader)
        {
            Basis basis = value.Basis;

            basis.X = new Vector3(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
            basis.Y = new Vector3(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
            basis.Z = new Vector3(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));

            value.Basis = basis;
            value.Origin = new Vector3(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Transform3D value, IDataWriter writer)
        {
            FloatSerializer.WriteValue(value.Basis.X.X, writer);
            FloatSerializer.WriteValue(value.Basis.X.Y, writer);
            FloatSerializer.WriteValue(value.Basis.X.Z, writer);
            FloatSerializer.WriteValue(value.Basis.Y.X, writer);
            FloatSerializer.WriteValue(value.Basis.Y.Y, writer);
            FloatSerializer.WriteValue(value.Basis.Y.Z, writer);
            FloatSerializer.WriteValue(value.Basis.Z.X, writer);
            FloatSerializer.WriteValue(value.Basis.Z.Y, writer);
            FloatSerializer.WriteValue(value.Basis.Z.Z, writer);
            FloatSerializer.WriteValue(value.Origin.X, writer);
            FloatSerializer.WriteValue(value.Origin.Y, writer);
            FloatSerializer.WriteValue(value.Origin.Z, writer);
        }
    }
}
