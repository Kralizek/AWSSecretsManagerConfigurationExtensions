[![Build status](https://ci.appveyor.com/api/projects/status/28xqpt2i138mg44v/branch/master?svg=true)](https://ci.appveyor.com/project/Kralizek/awssecretsmanagerconfigurationextensions/branch/master) [![NuGet version](https://img.shields.io/nuget/vpre/Kralizek.Extensions.Configuration.AWSSecretsManager.svg)](https://www.nuget.org/packages/Kralizek.Extensions.Configuration.AWSSecretsManager)

This repository contains a provider for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/) that retrieves secrets stored in [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/).

## Overview

Every application has some kind of setting that should never be checked into the source control like a database connection string or some external API credentials. Yet, your application needs that setting to be able to properly perform its job.

.NET Core natively supports the ingestion of settings from different sources. This allows the customization of the application according to the current environment. The typical example is the connection string to a database that can vary so that each environment can connect to a specific database.

Developers working on .NET Core often take advantage of the [secret manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) for their development environment. On the other hand, settings for the production environment are often stored in environment variables.

[AWS Secrets Manager](https://aws.amazon.com/secrets-manager/) offers a serverless managed solution to the problem.

[Kralizek.Extensions.Configuration.AWSSecretsManager](https://www.nuget.org/packages/Kralizek.Extensions.Configuration.AWSSecretsManager) offers an convenient method to access your secrets stored in AWS Secrets Manager. 

This is how your ASP.NET Core 2.0 application will look like. Notice the `config.AddSecretsManager();` in the delegate passed to the `ConfigureAppConfiguration` method.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>  
                {
                    config.AddSecretsManager();
                })
                .UseStartup<Startup>()
                .Build();
}
```

This code is also available in [this sample](/samples/SampleWeb).

You can use `AddSecretsManager` also in a classic console application.

```csharp
static void Main(string[] args)
{
    var builder = new ConfigurationBuilder();
    builder.AddSecretsManager();

    var configuration = builder.Build();

    Console.WriteLine("Hello World!");
}
```

This code is also available in [this sample](/samples/Sample1).

**Note**: the snippets above assume that some [AWS credentials are available by default](https://aws.amazon.com/blogs/security/a-new-and-standardized-way-to-manage-credentials-in-the-aws-sdks/) to your application. [Here](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-creds.html) you can see how to setup your environment.

## Community posts

* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 1)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-1/) by Andrew Lock
* [Secure secrets storage for ASP.NET Core with AWS Secrets Manager (Part 2)](https://andrewlock.net/secure-secrets-storage-for-asp-net-core-with-aws-secrets-manager-part-2/) by Andrew Lock
* [Useful tools to manage your application's secrets](https://raygun.com/blog/manage-application-secrets/) by Jerrie Pelser

## Customization

This library offers the possibility to customize how the setting values are retrieved from AWS Secrets Manager and added to the Configuration provider.

### AWS Credentials

By default, this library let the AWS SDK decide which credentials should be used according to available settings. You can customize that by providing your own set of credentials.

Here are some samples.

#### Basic credentials

You can provide your AWS access and secret key directly by using the `BasicAWSCredentials` class.

**Note** You should avoid this. After all, the intent of this library is to remove our secrets from the source code.

```csharp
var credentials = new BasicAWSCredentials("my-accessKey", "my-secretKey");
builder.AddSecretsManager(credentials: credentials);
```

#### Using credentials connected to a profile (old)

You can use a specific profile by using the `StoredProfileAWSCredentials` class.

**Note** The `StoredProfileAWSCredentials` has been marked as obsolete and will be removed in a later release.

```csharp
var credentials = new StoredProfileAWSCredentials("my_profile_name");
builder.AddSecretsManager(credentials: credentials);
```

#### Using credentials connected to a profile (current)

You can use the `CredentialProfileStoreChain` class to fetch a profile from the different sources available.

```csharp
var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();

if (chain.TryGetAWSCredentials("my_profile_name", out var credentials))
{
    builder.AddSecretsManager(credentials);
}
```

You can see an example [here](/samples/Sample3).

### AWS Region

By default, this library fetches the secrets registered in the AWS region associated with the default profile. You can change that by passing the desired region.

```csharp
builder.AddSecretsManager(region: RegionEndpoint.EUWest1);
```

You can see an example [here](/samples/Sample2).

### Filtering secrets before they are retrieved

Best practices suggest that you use IAM roles to restrict the list of secrets that your application has access to. This is not always doable, especially in older setups (e.g. multiple applications sitting on the same EC2 instance sharing the permissions via EC2 instance profile).

In this case, it's still possible to restrict which secrets should be retrieved by your application by providing a predicate to be applied on each secret returned.

**Note** Retrieving the list of available secrets and their secret value happens in two different moments so you can prevent your application from ever accessing the value of the secrets you don't need.

```csharp
var acceptedARNs = new[]
{
    "MySecretARN1",
    "MySecretARN2",
    "MySecretARN3",
};

builder.AddSecretsManager(configurator: options =>
{
    options.SecretFilter = entry => acceptedARNs.Contains(entry.ARN);
});
```

You can see an example [here](/samples/Sample4).

### Defining list of secrets in advance (no list secrets permission required)

Security best practices sometimes prevent the listing of secrets in a production environment.
As a result, it is possible to define a list of secrets in lieu of a secret filter. When using this this approach,
the library will only retrieve the secrets whose `ARN` or name are present in the given `AcceptedSecretArns` list.  

```csharp
var acceptedARNs = new[]
{
    "MySecretFullARN-abcxyz",
    "MySecretPartialARN",
    "MySecretUniqueName",
};

builder.AddSecretsManager(configurator: options =>
{
    options.AcceptedSecretArns = acceptedARNs;
});
```

### Altering how the values are added to the Configuration

Sometimes we are not in control of the full system. Maybe we are forced to use secrets defined by someone else that uses a different convention.

In this case, you can provide a function that gets invoked every time a value is discovered. This function allows you to customize which key should be used.

As an example, here we are converting all incoming keys to upper case

```csharp
builder.AddSecretsManager(configurator: options =>
{
    options.KeyGenerator = (entry, key) => key.ToUpper();
});
```

You can see an example [here](/samples/Sample5).

### Customizing the AmazonSecretsManagerConfig, for example to use localstack

There are some situations where you might want to customize how the AmazonSecretsManagerConfig is built, for example when you want to use
[localstack](https://github.com/localstack/localstack) during local development. In those cases, you should customize the ServiceUrl.

```csharp

builder.AddSecretsManager(configurator: options =>
{
    options.ConfigureSecretsManagerConfig = c => {
        c.ServiceUrl = "http://localhost:4584" // The url that's used by localstack
    };
});

```

## Versioning

This library follows [Semantic Versioning 2.0.0](http://semver.org/spec/v2.0.0.html) for the public releases (published to the [nuget.org](https://www.nuget.org/)).

## How to build

This project uses [Cake](https://cakebuild.net/) as a build engine.

If you would like to build this project locally, just execute the `build.cake` script.

You can do it by using the .NET tool created by CAKE authors and use it to execute the build script.

```powershell
dotnet tool install -g Cake.Tool
dotnet cake
```
