using Amazon;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.DependencyInjection;


namespace LambdaAnnotationsWithPowertools;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    { 
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonSimpleSystemsManagement>();
        services.AddAWSService<IAmazonSecretsManager>();
        services.AddAWSService<IAmazonCloudWatch>();
    }
}
