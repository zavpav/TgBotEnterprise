namespace RabbitMqInfrastructure
{
    /// <summary> Prefixes of queue for rabbit communication </summary>
    public static class RequestPrefixes
    {
        /// <summary> Direct request </summary>
        public static string DirectRequest = "Direct";

        /// <summary> Request </summary>
        public static string Response = "Response";

        /// <summary> Central hub </summary>
        public static string CentralPublisher = "CentralHub";

        /// <summary> Income query </summary>
        public static string IncomeData = "Income";
    }
}