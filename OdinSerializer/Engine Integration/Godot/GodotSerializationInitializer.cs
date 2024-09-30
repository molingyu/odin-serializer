//-----------------------------------------------------------------------
// <copyright file="GodotSerializationInitializer.cs" company="Sirenix IVS">
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
    using Utilities.Wrapper;

    /// <summary>
    /// Initializes the Odin serialization system for Godot. This is the Godot counterpart of Unity's
    /// UnitySerializationInitializer.
    /// <para />
    /// Initialization loads the global serialization config if present, and detects the current runtime
    /// platform so the serializer can enable fast unaligned memory reads/writes where supported.
    /// <para />
    /// <see cref="SerializedNode"/> and <see cref="SerializedResource"/> call <see cref="Initialize"/>
    /// automatically. If you use <see cref="SerializationUtility"/> or <see cref="GodotSerializationUtility"/>
    /// directly, call <see cref="Initialize"/> once yourself, for example from your game's entry point.
    /// </summary>
    public static class GodotSerializationInitializer
    {
        private static readonly object LOCK = new object();
        private static bool initialized;

        /// <summary>
        /// Initializes the serialization system. This method is idempotent and thread-safe.
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            lock (LOCK)
            {
                if (initialized)
                {
                    return;
                }

                GlobalSerializationConfig.LoadInstanceIfAssetExists();

                if (TryGetCurrentPlatform(out RuntimePlatform platform))
                {
                    ArchitectureInfo.SetRuntimePlatform(platform);
                }

                initialized = true;
            }
        }

        private static bool TryGetCurrentPlatform(out RuntimePlatform platform)
        {
            switch (OS.GetName())
            {
                case "Windows":
                    platform = RuntimePlatform.WindowsPlayer;
                    return true;
                case "macOS":
                    platform = RuntimePlatform.OSXPlayer;
                    return true;
                case "Linux":
                    platform = RuntimePlatform.LinuxPlayer;
                    return true;
                case "Android":
                    platform = RuntimePlatform.Android;
                    return true;
                case "iOS":
                    platform = RuntimePlatform.IPhonePlayer;
                    return true;
                case "Web":
                    platform = RuntimePlatform.WebGLPlayer;
                    return true;
                default:
                    platform = default(RuntimePlatform);
                    return false;
            }
        }
    }
}
