//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Item Opertaion representation
    /// </summary>
    public class ItemOperation
    {
        /// <summary>
        /// Partition key
        /// </summary>
        public PartitionKey PartitionKey { get; set; }

        /// <summary>
        /// Item identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Read Factory 
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="id"></param>
        /// <returns>ItemOperation</returns>
        public ItemOperation Read(PartitionKey partitionKey,
            string id)
        {
            return new ItemOperation()
            {
                PartitionKey = partitionKey,
                Id = id,
            };
        }
    }
}
