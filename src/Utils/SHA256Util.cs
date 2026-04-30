using System.Collections.Concurrent;
using System.Text;

namespace Hydro.Utils
{
    internal class SHA256Util
    {
        private static ConcurrentDictionary<string, string> _assemblyCached { get; } = new();

        /// <summary>
        /// This functions hash assembly names to ensure that the same assembly will always have the same hash, and that different assemblies will have different hashes.
        /// </summary>
        /// <remarks>Do not use this method for security purposes. Only use to hash assembly names for identification.</remarks>
        /// <param name="input">The assembly name to hash.</param>
        /// <returns>A unique hash for the assembly name.</returns>
        public static string ComputeHashForAssemblyName(string input)
        {
            // we cache the hashes to avoid computing the hash multiple times for the same assembly.
            if (!_assemblyCached.TryGetValue(input, out string hashed))
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha256.ComputeHash(bytes);

                // we only need a short hash for the assembly name, so we take the first 12 characters of the hash. This should be enough to ensure uniqueness while keeping the hash short.
                hashed = BitConverter.ToString(hashBytes)
                    .Replace("-", "")
                    .Replace(".", "")
                    .ToLowerInvariant()[..12];

                _assemblyCached[input] = hashed;
            }

            return hashed;
        }
    }
}
