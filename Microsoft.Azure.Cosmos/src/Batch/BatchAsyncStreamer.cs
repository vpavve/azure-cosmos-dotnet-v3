//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeed.Pagination;
    using Microsoft.Azure.Cosmos.Tracing;

    /// <summary>
    /// Handles operation queueing and dispatching.
    /// Fills batches efficiently and maintains a timer for early dispatching in case of partially-filled batches and to optimize for throughput.
    /// </summary>
    /// <remarks>
    /// There is always one batch at a time being filled. Locking is in place to avoid concurrent threads trying to Add operations while the timer might be Dispatching the current batch.
    /// The current batch is dispatched and a new one is readied to be filled by new operations, the dispatched batch runs independently through a fire and forget pattern.
    /// </remarks>
    /// <seealso cref="BatchAsyncBatcher"/>
    internal class BatchAsyncStreamer : IDisposable
    {
        private readonly object dispatchLimiter = new object();
        private readonly int maxBatchOperationCount;
        private readonly int maxBatchByteSize;
        private readonly BatchAsyncBatcherExecuteDelegate executor;
        private readonly BatchAsyncBatcherRetryDelegate retrier;
        private readonly CosmosSerializerCore serializerCore;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly BatchPartitionMetric oldPartitionMetric;
        private readonly BatchPartitionMetric partitionMetric;
        private readonly CosmosClientContext clientContext;

        private volatile BatchAsyncBatcher currentBatcher;

        private TimerWheelTimer congestionControlTimer;

        public BatchAsyncStreamer(
            int maxBatchOperationCount,
            int maxBatchByteSize,
            CosmosSerializerCore serializerCore,
            BatchAsyncBatcherExecuteDelegate executor,
            BatchAsyncBatcherRetryDelegate retrier,
            CosmosClientContext clientContext)
        {
            if (maxBatchOperationCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBatchOperationCount));
            }

            if (maxBatchByteSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBatchByteSize));
            }

            if (executor == null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            if (retrier == null)
            {
                throw new ArgumentNullException(nameof(retrier));
            }

            if (serializerCore == null)
            {
                throw new ArgumentNullException(nameof(serializerCore));
            }

            this.maxBatchOperationCount = maxBatchOperationCount;
            this.maxBatchByteSize = maxBatchByteSize;
            this.executor = executor;
            this.retrier = retrier;
            this.serializerCore = serializerCore;
            this.clientContext = clientContext;
            this.currentBatcher = this.CreateBatchAsyncBatcher();

            this.oldPartitionMetric = new BatchPartitionMetric();
            this.partitionMetric = new BatchPartitionMetric();
        }

        public void Add(ItemBatchOperation operation)
        {
            BatchAsyncBatcher toDispatch = null;
            lock (this.dispatchLimiter)
            {
                // TODO: Might throw NRE
                while (!this.currentBatcher.TryAdd(operation))
                {
                    // Batcher is full
                    toDispatch = this.GetBatchToDispatchAndCreate();
                }
            }

            if (toDispatch != null)
            {
                // Discarded for Fire & Forget
                _ = toDispatch.DispatchAsync(this.partitionMetric, this.cancellationTokenSource.Token);
            }
        }

        public void FlushAndClose()
        {
            BatchAsyncBatcher toDispatch = null;
            lock (this.dispatchLimiter)
            {
                toDispatch = this.currentBatcher;
                this.currentBatcher = null;
            }

            if (toDispatch != null)
            {
                // Discarded for Fire & Forget
                _ = toDispatch.DispatchAsync(this.partitionMetric, this.cancellationTokenSource.Token);
            }
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();

            if (this.congestionControlTimer != null)
            {
                this.congestionControlTimer.CancelTimer();
                this.congestionControlTimer = null;
            }
        }

        private BatchAsyncBatcher GetBatchToDispatchAndCreate()
        {
            if (this.currentBatcher.IsEmpty)
            {
                return null;
            }

            BatchAsyncBatcher previousBatcher = this.currentBatcher;
            this.currentBatcher = this.CreateBatchAsyncBatcher();
            return previousBatcher;
        }

        private BatchAsyncBatcher CreateBatchAsyncBatcher()
        {
            return new BatchAsyncBatcher(this.maxBatchOperationCount, this.maxBatchByteSize, this.serializerCore, this.executor, this.retrier, this.clientContext);
        }
    }
}
