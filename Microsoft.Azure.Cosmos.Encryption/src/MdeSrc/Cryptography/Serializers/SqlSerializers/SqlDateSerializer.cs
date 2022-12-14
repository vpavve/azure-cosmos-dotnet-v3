//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// This file isn't generated, but this comment is necessary to exclude it from StyleCop analysis.
// <auto-generated/>

using System;
using System.Linq;
using static System.BitConverter;

namespace Microsoft.Data.Encryption.Cryptography.Serializers
{
    /// <summary>
    /// Contains the methods for serializing and deserializing <see cref="DateTime"/> type data objects
	/// that is compatible with the Always Encrypted feature in SQL Server and Azure SQL.
    /// </summary>
    internal sealed class SqlDateSerializer : Serializer<DateTime>
    {
        private const int SizeOfDate = 3;

        /// <summary>
        /// The <see cref="Identifier"/> uniquely identifies a particular Serializer implementation.
        /// </summary>
        public override string Identifier => "SQL_Date";

        /// <summary>
        /// Deserializes the provided <paramref name="bytes"/>
        /// </summary>
        /// <param name="bytes">The data to be deserialized</param>
        /// <returns>The serialized data</returns>
        /// <exception cref="MicrosoftDataEncryptionException">
        /// <paramref name="bytes"/> is null.
        /// -or-
        /// The length of <paramref name="bytes"/> is less than 3.
        /// </exception>
        public override DateTime Deserialize(byte[] bytes)
        {
            bytes.ValidateNotNull(nameof(bytes));
            bytes.ValidateSize(SizeOfDate, nameof(bytes));

            byte[] padding = { 0 };
            byte[] bytesWithPadding = bytes.Concat(padding).ToArray();
            int days = ToInt32(bytesWithPadding, 0);
            return DateTime.MinValue.AddDays(days);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to be serialized</param>
        /// <returns>
        /// An array of bytes with length 3.
        /// </returns>
        public override byte[] Serialize(DateTime value)
        {
            int days = value.Subtract(DateTime.MinValue).Days;
            return GetBytes(days).Take(SizeOfDate).ToArray();
        }
    }
}
