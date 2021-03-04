//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Azure.Documents;

    /// <summary>
    /// Item Opertaion representation
    /// </summary>
    public class ItemOperation
    {
        /// <summary>
        /// Partition key
        /// </summary>
        public PartitionKey PartitionKey { get; set; }

        public string RawPartitionKey { get; set; }

        /// <summary>
        /// Item identifier
        /// </summary>
        public string Id { get; set; }

        internal OperationType OperationType { get; set; }

        /// <summary>
        /// Read Factory 
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="id"></param>
        /// <returns>ItemOperation</returns>
        public static ItemOperation Read(string partitionKey,
            string id)
        {
            return new ItemOperation()
            {
                PartitionKey = new PartitionKey(partitionKey),
                RawPartitionKey = partitionKey,
                Id = id,
                OperationType = OperationType.Read,
            };
        }
    }
}
