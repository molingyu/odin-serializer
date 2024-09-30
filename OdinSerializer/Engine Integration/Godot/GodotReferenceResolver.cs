//-----------------------------------------------------------------------
// <copyright file="GodotReferenceResolver.cs" company="Sirenix IVS">
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
    /// Resolves external index references to Godot objects.
    /// </summary>
    /// <seealso cref="IExternalIndexReferenceResolver" />
    /// <seealso cref="ICacheNotificationReceiver" />
    public sealed class EngineReferenceResolver : IExternalIndexReferenceResolver, ICacheNotificationReceiver
    {
        private Dictionary<Godot.GodotObject, int> _referenceIndexMapping = new(32, ReferenceEqualityComparer<Godot.GodotObject>.Default);
        private List<Godot.GodotObject> _referencedGodotObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineReferenceResolver"/> class.
        /// </summary>
        public EngineReferenceResolver()
        {
            this._referencedGodotObjects = new List<Godot.GodotObject>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineReferenceResolver"/> class with a list of Godot objects.
        /// </summary>
        /// <param name="referencedGodotObjects">The referenced Godot objects.</param>
        public EngineReferenceResolver(List<Godot.GodotObject> referencedGodotObjects)
        {
            this.SetReferencedEngineObjects(referencedGodotObjects);
        }

        /// <summary>
        /// Gets the currently referenced Godot objects.
        /// </summary>
        /// <returns>A list of the currently referenced Godot objects.</returns>
        public List<Godot.GodotObject> GetReferencedEngineObjects()
        {
            return this._referencedGodotObjects;
        }

        /// <summary>
        /// Sets the referenced Godot objects of the resolver to a given list, or a new list if the value is null.
        /// </summary>
        /// <param name="referencedGodotObjects">The referenced Godot objects to set, or null if a new list is required.</param>
        public void SetReferencedEngineObjects(List<Godot.GodotObject> referencedGodotObjects)
        {
            if (referencedGodotObjects == null)
            {
                referencedGodotObjects = new List<Godot.GodotObject>();
            }

            this._referencedGodotObjects = referencedGodotObjects;
            this._referenceIndexMapping.Clear();

            for (int i = 0; i < this._referencedGodotObjects.Count; i++)
            {
                if (object.ReferenceEquals(this._referencedGodotObjects[i], null) == false)
                {
                    if (!this._referenceIndexMapping.ContainsKey(this._referencedGodotObjects[i]))
                    {
                        this._referenceIndexMapping.Add(this._referencedGodotObjects[i], i);
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
            if (this._referencedGodotObjects == null)
            {
                this._referencedGodotObjects = new List<Godot.GodotObject>(32);
            }

            var obj = value as Godot.GodotObject;

            // Resources are not referenced externally; they are serialized by path
            // or inline data via ResourceFormatter instead.
            if (object.ReferenceEquals(null, obj) == false && obj is not Godot.Resource)
            {
                if (this._referenceIndexMapping.TryGetValue(obj, out index) == false)
                {
                    index = this._referencedGodotObjects.Count;
                    this._referenceIndexMapping.Add(obj, index);
                    this._referencedGodotObjects.Add(obj);
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
            if (this._referencedGodotObjects == null || index < 0 || index >= this._referencedGodotObjects.Count)
            {
                // Sometimes something has destroyed the list of references in between serialization and deserialization,
                // and in these cases we still don't want the system to fall back to a formatter,
                // so we give out a null value.
                value = null;
                return true;
            }

            value = this._referencedGodotObjects[index];
            return true;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            this._referencedGodotObjects = null;
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
