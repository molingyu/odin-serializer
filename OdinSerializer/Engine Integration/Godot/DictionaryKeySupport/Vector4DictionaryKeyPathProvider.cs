//-----------------------------------------------------------------------
// <copyright file="Vector4DictionaryKeyPathProvider.cs" company="Sirenix IVS">
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

using OdinSerializer;

[assembly: RegisterDictionaryKeyPathProvider(typeof(Vector4DictionaryKeyPathProvider))]

namespace OdinSerializer
{
    using System.Globalization;
    using Godot;

    public sealed class Vector4DictionaryKeyPathProvider : BaseDictionaryKeyPathProvider<Vector4>
    {
        public override string ProviderID { get { return "v4"; } }

        public override int Compare(Vector4 x, Vector4 y)
        {
            int result = x.X.CompareTo(y.X);

            if (result == 0)
            {
                result = x.Y.CompareTo(y.Y);
            }

            if (result == 0)
            {
                result = x.Z.CompareTo(y.Z);
            }

            if (result == 0)
            {
                result = x.W.CompareTo(y.W);
            }

            return result;
        }

        public override Vector4 GetKeyFromPathString(string pathStr)
        {
            int sep1 = pathStr.IndexOf('|');
            int sep2 = pathStr.IndexOf('|', sep1 + 1);
            int sep3 = pathStr.IndexOf('|', sep2 + 1);

            string x = pathStr.Substring(1, sep1 - 1).Trim();
            string y = pathStr.Substring(sep1 + 1, sep2 - (sep1 + 1)).Trim();
            string z = pathStr.Substring(sep2 + 1, sep3 - (sep2 + 1)).Trim();
            string w = pathStr.Substring(sep3 + 1, pathStr.Length - (sep3 + 2)).Trim();

            return new Vector4(float.Parse(x), float.Parse(y), float.Parse(z), float.Parse(w));
        }

        public override string GetPathStringFromKey(Vector4 key)
        {
            var x = key.X.ToString("R", CultureInfo.InvariantCulture);
            var y = key.Y.ToString("R", CultureInfo.InvariantCulture);
            var z = key.Z.ToString("R", CultureInfo.InvariantCulture);
            var w = key.W.ToString("R", CultureInfo.InvariantCulture);

            return ("(" + x + "|" + y + "|" + z + "|" + w + ")").Replace('.', ',');
        }
    }
}