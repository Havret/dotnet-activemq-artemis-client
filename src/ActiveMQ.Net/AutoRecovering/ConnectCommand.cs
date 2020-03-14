namespace ActiveMQ.Net.AutoRecovering
{
    internal class ConnectCommand
    {
        private ConnectCommand() { }
        public static readonly ConnectCommand Instance = new ConnectCommand();
    }
}