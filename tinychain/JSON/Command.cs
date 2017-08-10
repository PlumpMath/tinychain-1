namespace tinychain
{
    class Command
    {
        public string command;
        public object data;

        public Command(string command)
        {
            this.command = command;
        }
    }
}
