//-----------------------------------------------------------------------
// <copyright file="Transform2DFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(Transform2DFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Transform2D"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Transform2D}" />
    public class Transform2DFormatter : MinimalBaseFormatter<Transform2D>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Transform2D value, IDataReader reader)
        {
            value.X = new Vector2(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
            value.Y = new Vector2(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
            value.Origin = new Vector2(FloatSerializer.ReadValue(reader), FloatSerializer.ReadValue(reader));
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Transform2D value, IDataWriter writer)
        {
            FloatSerializer.WriteValue(value.X.X, writer);
            FloatSerializer.WriteValue(value.X.Y, writer);
            FloatSerializer.WriteValue(value.Y.X, writer);
            FloatSerializer.WriteValue(value.Y.Y, writer);
            FloatSerializer.WriteValue(value.Origin.X, writer);
            FloatSerializer.WriteValue(value.Origin.Y, writer);
        }
    }
}
