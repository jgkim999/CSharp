using Newtonsoft.Json;

namespace DemoDomain.Entities.Aws;
/*
{
    "DockerId": "ea32192c8553fbff06c9340478a2ff089b2bb5646fb718b4ee206641c9086d66",
    "Name": "curl",
    "DockerName": "ecs-curltest-24-curl-cca48e8dcadd97805600",
    "Image": "111122223333.dkr.ecr.us-west-2.amazonaws.com/curltest:latest",
    "ImageID": "sha256:d691691e9652791a60114e67b365688d20d19940dde7c4736ea30e660d8d3553",
    "Labels": {
        "com.amazonaws.ecs.cluster": "default",
        "com.amazonaws.ecs.container-name": "curl",
        "com.amazonaws.ecs.task-arn": "arn:aws:ecs:us-west-2:111122223333:task/default/8f03e41243824aea923aca126495f665",
        "com.amazonaws.ecs.task-definition-family": "curltest",
        "com.amazonaws.ecs.task-definition-version": "24"
    },
    "DesiredStatus": "RUNNING",
    "KnownStatus": "RUNNING",
    "Limits": {
        "CPU": 10,
        "Memory": 128
    },
    "CreatedAt": "2020-10-02T00:15:07.620912337Z",
    "StartedAt": "2020-10-02T00:15:08.062559351Z",
    "Type": "NORMAL",
    "LogDriver": "awslogs",
    "LogOptions": {
        "awslogs-create-group": "true",
        "awslogs-group": "/ecs/metadata",
        "awslogs-region": "us-west-2",
        "awslogs-stream": "ecs/curl/8f03e41243824aea923aca126495f665"
    },
    "ContainerARN": "arn:aws:ecs:us-west-2:111122223333:container/0206b271-b33f-47ab-86c6-a0ba208a70a9",
    "Networks": [
        {
            "NetworkMode": "awsvpc",
            "IPv4Addresses": [
                "10.0.2.100"
            ],
            "AttachmentIndex": 0,
            "MACAddress": "0e:9e:32:c7:48:85",
            "IPv4SubnetCIDRBlock": "10.0.2.0/24",
            "PrivateDNSName": "ip-10-0-2-100.us-west-2.compute.internal",
            "SubnetGatewayIpv4Address": "10.0.2.1/24"
        }
    ]
}
*/
public class EcsTaskMetaDataNetwork
{
    [JsonProperty("NetworkMode")]
    public string NetworkMode { get; set; } = string.Empty;
    
    [JsonProperty("IPv4Addresses")]
    public string[]? IPv4Addresses { get; set; }

    [JsonProperty("AttachmentIndex")]
    public int AttachmentIndex { get; set; }
    
    [JsonProperty("MACAddress")]
    public string MACAddress { get; set; } = string.Empty;
    
    [JsonProperty("IPv4SubnetCIDRBlock")]
    public string IPv4SubnetCIDRBlock { get; set; } = string.Empty;
    
    [JsonProperty("PrivateDNSName")]
    public string PrivateDNSName { get; set; } = string.Empty;

    [JsonProperty("SubnetGatewayIpv4Address")]
    public string SubnetGatewayIpv4Address { get; set; } = string.Empty;
}

public class EcsTaskMetaDataLimit
{
    [JsonProperty("CPU")]
    public int CPU { get; set; }
    
    [JsonProperty("Memory")]
    public int Memory { get; set; }
}

/// <summary>
/// https://docs.aws.amazon.com/ko_kr/AmazonECS/latest/developerguide/task-metadata-endpoint-v4.html
/// ${ECS_CONTAINER_METADATA_URI_V4}
/// var ecsMetaUrl = Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");
/// </summary>
public class EcsTaskMetadata
{
    [JsonProperty("DockerId")]
    public string DockerId { get; set; } = string.Empty;
    
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("DockerName")]
    public string DockerName { get; set; } = string.Empty;
    
    [JsonProperty("Image")]
    public string Image { get; set; } = string.Empty;
    
    [JsonProperty("ImageID")]
    public string ImageId { get; set; } = string.Empty;
    
    [JsonProperty("Labels")]
    public Dictionary<string, string> Labels { get; set; }  = new();
    
    [JsonProperty("DesiredStatus")]
    public string DesiredStatus { get; set; } = string.Empty;
    
    [JsonProperty("KnownStatus")]
    public string KnownStatus { get; set; } = string.Empty;

    [JsonProperty("Limits")]
    public EcsTaskMetaDataLimit Limits { get; set; } = new();
    
    [JsonProperty("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonProperty("StartedAt")]
    public DateTime StartedAt { get; set; }

    [JsonProperty("Type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonProperty("LogDriver")]
    public string LogDriver { get; set; } = string.Empty;

    [JsonProperty("LogOptions")]
    public Dictionary<string, string> LogOptions { get; set; } = new();
    
    [JsonProperty("ContainerARN")]
    public string ContainerARN { get; set; } = string.Empty;
    
    [JsonProperty("Networks")]
    public EcsTaskMetaDataNetwork[]? Networks { get; set; }
}
