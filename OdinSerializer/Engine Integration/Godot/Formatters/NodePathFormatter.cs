//-----------------------------------------------------------------------
// <copyright file="NodePathFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(NodePathFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="NodePath"/> type. Serializes the path as a string.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{NodePath}" />
    public class NodePathFormatter : MinimalBaseFormatter<NodePath>
    {
        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref NodePath value, IDataReader reader)
        {
            string name;

            if (reader.PeekEntry(out name) == EntryType.String)
            {
                string path;
                reader.ReadString(out path);

                if (path != null)
                {
                    value = new NodePath(path);
                    this.RegisterReferenceID(value, reader);
                }
            }
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref NodePath value, IDataWriter writer)
        {
            writer.WriteString(null, value.ToString());
        }

        /// <summary>
        /// Returns null; the instance is created during <see cref="Read"/>.
        /// </summary>
        /// <returns>null.</returns>
        protected override NodePath GetUninitializedObject()
        {
            return null;
        }
    }
}
