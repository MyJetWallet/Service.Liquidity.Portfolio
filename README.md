![.NET Core](https://github.com/MyJetWallet/Service.Template/workflows/.NET%20Core/badge.svg)

# HOW TO USE TEMPLATE

### Register 
`
dotnet new --install ${path}
`

where ${path} is the **full** path to the clonned directory (where the folder .template.config placed) **without trailing slash** (e.g. `D:\Root\Service.Template\template`)

### Create

`
dotnet new myjetservice -n {ServiceName} -o {ServiceDirectory}
`

e.g.

`
dotnet new myjetservice -n SomeService -o SomeServiceFoulder
`
# Features added to the template

### Load appsettings.josn from external server
You can collect settings for all services in a cluster in one place. It is can simplify to DevOps management of configuration.
Service after start can load appsettings.josn from the external server by HTTP GET request.
To activate this feature you should set in Environment variable 'RemoteSettingsUrl' URL address for getting JSON content.

### Use Serilog for structed logs
Use Serilog to improve log system for write structed logs. 
Log by default writed to console.
You can activate the feature to write logs to external log storage - [Seq](https://datalust.co/seq). Seq it is a very light tool, you can run in your local machine or into your cluster. And collect logs from all services in this single store to analyze.
To activate this feature you should set configuration variable 'SeqUrl' URL address for Seq.

#### Use SEQ locally
To collect and read logs locally you can run Seq locally by docker:
`
docker run -it -p 5341:5341 -p 81:80 -e ACCEPT_EULA=Y datalust/seq
`
User interface you can see at http://localhost:81


### Use GRPC and Rest
Template service by default ready to work with GRPC services and REst services. application host two ports:
* 8080 - protocol HTTP 1.1 to work with standard Rest api with aspnet and WebAPI.
* 80 - protocol HTTP 2.0 to wotk with GRPC services.

By default application do not support SSL and HTTPS. Client for GRPS should use http ptotocol with out encription.

### Environment variables used
|Variable|Description|Required|Default value|
|-------|-------|-------|-------|
|SETTINGS_URL|Url to settings yaml. If utl not specified then settings will be readed from file `.myjetwallet`|no|null|
|ConsoleOutputLogLevel|Print log to console from this level (Debug,Information,Warning,Error,Verbose)|no|null|
|HOSTNAME|Will be used as the `host-name` key in the logs. If not specified, the OS user name will be used instaed|no|`null`|


