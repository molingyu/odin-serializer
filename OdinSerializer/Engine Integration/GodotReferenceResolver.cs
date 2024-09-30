//-----------------------------------------------------------------------
// <copyright file="UnityReferenceResolver.cs" company="Sirenix IVS">
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
    using Utilities;

    /// <summary>
    /// Resolves external index references to Unity objects.
    /// </summary>
    /// <seealso cref="IExternalIndexReferenceResolver" />
    /// <seealso cref="ICacheNotificationReceiver" />
    public sealed class EngineReferenceResolver : IExternalIndexReferenceResolver, ICacheNotificationReceiver
    {
        private Dictionary<Godot.GodotObject, int> _referenceIndexMapping = new(32, ReferenceEqualityComparer<Godot.GodotObject>.Default);
        private List<Godot.GodotObject> _referencedUnityObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineReferenceResolver"/> class.
        /// </summary>
        public EngineReferenceResolver()
        {
            this._referencedUnityObjects = new List<Godot.GodotObject>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineReferenceResolver"/> class with a list of Unity objects.
        /// </summary>
        /// <param name="referencedUnityObjects">The referenced Unity objects.</param>
        public EngineReferenceResolver(List<Godot.GodotObject> referencedUnityObjects)
        {
            this.SetReferencedEngineObjects(referencedUnityObjects);
        }

        /// <summary>
        /// Gets the currently referenced Unity objects.
        /// </summary>
        /// <returns>A list of the currently referenced Unity objects.</returns>
        public List<Godot.GodotObject> GetReferencedEngineObjects()
        {
            return this._referencedUnityObjects;
        }

        /// <summary>
        /// Sets the referenced Unity objects of the resolver to a given list, or a new list if the value is null.
        /// </summary>
        /// <param name="referencedNodes">The referenced Unity objects to set, or null if a new list is required.</param>
        public void SetReferencedEngineObjects(List<Godot.GodotObject> referencedNodes)
        {
            if (referencedNodes == null)
            {
                referencedNodes = new List<Godot.GodotObject>();
            }

            this._referencedUnityObjects = referencedNodes;
            this._referenceIndexMapping.Clear();

            for (int i = 0; i < this._referencedUnityObjects.Count; i++)
            {
                if (object.ReferenceEquals(this._referencedUnityObjects[i], null) == false)
                {
                    if (!this._referenceIndexMapping.ContainsKey(this._referencedUnityObjects[i]))
                    {
                        this._referenceIndexMapping.Add(this._referencedUnityObjects[i], i);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the specified value can be referenced externally via this resolver.
        /// </summary>
        /// <param name="value">The value to reference.</param>
        /// <param name="index">The index of the resolved value, if it can be referenced.</param>
        /// <returns>
        ///   <c>true</c> if the reference can be resolved, otherwise <c>false</c>.
        /// </returns>
        public bool CanReference(object value, out int index)
        {
            if (this._referencedUnityObjects == null)
            {
                this._referencedUnityObjects = new List<Godot.GodotObject>(32);
            }

            var obj = value as Godot.GodotObject;

            if (object.ReferenceEquals(null, obj) == false)
            {
                if (this._referenceIndexMapping.TryGetValue(obj, out index) == false)
                {
                    index = this._referencedUnityObjects.Count;
                    this._referenceIndexMapping.Add(obj, index);
                    this._referencedUnityObjects.Add(obj);
                }

                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Tries to resolve the given reference index to a reference value.
        /// </summary>
        /// <param name="index">The index to resolve.</param>
        /// <param name="value">The resolved value.</param>
        /// <returns>
        ///   <c>true</c> if the index could be resolved to a value, otherwise <c>false</c>.
        /// </returns>
        public bool TryResolveReference(int index, out object value)
        {
            if (this._referencedUnityObjects == null || index < 0 || index >= this._referencedUnityObjects.Count)
            {
                // Sometimes something has destroyed the list of references in between serialization and deserialization
                // (Unity prefab instances are especially bad at preserving such data), and in these cases we still don't
                // want the system to fall back to a formatter, so we give out a null value.
                value = null;
                return true;
            }

            value = this._referencedUnityObjects[index];
            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            this._referencedUnityObjects = null;
            this._referenceIndexMapping.Clear();
        }

        void ICacheNotificationReceiver.OnFreed()
        {
            this.Reset();
        }

        void ICacheNotificationReceiver.OnClaimed()
        {
        }
    }
}