//-----------------------------------------------------------------------
// <copyright file="Rect2IFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(Rect2IFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Rect2I"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Rect2I}" />
    public class Rect2IFormatter : MinimalBaseFormatter<Rect2I>
    {
        private static readonly Serializer<int> IntSerializer = Serializer.Get<int>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Rect2I value, IDataReader reader)
        {
            int posX = IntSerializer.ReadValue(reader);
            int posY = IntSerializer.ReadValue(reader);
            int sizeX = IntSerializer.ReadValue(reader);
            int sizeY = IntSerializer.ReadValue(reader);

            value.Position = new Vector2I(posX, posY);
            value.Size = new Vector2I(sizeX, sizeY);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Rect2I value, IDataWriter writer)
        {
            IntSerializer.WriteValue(value.Position.X, writer);
            IntSerializer.WriteValue(value.Position.Y, writer);
            IntSerializer.WriteValue(value.Size.X, writer);
            IntSerializer.WriteValue(value.Size.Y, writer);
        }
    }
}
