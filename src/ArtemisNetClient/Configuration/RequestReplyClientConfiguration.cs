namespace ActiveMQ.Artemis.Client;

public class RequestReplyClientConfiguration
{
    internal string Address { get; set; }
    internal string ReplyToAddress { get; set; }
    public int Credit { get; set; } = 200;
}