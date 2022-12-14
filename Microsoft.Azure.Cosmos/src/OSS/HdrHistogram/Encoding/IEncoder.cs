// ------------------------------------------------------------
// The code in this repository code was written by Lee Campbell, as a
// derived work from the original Java by Gil Tene of Azul Systems and
// Michael Barker, and released to the public domain, as explained
// at http://creativecommons.org/publicdomain/zero/1.0/
// ------------------------------------------------------------

// This file isn't generated, but this comment is necessary to exclude it from StyleCop analysis.
// <auto-generated/>

using HdrHistogram.Utilities;

namespace HdrHistogram.Encoding
{
    /// <summary>
    /// Defines a method to allow histogram data to be encoded into a <see cref="ByteBuffer"/>.
    /// </summary>
    internal interface IEncoder
    {
        /// <summary>
        /// Encodes the supplied <see cref="IRecordedData"/> into the supplied <see cref="ByteBuffer"/>.
        /// </summary>
        /// <param name="data">The data to encode.</param>
        /// <param name="buffer">The target <see cref="ByteBuffer"/> to write to.</param>
        /// <returns>The number of bytes written.</returns>
        int Encode(IRecordedData data, ByteBuffer buffer);
    }
}