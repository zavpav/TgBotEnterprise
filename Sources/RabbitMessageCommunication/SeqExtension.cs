using System;
using RabbitMessageCommunication;
using Serilog;

namespace CommonInfrastructure
{
    public static class SeqExtension
    {
        public const string IncomeMessageIdEnrichName = "SystemEventId";


        public static void InformationWithEventContext(this ILogger logger,
            string? eventIdContext,
            string templateMessage,
            params object?[] parts
        )
        {
            if (string.IsNullOrEmpty(eventIdContext))
            {
                logger.Information(templateMessage, parts);
            }
            else
                logger.ForContext(IncomeMessageIdEnrichName, eventIdContext)
                    .Information(templateMessage, parts);
        }

        public static void Information(this ILogger logger,
            IRabbitMessage processingMessage, 
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId) 
                .Information(templateMessage, parts);
        }

        public static void Information(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Information(exception, templateMessage, parts);
        }

        public static void Warning(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Warning(templateMessage, parts);
        }
        public static void Warning(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Warning(exception, templateMessage, parts);
        }


        public static void ErrorWithEventContext(this ILogger logger,
            string? eventIdContext,
            Exception exception,
            string templateMessage,
            params object?[] parts
        )
        {
            if (string.IsNullOrEmpty(eventIdContext))
            {
                logger.Error(exception, templateMessage, parts);
            }
            else
                logger.ForContext(IncomeMessageIdEnrichName, eventIdContext)
                    .Error(exception, templateMessage, parts);
        }

        public static void Error(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Error(templateMessage, parts);
        }

        public static void Error(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Error(exception, templateMessage, parts);
        }

        public static void Fatal(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Fatal(templateMessage, parts);
        }
        public static void Fatal(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object?[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.SystemEventId)
                .Fatal(exception, templateMessage, parts);
        }

    }
}