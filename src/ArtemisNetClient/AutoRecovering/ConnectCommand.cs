namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class ConnectCommand
    {
        private ConnectCommand() { }
        public static readonly ConnectCommand Instance = new();
    }
}