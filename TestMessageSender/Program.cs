using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using System.Data;
using TestMessageSender;
using TestMessageSender.Models;
using System.Collections.Generic;
using System.Reflection;

public class Message
{
    public string Spider { get; set; }
    public DMInputModel Input { get; set; }
    public Dictionary<string, string> Args { get; set; }
}

class Program
{
    private static readonly string _rabHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
    private static readonly string _rabUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? string.Empty;
    private static readonly string _rabPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? string.Empty;
    private static readonly string _rabQueue = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "scrapy_queue";
    private static readonly ConnectionFactory factory = new() { HostName = _rabHost, UserName = _rabUser, Password = _rabPass};

    static void Main(string[] args)
    {
        var openRequests = new OpenRequests();
        var reqs = openRequests.GetOpenRecords();
        foreach(var req in reqs)
        {
            SendMessage(req);
        }
    }

    static void SendMessage(OpenRequestModel req)
    {
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _rabQueue, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var message = new Message
        {
            Spider = req.CourtID switch {
                15143 => "Crashdocs",
                _ => "Crashdocs"
            },
            Input = CreateInput(req),
            Args = new Dictionary<string, string>
            {
                { "spiderarg2", "/Scrapy/PoliceReports/Crashdocs/Test/" }
            }
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        channel.BasicPublish(exchange: "", routingKey: _rabQueue, basicProperties: null, body: body);
        Console.WriteLine(" [x] Sent {0}", message);
    }

    static DMInputModel CreateInput(OpenRequestModel req)
    {
        var inputModel = new DMInputModel();
        var reqContentProperties = req.ReqContent.ReqContent.GetType().GetProperties();
        var inputModelProperties = typeof(DMInputModel).GetProperties();

        foreach (var reqProp in reqContentProperties)
        {
            foreach (var inputProp in inputModelProperties)
            {
                if (reqProp.Name == inputProp.Name && inputProp.CanWrite)
                {
                    inputProp.SetValue(inputModel, reqProp.GetValue(req.ReqContent.ReqContent, null), null);
                    break;
                }
            }
        }

        // Manually handle any properties that do not match directly
        inputModel.RequestID = req.RequestID.ToString();

        return inputModel;
    }
}
