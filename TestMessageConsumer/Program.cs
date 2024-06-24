using System;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using k8s;
using k8s.Models;

public class Message
{
    public string Spider { get; set; }
    public Dictionary<string, string> Input { get; set; }
    public Dictionary<string, string> Args { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        string rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "production-rabbitmqcluster";
        string queueName = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "scrapy_queue";

        var factory = new ConnectionFactory()
        {
            HostName = "production-rabbitmqcluster.default.svc.cluster.local",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        while (true)
        {
            var result = channel.BasicGet(queueName, autoAck: false);
            if (result == null)
            {
                Console.WriteLine("No messages in the queue");
                Thread.Sleep(30000);
                continue;
            }
            var uid = Guid.NewGuid().ToString();
            var body = result.Body.ToArray();
            var msg = JsonSerializer.Deserialize<Message>(body);
            if (msg == null)
            {
                Console.WriteLine("Failed to deserialize message");
                channel.BasicAck(result.DeliveryTag, multiple: false);
                continue;
            }

            var arglist = new List<string>();
            if (msg.Args != null)
            {
                foreach (var param in msg.Args)
                {
                    arglist.Add("-a");
                    arglist.Add($"{param.Key}={param.Value}");
                }
            }


            var config = KubernetesClientConfiguration.InClusterConfig();
            var client = new Kubernetes(config);

            var job = new V1Job
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
                Metadata = new V1ObjectMeta { Name = $"run-crashdocs-{uid}" },
                Spec = new V1JobSpec
                {
                    Template = new V1PodTemplateSpec
                    {
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "crashdocs",
                                    Image = "crashdocs:1.0",
                                    ImagePullPolicy = "Never",
                                    Args = arglist,
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar { Name = "SPIDER_INPUT", Value = JsonSerializer.Serialize(msg.Input) }
                                    }
                                }
                            },
                            RestartPolicy = "Never"
                        }
                    }
                }
            };

            // Create the job
            await client.CreateNamespacedJobAsync(job, "default");

            // Optionally, wait for the job to complete
            await WaitForJobCompletion(client, job);
            channel.BasicAck(result.DeliveryTag, multiple: false);

            // Retrieve logs
            var logs = await GetJobLogs(client, job);
            Console.WriteLine(logs);
        }
    }

    static async Task WaitForJobCompletion(IKubernetes client, V1Job job)
    {
        var finished = false;
        while (!finished)
        {
            var jobStatus = await client.BatchV1.ReadNamespacedJobAsync(job.Metadata.Name, "default");
            finished = jobStatus.Status.Succeeded.HasValue && jobStatus.Status.Succeeded.Value > 0;
            if (!finished)
            {
                if (jobStatus.Status.Failed.HasValue && jobStatus.Status.Failed.Value > 0)
                {
                    throw new Exception("Job failed");
                }
                await Task.Delay(1000); // Wait for 1 second before checking again
            }
        }
    }

    static async Task<string> GetJobLogs(IKubernetes client, V1Job job)
    {
        var pods = await client.CoreV1.ListNamespacedPodAsync("default", labelSelector: $"job-name={job.Metadata.Name}");
        var pod = pods.Items.FirstOrDefault();
        if (pod == null)
        {
            throw new Exception("Pod not found for job");
        }

        var log = await client.CoreV1.ReadNamespacedPodLogAsync(pod.Metadata.Name, "default");

        using (var reader = new StreamReader(log))
        {
            return reader.ReadToEnd();
        }

    }
}
