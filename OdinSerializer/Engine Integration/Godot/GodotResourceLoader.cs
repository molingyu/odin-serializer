//-----------------------------------------------------------------------
// <copyright file="GodotResourceLoader.cs" company="Sirenix IVS">
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
    using System;
    using Utilities.Wrapper;

    /// <summary>
    /// Loads Godot resources from paths, choosing the loading mechanism by path scheme and
    /// file extension:
    /// <para />
    /// - Engine paths (res://, uid://) and Godot resource files (.tres/.res, including under
    ///   user://) go through the engine's resource loading system (<see cref="ResourceLoader"/>).
    /// <para />
    /// - External media files (absolute paths, or paths outside the project, any scheme) are
    ///   loaded at runtime through the engine's dedicated external loading interfaces:
    ///   images via <see cref="Image.LoadFromFile(string)"/>, audio via
    ///   <see cref="AudioStreamWav.LoadFromFile(string, Godot.Collections.Dictionary)"/> /
    ///   <see cref="AudioStreamMP3.LoadFromFile(string)"/> /
    ///   <see cref="AudioStreamOggVorbis.LoadFromFile(string)"/>, fonts via
    ///   <see cref="FontFile.LoadDynamicFont(string)"/>.
    /// <para />
    /// Resources loaded through the runtime path get their <see cref="Resource.ResourcePath"/>
    /// set to the source path, so serializing them stays path-based (instead of falling back
    /// to inline data) and repeated loads are easy to deduplicate.
    /// <para />
    /// For mod-style scenarios, load the pack once with
    /// <see cref="ProjectSettings.LoadResourcePack(string, bool, int)"/>; the resources inside
    /// then become regular res:// resources and go through the engine branch automatically.
    /// </summary>
    public static class GodotResourceLoader
    {
        private static readonly System.Collections.Generic.HashSet<string> ImageExtensions = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".svg", ".exr", ".hdr", ".tga",
        };

        private static readonly System.Collections.Generic.HashSet<string> FontExtensions = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ttf", ".otf", ".woff", ".woff2",
        };

        /// <summary>
        /// Cache for runtime-loaded external resources, mirroring <see cref="ResourceLoader"/>'s
        /// cache: loading the same path twice returns the same instance. This also avoids fighting
        /// the engine's resource_path registration, which rejects assigning a path that another
        /// living resource already holds.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, Resource> RuntimeResourceCache = new System.Collections.Generic.Dictionary<string, Resource>();

        /// <summary>
        /// Clears the cache of runtime-loaded external resources.
        /// </summary>
        public static void ClearRuntimeCache()
        {
            RuntimeResourceCache.Clear();
        }

        /// <summary>
        /// Loads a resource of type <typeparamref name="T"/> from the given path, using the
        /// engine resource system for engine paths and dedicated runtime loaders for external
        /// media files.
        /// </summary>
        /// <typeparam name="T">The expected resource type.</typeparam>
        /// <param name="path">The resource path (res://, uid://, user:// or an absolute/external path).</param>
        /// <returns>The loaded resource, or null if loading failed.</returns>
        public static T Load<T>(string path) where T : Resource
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // Runtime-loaded external resources are deduplicated through a path cache
            if (RuntimeResourceCache.TryGetValue(path, out var cached))
            {
                if (cached is T typedCached && GodotObject.IsInstanceValid(cached))
                {
                    return typedCached;
                }

                RuntimeResourceCache.Remove(path);
            }

            string extension = System.IO.Path.GetExtension(path);

            // Engine paths (res://, uid://) always go through the engine's resource system,
            // regardless of extension: an imported res://foo.svg must resolve to the imported
            // CompressedTexture2D (with its ResourcePath), not to a runtime ImageTexture that
            // would lose the path on the next save. The runtime loaders below are only for
            // external media files (absolute paths, user://, paths outside the project).
            if (path.StartsWith("res://") || path.StartsWith("uid://"))
            {
                return ResourceLoader.Load<T>(path);
            }

            if (ImageExtensions.Contains(extension))
            {
                return LoadImageResource<T>(path);
            }

            if (FontExtensions.Contains(extension))
            {
                return LoadFontResource<T>(path);
            }

            switch (extension.ToLowerInvariant())
            {
                case ".wav":
                    return FinishLoad<T>(AudioStreamWav.LoadFromFile(path), path);
                case ".mp3":
                    return FinishLoad<T>(AudioStreamMP3.LoadFromFile(path), path);
                case ".ogg":
                    return FinishLoad<T>(AudioStreamOggVorbis.LoadFromFile(path), path);
                default:
                    // res://, uid://, and .tres/.res files (including under user://)
                    return ResourceLoader.Load<T>(path);
            }
        }

        private static T LoadImageResource<T>(string path) where T : Resource
        {
            var image = Image.LoadFromFile(path);

            if (image == null)
            {
                DebugWrapper.LogWarning("GodotResourceLoader: failed to load image at '" + path + "'.");
                return null;
            }

            // The caller asked for the Image itself (or a base type of it)
            if (image is T typedImage)
            {
                typedImage.ResourcePath = path;
                return typedImage;
            }

            // Otherwise hand out a texture
            var texture = ImageTexture.CreateFromImage(image);
            return FinishLoad<T>(texture, path);
        }

        private static T LoadFontResource<T>(string path) where T : Resource
        {
            var font = new FontFile();
            var error = font.LoadDynamicFont(path);

            if (error != Error.Ok)
            {
                DebugWrapper.LogWarning("GodotResourceLoader: failed to load font at '" + path + "' (error " + error + ").");
                return null;
            }

            return FinishLoad<T>(font, path);
        }

        private static T FinishLoad<T>(Resource resource, string path) where T : Resource
        {
            if (resource == null)
            {
                DebugWrapper.LogWarning("GodotResourceLoader: failed to load resource at '" + path + "'.");
                return null;
            }

            if (resource is T typed)
            {
                // Keep the source path so serialization stays path-based, and cache the instance
                // so repeated loads (including during deserialization) return the same resource.
                // Note the path assignment only sticks when no other living resource holds the
                // path, which is exactly what the cache guarantees.
                typed.ResourcePath = path;
                RuntimeResourceCache[path] = typed;
                return typed;
            }

            DebugWrapper.LogWarning("GodotResourceLoader: resource at '" + path + "' is of type '" + resource.GetType().Name + "', which does not match the requested type '" + typeof(T).Name + "'.");
            return null;
        }
    }
}
