﻿//-----------------------------------------------------------------------
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

[assembly: RegisterFormatter(typeof(Vector4Formatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Vector4"/> type.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{UnityEngine.Vector4}" />
    public class Vector4Formatter : MinimalBaseFormatter<Vector4>
    {
        private static readonly Serializer<float> FloatSerializer = Serializer.Get<float>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Vector4 value, IDataReader reader)
        {
            value.X = FloatSerializer.ReadValue(reader);
            value.Y = FloatSerializer.ReadValue(reader);
            value.Z = FloatSerializer.ReadValue(reader);
            value.W = FloatSerializer.ReadValue(reader);
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Vector4 value, IDataWriter writer)
        {
            FloatSerializer.WriteValue(value.X, writer);
            FloatSerializer.WriteValue(value.Y, writer);
            FloatSerializer.WriteValue(value.Z, writer);
            FloatSerializer.WriteValue(value.W, writer);
        }
    }
}