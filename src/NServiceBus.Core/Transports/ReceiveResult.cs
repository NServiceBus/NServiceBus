﻿namespace NServiceBus.Transport
{
    /// <summary>
    /// The result of receiving a message.
    /// </summary>
    public enum ReceiveResult
    {
        /// <summary>
        /// The message was processed.
        /// </summary>
        Processed,

        /// <summary>
        /// The message was discarded.
        /// </summary>
        Discarded,

        /// <summary>
        /// The message was moved to the error queue.
        /// </summary>
        MovedToErrorQueue,

        /// <summary>
        /// The message was queued for a delayed retry.
        /// </summary>
        QueuedForDelayedRetry,

        /// <summary>
        /// The message expired and was ignored.
        /// </summary>
        Expired
    }
}