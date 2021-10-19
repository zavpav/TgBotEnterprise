namespace MainBotService.RabbitCommunication
{
    public partial class MainBotService
    {
        public class RedmineProcessor
        {
            private readonly MainBotService _owner;

            public RedmineProcessor(MainBotService owner)
            {
                this._owner = owner;
            }

        }


    }
}