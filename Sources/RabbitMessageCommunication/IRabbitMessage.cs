namespace RabbitMessageCommunication
{
    /// <summary> Required fields for rabbit messages </summary>
    public interface IRabbitMessage
    {
        /// <summary> Message Income Id - Unique id in all services </summary>
        string IncomeId { get; }
    }
}