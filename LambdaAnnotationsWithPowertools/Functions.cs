using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.SimpleSystemsManagement;
using LambdaAnnotationsWithPowertools.Support;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.Model.Internal.MarshallTransformations;
using System.Runtime.CompilerServices;
using Amazon.S3;
using System.Text;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaAnnotationsWithPowertools;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public class Functions
{
    const string ROLE = "arn:aws:iam::425173371283:role/lambda-role";

    /// <summary>
    /// Default constructor.
    /// </summary>
    public Functions()
    {
        AWSSDKHandler.RegisterXRayForAllServices();
    }

    /// <summary>
    /// Root route that provides information about the other requests that can be made.
    /// </summary>
    /// <returns>API descriptions.</returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "DefaultFunction")]
    [HttpApi(LambdaHttpMethod.Get, "/")]
    public string Default()
    {
        var docs = @"Here are the functions that you can call:


";
        return docs;
    }


    #region Systems Manager

    /// <summary>
    /// Reads a Value from SSM Parameter Store
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ssmClient"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "ReadParameter")]
    [HttpApi(LambdaHttpMethod.Get, "/readparameter")]
    public async Task<string> ReadParameter(ILambdaContext context, [FromServices] IAmazonSimpleSystemsManagement ssmClient)
    {
        var parameterValue = await ssmClient.GetParameterAsync(new()
        {
            Name = GetEnv("PARAMETER")
        });
        return parameterValue.Parameter.Value;
    }

    /// <summary>
    /// Updates SSM Parameter Store Value
    /// </summary>
    /// <param name="value"></param>
    /// <param name="context"></param>
    /// <param name="ssmClient"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "WriteParameter")]
    [HttpApi(LambdaHttpMethod.Post, "/writeparameter")]
    public async Task<string> WriteParameter([FromBody] string value, ILambdaContext context, [FromServices] IAmazonSimpleSystemsManagement ssmClient)
    {
        var parameterValue = await ssmClient.PutParameterAsync(new()
        {
            Name = GetEnv("PARAMETER"),
            Value = value,
            Overwrite = true
        });
        return $"{GetEnv("PARAMETER")} => {value}";
    }

    #endregion

    #region DynamoDB

    /// <summary>
    /// Writes a value to a DynamoDB Table
    /// </summary>
    /// <param name="item"></param>
    /// <param name="ctx"></param>
    /// <param name="dbClient"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "WriteTableItem")]
    [HttpApi(LambdaHttpMethod.Post, "/writetableitem")]
    public async Task<TableItem> WriteTableItem([FromBody] TableItem item, ILambdaContext ctx, [FromServices] IAmazonDynamoDB dbClient)
    {
        Metrics.AddMetric("AmountAdded", item.Amount, MetricUnit.Count);

        var result = await dbClient.PutItemAsync(new()
        {
            TableName = GetEnv("TABLE"),
            Item = new() {
                { "id", new AttributeValue { S = item.Id } },
                { "value", new AttributeValue { S = item.Value } },
                { "amount", new AttributeValue { N = item.Amount.ToString() }
            }

        }
        });

        return item;
    }

    /// <summary>
    /// Reads a value from DynamoDB
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ctx"></param>
    /// <param name="dbClient"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "ReadTableItem")]
    [HttpApi(LambdaHttpMethod.Get, "/readtableitem/{id}")]
    public async Task<TableItem> ReadTableItem(string id, ILambdaContext ctx, [FromServices] IAmazonDynamoDB dbClient)
    {
        var result = await dbClient.GetItemAsync(new()
        {
            TableName = GetEnv("TABLE"),
            Key = new()
            {
                { "id", new AttributeValue { S = id } }

            }
        });

        if (result.Item is null)
        {
            return new TableItem
            {
                Id = id,
                Value = "ITEM NOT FOUND",
                Amount = -1
            };
        }

        return new TableItem
        {
            Id = result.Item["id"].S,
            Value = result.Item["value"].S,
            Amount = int.Parse(result.Item["amount"].N)
        };

    }

    #endregion

    #region S3

    /// <summary>
    /// Write Text to a file in S3
    /// </summary>
    /// <param name="item"></param>
    /// <param name="context"></param>
    /// <param name="s3Client"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "WriteS3")]
    [HttpApi(LambdaHttpMethod.Post, "/writeS3item")]
    public async Task<string> WriteS3Item([FromBody]S3Item item, ILambdaContext context, [FromServices] IAmazonS3 s3Client)
    {
        Logger.LogInformation(item);

        var result = await s3Client.PutObjectAsync(new() 
        {
            BucketName = GetEnv("BUCKET"),
            Key = item.Key,
            ContentBody= item.Content
        });

        return $"{item.Key} created.";
    }

    /// <summary>
    /// Read value from S3
    /// </summary>
    /// <param name="key"></param>
    /// <param name="context"></param>
    /// <param name="s3Client"></param>
    /// <returns></returns>
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Logging]
    [LambdaFunction(MemorySize = 1024, Role = ROLE, ResourceName = "ReadS3")]
    [HttpApi(LambdaHttpMethod.Get, "/readS3item/{key}")]
    public async Task<string> ReadS3Item(string key, ILambdaContext context, [FromServices] IAmazonS3 s3Client)
    {
        string content;

        var result = await s3Client.GetObjectAsync(new() 
        {
            BucketName = GetEnv("BUCKET"),
            Key = key,
        });

        Tracing.WithSubsegment("loggingResponse", async (subsegment) => {
            await Task.Delay(DateTime.Now.Millisecond);
        });

        if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
           return "Not Found!";
        }

        using(MemoryStream str = new())
        {
            result.ResponseStream.CopyTo(str);
            content = Encoding.ASCII.GetString(str.ToArray());
        }
        return content;

    }


    #endregion

    private string GetEnv(string name) => Environment.GetEnvironmentVariable(name);

}