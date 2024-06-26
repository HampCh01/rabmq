using System.Text.Json;
using RabbitMQ.Client;
using k8s;
using k8s.Models;
using C3MSFramework;

public class Message
{
    public string Spider { get; set; }
    public Dictionary<string, string> Input { get; set; }
    public Dictionary<string, string> Args { get; set; }
}

class Program
{
    static readonly string RABBIT_HOST = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? string.Empty;
    static readonly string RABBIT_PORT = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? string.Empty;
    static readonly string QUEUE_NAME = Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? string.Empty;
    static readonly string IMAGE_NAME = Environment.GetEnvironmentVariable("IMAGE_NAME") ?? string.Empty;
    static readonly string RABBIT_USER = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? string.Empty;
    static readonly string RABBIT_PASS = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? string.Empty;
    static async Task Main(string[] args)
    {
        var factory = new ConnectionFactory()
        {
            HostName = RABBIT_HOST,
            Port = RABBIT_PORT == string.Empty ? 5672 : int.Parse(RABBIT_PORT),
            UserName = RABBIT_USER,
            Password = RABBIT_PASS
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: QUEUE_NAME, durable: false, exclusive: false, autoDelete: false, arguments: null);

        while (true)
        {
            var result = channel.BasicGet(QUEUE_NAME, autoAck: false);
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


            var config = KubernetesClientConfiguration.InClusterConfig();
            var client = new Kubernetes(config);
            var biname = IMAGE_NAME.Split(':')[0];

            var job = new V1Job
            {
                ApiVersion = "batch/v1",
                Kind = "Job",
                Metadata = new V1ObjectMeta { Name = $"run-{biname}-{uid}" },
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
                                    Name = biname,
                                    Image = IMAGE_NAME,
                                    ImagePullPolicy = "Never",
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar { Name = "SPIDER_INPUT", Value = JsonSerializer.Serialize(msg.Input) },
                                        new V1EnvVar { Name = "FTP_PATH", Value = msg.Args["spiderarg2"] }
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
