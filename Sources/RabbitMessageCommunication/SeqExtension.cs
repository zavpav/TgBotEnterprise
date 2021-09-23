using System;
using RabbitMessageCommunication;
using Serilog;

namespace CommonInfrastructure
{
    public static class SeqExtension
    {
        public const string IncomeMessageIdEnrichName = "IncomeMessageId";

        public static void Information(this ILogger logger,
            IRabbitMessage processingMessage, 
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId) 
                .Information(templateMessage, parts);
        }

        public static void Information(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Information(exception, templateMessage, parts);
        }

        public static void Warning(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Warning(templateMessage, parts);
        }
        public static void Warning(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Warning(exception, templateMessage, parts);
        }

        public static void Error(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Error(templateMessage, parts);
        }
        public static void Error(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Error(exception, templateMessage, parts);
        }

        public static void Fatal(this ILogger logger,
            IRabbitMessage processingMessage,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Fatal(templateMessage, parts);
        }
        public static void Fatal(this ILogger logger,
            IRabbitMessage processingMessage,
            Exception exception,
            string templateMessage,
            params object[] parts
        )
        {
            logger.ForContext(IncomeMessageIdEnrichName, processingMessage.IncomeId)
                .Fatal(exception, templateMessage, parts);
        }

    }
}