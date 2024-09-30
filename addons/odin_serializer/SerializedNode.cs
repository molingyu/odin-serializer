//-----------------------------------------------------------------------
// <copyright file="SerializedNode.cs" company="Sirenix IVS">
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

namespace OdinSerializer
{
    using System.Collections.Generic;

    /// <summary>
    /// A Godot <see cref="Node"/> base class that serializes its Odin-serialized members with Odin Serializer.
    /// <para />
    /// All members that Godot itself does not serialize (IE, members not marked with <see cref="ExportAttribute"/>)
    /// are serialized by Odin into the exported <see cref="SerializedBytes"/>/<see cref="SerializedBytesString"/>
    /// storage, which Godot persists in the scene file like any other exported data.
    /// <para />
    /// Godot provides no serialization callbacks, so <see cref="Serialize"/> must be called manually
    /// (for example from a tool button before saving the scene, or when writing a savegame).
    /// Deserialization happens automatically in <see cref="_Ready"/>; if you override <see cref="_Ready"/>,
    /// remember to call <c>base._Ready()</c>.
    /// <para />
    /// Note that exported Godot object references in <see cref="ReferencedGodotObjects"/> only persist where
    /// Godot supports exported object references (Resource references in .tres files, Node references within scenes).
    /// </summary>
    public abstract partial class SerializedNode : Node
    {
        /// <summary>
        /// The Odin serialized data when serializing with the Binary format.
        /// </summary>
        [Export]
        public byte[] SerializedBytes { get; set; }

        /// <summary>
        /// The Odin serialized data when serializing with the JSON format, as a base64 string.
        /// </summary>
        [Export]
        public string SerializedBytesString { get; set; }

        /// <summary>
        /// The data format that was used for serialization, cast to int for Godot export compatibility.
        /// </summary>
        [Export]
        public int SerializedFormat { get; set; }

        /// <summary>
        /// All Godot objects that were referenced during serialization.
        /// </summary>
        [Export]
        public Godot.Collections.Array<GodotObject> ReferencedGodotObjects { get; set; } = new Godot.Collections.Array<GodotObject>();

        /// <summary>
        /// Whether <see cref="Serialize"/> also captures this node's engine-visible ClassDB properties
        /// (for example transform, visibility and other engine state) in addition to its Odin-serialized
        /// managed members. Enable this for runtime savegames where the full node state must be restored;
        /// the values are assigned back via the engine property system on <see cref="Deserialize"/>.
        /// </summary>
        [Export]
        public bool CaptureEngineProperties { get; set; } = true;

        /// <summary>
        /// Serializes this node's Odin-serialized members into the exported storage properties.
        /// Call this before the scene is saved for the data to persist.
        /// </summary>
        public void Serialize()
        {
            GodotSerializationInitializer.Initialize();

            if (this is Utilities.Wrapper.ISerializationCallbackReceiver callbackReceiver)
            {
                callbackReceiver.OnBeforeSerialize();
            }

            var data = new SerializationData();
            GodotSerializationUtility.SerializeGodotObject(this, ref data, serializeEngineProperties: this.CaptureEngineProperties);

            this.SerializedBytes = data.SerializedBytes;
            this.SerializedBytesString = data.SerializedBytesString;
            this.SerializedFormat = (int)data.SerializedFormat;
            this.ReferencedGodotObjects = data.ReferencedGodotObjects != null
                ? new Godot.Collections.Array<GodotObject>(data.ReferencedGodotObjects)
                : new Godot.Collections.Array<GodotObject>();
        }

        /// <summary>
        /// Deserializes this node's Odin-serialized members from the exported storage properties.
        /// Does nothing if no serialized data is present.
        /// </summary>
        public void Deserialize()
        {
            GodotSerializationInitializer.Initialize();

            bool hasBytes = this.SerializedBytes != null && this.SerializedBytes.Length > 0;
            bool hasString = !string.IsNullOrEmpty(this.SerializedBytesString);

            if (!hasBytes && !hasString)
            {
                return;
            }

            var data = new SerializationData
            {
                SerializedBytes = this.SerializedBytes,
                SerializedBytesString = this.SerializedBytesString,
                SerializedFormat = (DataFormat)this.SerializedFormat,
                ReferencedGodotObjects = this.ReferencedGodotObjects != null
                    ? new List<GodotObject>(this.ReferencedGodotObjects)
                    : new List<GodotObject>(),
            };

            GodotSerializationUtility.DeserializeGodotObject(this, ref data);

            if (this is Utilities.Wrapper.ISerializationCallbackReceiver callbackReceiver)
            {
                callbackReceiver.OnAfterDeserialize();
            }
        }

        /// <summary>
        /// Called when the node enters the scene tree. Deserializes the Odin-serialized members.
        /// If you override this method, remember to call <c>base._Ready()</c>.
        /// </summary>
        public override void _Ready()
        {
            this.Deserialize();
        }
    }
}
