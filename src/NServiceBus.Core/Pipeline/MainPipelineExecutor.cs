namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IServiceProvider rootBuilder, IPipelineCache pipelineCache, MessageOperations messageOperations, INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification, Pipeline<ITransportReceiveContext> receivePipeline)
        {
            this.rootBuilder = rootBuilder;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.receivePipelineNotification = receivePipelineNotification;
            this.receivePipeline = receivePipeline;
        }

        public async Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
        {
            var pipelineStartedAt = DateTimeOffset.UtcNow;

            using (var childScope = rootBuilder.CreateScope())
            {
                var message = new IncomingMessage(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

                var rootContext = new RootContext(childScope.ServiceProvider, messageOperations, pipelineCache, cancellationToken);
                rootContext.Extensions.Merge(messageContext.Extensions);

                var transportReceiveContext = new TransportReceiveContext(message, messageContext.TransportTransaction, rootContext);

                try
                {
                    await receivePipeline.Invoke(transportReceiveContext).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    e.Data["Message ID"] = message.MessageId;
                    if (message.NativeMessageId != message.MessageId)
                    {
                        e.Data["Transport message ID"] = message.NativeMessageId;
                    }

                    throw;
                }

                await receivePipelineNotification.Raise(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTimeOffset.UtcNow), cancellationToken).ConfigureAwait(false);
            }
        }

        readonly IServiceProvider rootBuilder;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly INotificationSubscriptions<ReceivePipelineCompleted> receivePipelineNotification;
        readonly Pipeline<ITransportReceiveContext> receivePipeline;
    }
}