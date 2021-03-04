namespace NServiceBus.TransportTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    public class When_sending_from_on_error : NServiceBusTransportTest
    {
        [TestCase(TransportTransactionMode.None)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.TransactionScope)]
        public async Task Should_dispatch_the_message(TransportTransactionMode transactionMode)
        {
            var completed = new TaskCompletionSource<bool>();
            OnTestTimeout(() => completed.SetCanceled());

            await StartPump(
                (context, _) =>
                {
                    if (context.Headers.ContainsKey("FromOnError"))
                    {
                        return Task.CompletedTask;
                    }

                    throw new Exception("Simulated exception");
                },
                async (context, _) =>
                {
                    await SendMessage(InputQueueName, new Dictionary<string, string> { { "FromOnError", "true" } }, context.TransportTransaction);
                    return ErrorHandleResult.Discarded;
                },
                (context, _) => context.Result == ReceiveResult.Processed ? completed.SetCompleted() : Task.CompletedTask,
                transactionMode);

            await SendMessage(InputQueueName);

            _ = await completed.Task;
        }
    }
}