// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class DllResourceStore : IResourceStore<byte[]>
    {
        private readonly Assembly assembly;
        private readonly string prefix;

        public DllResourceStore(string dllName)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), dllName);

            // prefer the local file if it exists, else load from assembly cache.
            assembly = File.Exists(filePath) ? Assembly.LoadFrom(filePath) : Assembly.Load(Path.GetFileNameWithoutExtension(dllName));

            prefix = Path.GetFileNameWithoutExtension(dllName);
        }

        public DllResourceStore(AssemblyName name)
            : this(Assembly.Load(name))
        {
        }

        public DllResourceStore(Assembly assembly)
        {
            this.assembly = assembly;
            prefix = assembly.GetName().Name;
        }

        public byte[] Get(string name)
        {
            this.LogIfNonBackgroundThread(name);

            using (Stream input = GetStream(name))
            {
                if (input == null)
                    return null;

                byte[] buffer = new byte[input.Length];
                input.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public virtual async Task<byte[]> GetAsync(string name)
        {
            this.LogIfNonBackgroundThread(name);

            using (Stream input = GetStream(name))
            {
                if (input == null)
                    return null;

                byte[] buffer = new byte[input.Length];
                await input.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
                return buffer;
            }
        }

        /// <summary>
        /// Retrieve a list of available resources provided by this store.
        /// </summary>
        public IEnumerable<string> GetAvailableResources() =>
            assembly.GetManifestResourceNames().Select(n =>
            {
                n = n.Substring(n.StartsWith(prefix, StringComparison.Ordinal) ? prefix.Length + 1 : 0);

                int lastDot = n.LastIndexOf('.');

                char[] chars = n.ToCharArray();

                for (int i = 0; i < lastDot; i++)
                {
                    if (chars[i] == '.')
                        chars[i] = '/';
                }

                return new string(chars);
            });

        public Stream GetStream(string name)
        {
            this.LogIfNonBackgroundThread(name);

            string[] split = name.Split('/');
            for (int i = 0; i < split.Length - 1; i++)
                split[i] = split[i].Replace('-', '_');

            return assembly?.GetManifestResourceStream($@"{prefix}.{string.Join('.', split)}");
        }

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }
}
