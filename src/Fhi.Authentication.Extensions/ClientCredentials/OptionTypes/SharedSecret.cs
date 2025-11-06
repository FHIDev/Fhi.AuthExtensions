
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents a shared secret used for secure communication or authentication purposes.
    /// </summary>
    /// <remarks>The <see cref="SharedSecret"/> struct provides implicit conversion to and from <see
    /// cref="string"/>, allowing seamless integration with APIs that require a string representation of the
    /// secret.</remarks>
    public struct SharedSecret
    {
        private readonly string _sharedSecret;
        ///<inheritdoc/>
        private SharedSecret(string sharedSecret)
        {
            _sharedSecret = sharedSecret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static SharedSecret Parse(string secret)
        {
            return new SharedSecret(secret);
        }

        /// <summary>
        /// Converts a <see cref="SharedSecret"/> instance to its string representation.
        /// </summary>
        /// <param name="sharedSecret">The <see cref="SharedSecret"/> instance to convert.</param>
        public static implicit operator string(SharedSecret sharedSecret) => sharedSecret._sharedSecret;
        //public static implicit operator SharedSecret(string sharedSecret) => new SharedSecret(sharedSecret);
    }

}
