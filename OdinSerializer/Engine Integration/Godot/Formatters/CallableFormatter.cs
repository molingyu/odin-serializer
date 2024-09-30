//-----------------------------------------------------------------------
// <copyright file="CallableFormatter.cs" company="Sirenix IVS">
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

[assembly: RegisterFormatter(typeof(CallableFormatter))]

namespace OdinSerializer
{
    /// <summary>
    /// Custom formatter for the <see cref="Callable"/> type. Serializes the target object
    /// (as an external Godot object reference) and the method name.
    /// <para />
    /// Only method callables can be serialized; custom callables wrapping delegates or
    /// lambdas have no target object and are deserialized as <c>default(Callable)</c>.
    /// </summary>
    /// <seealso cref="MinimalBaseFormatter{Callable}" />
    public class CallableFormatter : MinimalBaseFormatter<Callable>
    {
        private static readonly Serializer<GodotObject> GodotObjectSerializer = Serializer.Get<GodotObject>();
        private static readonly Serializer<StringName> StringNameSerializer = Serializer.Get<StringName>();

        /// <summary>
        /// Reads into the specified value using the specified reader.
        /// </summary>
        /// <param name="value">The value to read into.</param>
        /// <param name="reader">The reader to use.</param>
        protected override void Read(ref Callable value, IDataReader reader)
        {
            bool hasTarget;
            reader.ReadBoolean(out hasTarget);

            if (hasTarget)
            {
                var target = GodotObjectSerializer.ReadValue(reader);
                var method = StringNameSerializer.ReadValue(reader);
                value = new Callable(target, method);
            }
            else
            {
                value = default;
            }
        }

        /// <summary>
        /// Writes from the specified value using the specified writer.
        /// </summary>
        /// <param name="value">The value to write from.</param>
        /// <param name="writer">The writer to use.</param>
        protected override void Write(ref Callable value, IDataWriter writer)
        {
            var target = value.Target;

            if (target != null)
            {
                writer.WriteBoolean(null, true);
                GodotObjectSerializer.WriteValue(target, writer);
                StringNameSerializer.WriteValue(value.Method, writer);
            }
            else
            {
                writer.WriteBoolean(null, false);
            }
        }
    }
}
