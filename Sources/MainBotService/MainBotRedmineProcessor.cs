namespace MainBotService
{
    public partial class MainBotWorker
    {
        public class RedmineProcessor
        {
            private readonly MainBotWorker _owner;

            public RedmineProcessor(MainBotWorker owner)
            {
                this._owner = owner;
            }

        }


    }
}