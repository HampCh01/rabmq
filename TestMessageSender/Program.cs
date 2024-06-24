using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

public class Message
{
    public string Spider { get; set; }
    public Dictionary<string, string> Params { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 100; i++)
        {
            SendMessage(i);
        }
    }

    static void SendMessage(int iteration)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "scrapy_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var message = new Message
        {
            Spider = "example_spider",
            Params = new Dictionary<string, string>
            {
                { "input", "SpiderME" },
                { "spiderarg2", iteration.ToString() }
            }
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        channel.BasicPublish(exchange: "", routingKey: "scrapy_queue", basicProperties: null, body: body);
        Console.WriteLine(" [x] Sent {0}", message);
    }
}
