using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElfScanServer.Common.Options;
using AElfScanServer.HttpApi.Options;
using Amazon.S3;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElfScanServer.Common.Provider;

public interface IK8sProvider
{
    Task<string> StartJob(string image, string chainId, string contractAddress, string contractName,
        string version);
}

public class K8sProvider : IK8sProvider
{
    private readonly ILogger<K8sProvider> _logger;
    private readonly IOptionsMonitor<SecretOptions> _secretOptions;
    private readonly IOptionsMonitor<GlobalOptions> _globalOptions;
    private readonly Kubernetes _client;
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

    public K8sProvider(ILogger<K8sProvider> logger,
        IOptionsMonitor<SecretOptions> secretOptions, IOptionsMonitor<GlobalOptions> globalOptions)
    {
        _logger = logger;
        _secretOptions = secretOptions;
        _globalOptions = globalOptions;
        _client = InitK8s3Client();
    }

    private Kubernetes InitK8s3Client()
    {
        var path = Environment.GetEnvironmentVariable("KUBECONFIG");


        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(path);

        return new Kubernetes(config);
    }

    public async Task<string> StartJob(string image, string chainId, string contractAddress, string contractName,
        string version)
    {
        string logPrefix =
            $"[ChainId:{chainId}-ContractAddress:{contractAddress}-ContractName:{contractName}-Version:{version}]";
        var jobName = "example-job-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        var jobNamespace = _globalOptions.CurrentValue.K8sNamespace;

        _logger.LogInformation($"{logPrefix} Creating job '{jobName}' with image '{image}'.");

        var job = CreateJobSpec(jobName, image, chainId, contractAddress, contractName, version);

        // Add serviceAccountName, affinity, and tolerations to the job spec
        job.Spec.Template.Spec.ServiceAccountName = "aelfscan-complier-sa";
        job.Spec.Template.Spec.Affinity = new V1Affinity
        {
            NodeAffinity = new V1NodeAffinity
            {
                RequiredDuringSchedulingIgnoredDuringExecution = new V1NodeSelector
                {
                    NodeSelectorTerms = new List<V1NodeSelectorTerm>
                    {
                        new V1NodeSelectorTerm
                        {
                            MatchExpressions = new List<V1NodeSelectorRequirement>
                            {
                                new V1NodeSelectorRequirement
                                {
                                    Key = "resource",
                                    OperatorProperty = "In", // Use string values for Operator
                                    Values = new List<string> { "aelfscan" }
                                },
                                new V1NodeSelectorRequirement
                                {
                                    Key = "app",
                                    OperatorProperty = "In", // Use string values for Operator
                                    Values = new List<string> { "aelfscan-complier" }
                                }
                            }
                        }
                    }
                }
            }
        };

        job.Spec.Template.Spec.Tolerations = new List<V1Toleration>
        {
            new V1Toleration
            {
                Key = "sandbox.gke.io/runtime",
                OperatorProperty = "Equal", // Use string values for Operator
                Value = "gvisor",
                Effect = "NoSchedule" // Use string values for Effect
            }
        };

        var createdJob = await _client.CreateNamespacedJobAsync(job, jobNamespace);

        _logger.LogInformation($"{logPrefix} Job '{createdJob.Metadata.Name}' created.");

        var podName = await GetPodNameForJob(_client, jobNamespace, createdJob.Metadata.Name, logPrefix);

        await WaitForPodToBeRunning(_client, jobNamespace, podName, logPrefix);
        await StreamPodLogs(_client, jobNamespace, podName, logPrefix);

        bool jobSucceeded = await WaitForJobCompletion(jobNamespace, createdJob.Metadata.Name, logPrefix);

        if (!jobSucceeded)
        {
            _logger.LogError($"{logPrefix} Job '{createdJob.Metadata.Name}' failed to complete successfully.");
            throw new Exception($"Job '{createdJob.Metadata.Name}' encountered an error.");
        }

        Task.Run(async () =>
        {
            await _client.DeleteNamespacedPodAsync(podName, jobNamespace, new V1DeleteOptions());
            _logger.LogInformation($"{logPrefix} Pod '{podName}' deleted asynchronously.");
        });

        return $"{logPrefix} Job '{createdJob.Metadata.Name}' completed successfully.";
    }


    private V1Job CreateJobSpec(string jobName, string image, string chainId, string contractAddress,
        string contractName, string version)
    {
        return new V1Job
        {
            Metadata = new V1ObjectMeta { Name = jobName },
            Spec = new V1JobSpec
            {
                Template = new V1PodTemplateSpec
                {
                    Spec = new V1PodSpec
                    {
                        RestartPolicy = "Never",
                        Containers = new[]
                        {
                            new V1Container
                            {
                                Name = "example-job-container",
                                Image = image,
                                Env = new[]
                                {
                                    new V1EnvVar
                                    {
                                        Name = "S3_BUCKET", Value = _globalOptions.CurrentValue.S3ContractFileBucket
                                    },
                                    new V1EnvVar
                                    {
                                        Name = "S3_DIRECTORY",
                                        Value = _globalOptions.CurrentValue.S3ContractFileDirectory
                                    },
                                    new V1EnvVar
                                        { Name = "S3_ACCESS_KEY", Value = _globalOptions.CurrentValue.S3AccessKey },
                                    new V1EnvVar
                                        { Name = "S3_SECRET_KEY", Value = _globalOptions.CurrentValue.S3SecretKey },
                                    new V1EnvVar { Name = "CONTRACT_VERSION_PARAM", Value = version },
                                    new V1EnvVar { Name = "CHAIN_ID", Value = chainId },
                                    new V1EnvVar { Name = "CONTRACT_NAME", Value = contractName },
                                    new V1EnvVar { Name = "CONTRACT_ADDRESS", Value = contractAddress }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private async Task<string> GetPodNameForJob(Kubernetes client, string namespaceName, string jobName,
        string logPrefix)
    {
        while (true)
        {
            var pods = await client.ListNamespacedPodAsync(namespaceName, labelSelector: $"job-name={jobName}");
            if (pods.Items.Count > 0)
            {
                _logger.LogInformation(
                    $"{logPrefix} Found Pod '{pods.Items[0].Metadata.Name}' for job '{jobName}'.");
                return pods.Items[0].Metadata.Name;
            }

            _logger.LogInformation($"{logPrefix} Waiting for Pod to start...");
            await Task.Delay(2000);
        }
    }

    private async Task WaitForPodToBeRunning(Kubernetes client, string namespaceName, string podName,
        string logPrefix)
    {
        _logger.LogInformation($"{logPrefix} Waiting for Pod '{podName}' to be in Running state...");
        var startTime = DateTime.UtcNow;

        while (true)
        {
            var pod = await client.ReadNamespacedPodAsync(podName, namespaceName);
            var containerStatus = pod.Status.ContainerStatuses?.FirstOrDefault();

            if (containerStatus?.State?.Running != null)
            {
                _logger.LogInformation($"{logPrefix} Pod '{podName}' is now in Running state.");
                break;
            }

            if (containerStatus?.State?.Waiting != null)
            {
                _logger.LogWarning(
                    $"{logPrefix} Pod '{podName}' is in {containerStatus.State.Waiting.Reason} state: {containerStatus.State.Waiting.Message}");
            }

            if (DateTime.UtcNow - startTime > _timeout)
            {
                _logger.LogError($"{logPrefix} Timeout waiting for Pod '{podName}' to enter Running state.");
                throw new TimeoutException("Timeout waiting for Pod to be Running.");
            }

            await Task.Delay(2000);
        }
    }

    private async Task<bool> WaitForJobCompletion(string namespaceName, string jobName, string logPrefix)
    {
        var startTime = DateTime.UtcNow;

        while (true)
        {
            var jobStatus = await _client.ReadNamespacedJobStatusAsync(jobName, namespaceName);

            if (jobStatus.Status.Succeeded.HasValue && jobStatus.Status.Succeeded.Value > 0)
            {
                _logger.LogInformation($"{logPrefix} Job '{jobName}' completed successfully.");
                return true;
            }

            if (jobStatus.Status.Failed.HasValue && jobStatus.Status.Failed.Value > 0)
            {
                _logger.LogError($"{logPrefix} Job '{jobName}' failed with status.");
                return false;
            }

            if (DateTime.UtcNow - startTime > _timeout)
            {
                _logger.LogError($"{logPrefix} Timeout waiting for Job '{jobName}' to complete.");
                throw new TimeoutException("Timeout waiting for Job completion.");
            }

            await Task.Delay(2000);
        }
    }

    private async Task StreamPodLogs(Kubernetes client, string namespaceName, string podName, string logPrefix)
    {
        _logger.LogInformation($"{logPrefix} Starting to stream logs for Pod '{podName}'...");

        using var podLogs = client.ReadNamespacedPodLogAsync(podName, namespaceName, follow: true);
        using var reader = new StreamReader(await podLogs);

        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            _logger.LogInformation($"{logPrefix} [Pod Log] {line}");
        }
    }
}