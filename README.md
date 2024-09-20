#  Biflow

### Contents

1. [Introduction](#1-introduction)
    1. [Rationale](#11-rationale)
    2. [Technical requirements](#12-technical-requirements)
    3. [Authentication providers](#13-authentication-providers)
    4. [Architecture options](#14-architecture-options)
2. [Terminology and features](#2-terminology-and-features)
3. [Documentation](#3-documentation)
    1. [Source code solution and projects](#31-source-code-solution-and-projects)
    2. [Execution statuses](#32-execution-statuses)
        1. [Job execution statuses](#321-possible-job-execution-statuses)
        2. [Step execution statuses](#322-possible-step-execution-statuses)
        3. [Step execution lifecycle](#323-step-execution-lifecycle)
    3. [User roles](#33-user-roles)
    4. [Encryption](#34-encryption)
4. [Installation](#4-installation)
    1. [On-premise](#41-on-premise)
    2. [Azure (monolithic)](#42-azure-monolithic)
    3. [Azure (modular)](#43-azure-modular)
    4. [First use and configuration](#44-first-use-and-configuration)
5. [Operation and administrative tasks](#5-operation-and-administrative-tasks)
    1. [Executions](#51-executions)
    2. [Services](#52-services)


# 1. Introduction

Biflow is a powerful platform for easy business intelligence (BI) and data platform workflow orchestration built on top of the .NET stack. It includes an easy-to-use web user interface and integrates with several data related technologies such as
- Microsoft SQL Server (including SSIS and SSAS)
- Microsoft Azure
  - Azure SQL
  - Azure Data Factory
  - Azure Synapse
  - Azure Functions
  - Azure Analysis Services
- Microsoft Power BI
- Snowflake
- Qlik Cloud

The focus of Biflow is ease-of-use. When set up, it should be fairly easy even for relatively non-technical people to start authoring, scheduling, executing and monitoring workflows via the web user interface. Biflow enables intelligent management of even large and complex workflows.

- *__Author, schedule, execute and monitor workflows__* (jobs) comprising of multiple steps
    - Supports complex schedules using Cron expressions
    - Easy manual execution of either entire or partial workflows
- Create jobs and *__manage the order in which steps are run__* either by
    1. Simply grouping steps into consecutive execution phases
    2. Defining detailed dependencies (loose or strict) between steps in each job
- *__Parameterize__* workflows and steps
    - Define static or dynamic parameters that are passed to steps
    - Share commong parameters and values across multiple steps
    - Evaluate or assign parameter values during execution
- *__Visualize__* workflows
    - Understand your workflow's dependencies more easily by analyzing dependency graphs
    - Find bottlenecks and problematic steps in your executions by visualizing durations, statuses etc.
- Manage *__subscriptions__* to receive alerts when something goes wrong
    - Or temporarily bypass subscriptions when doing development and testing of workflows
- Easily *__manage simple Master Data__* through the web user interface
    - Configure SQL database tables available for editing in the UI with minimal effort
- Grant users different levels of access and permissions
- Authorize users to manage specific workflows or data tables

Currently supported step types:
- Sql
    - Run arbitrary SQL commands (e.g. stored procedures) on SQL Server, Azure SQL and Snowflake.
    - Return scalar values and assign them to workflow parameters/variables
- Package
    - Execute SQL Server Integration Services (SSIS) packages deployed to the SSIS catalogue
- Tabular
    - Process tabular models deployed to SQL Server (SSAS) or Azure Analysis Services
- AgentJob
    - Execute SQL Server Agent jobs
- Pipeline
    - Execute pipelines created in Azure Data Factory and Synapse Analytics workspaces
- Function
    - Invoke Azure Function App functions
    - Wait for durable functions to complete before step is considered completed
- Exe
    - Run locally stored executables (e.g. Python or PowerShell scripts)
- Dataset
    - Refresh datasets/semantic models published to Power BI Service workspaces
- Qlik
    - Reload apps in Qlik Cloud
- Mail
    - Send emails as part of your workflows
- Job
    - Start another Biflow job as part of your workflow

## 1.1. Rationale

Why should I use Biflow? Can't I already orchestrate my data platform using one of the following tools?
- SQL Server Agent
- SSIS
- Azure Data Factory
- Airflow
- etc.

Yes, you can, and we'll get to that shortly. First though, it should be clarified that Biflow *is not an ETL tool*. It focuses on data orchestration, part of which is orchestrating ETL processes. When it comes to implementing the ETL processes themselves, using tools such as SSIS, ADF, Azure Functions and others is obviously the way to go. But tying all these different technologies together and bridging the gaps to create a single orchestration job, that's where Biflow comes in.

Let's look at some common and simple orchestration methods implemented using the previosuly listed tools.

#### SQL Server Agent

In on-premise SQL Server data platforms, SQL Server Agent is often used to at least schedule and sometimes even to orchestrate ETL processes. You can easily run SSIS packages as well as stored procedures and the scheduling capabilities are relatively powerful. It is also extremely reliable. Where SQL Server Agent falls (massively) short is the orchestration part, which is understadable as it was never meant to be one.

All steps in a job are executed sequentially and defining dependencies is almost nonexistent. Your options are to go to the next step, go back to a previous step or exit the job. SQL Server Agent was primarily meant to target administrative tasks (backups, index rebuilds etc.), where orchestration of a large number of steps was rarely the issue.

#### SSIS & ADF

The orchestration capabilities in SSIS and ADF are very similar. With SSIS, you often use the scheduling capabilities of SQL Server Agent and the triggers in ADF are also quite powerful. The way in which you can define dependencies between tasks in SSIS and activities in ADF is also similar and has largely inspired and affected how it works in Biflow too.

The downside in both tools is the fact that dependency management between tasks *is not metadata based* but instead you define dependencies between tasks graphically. This works very well and is highly intuitive with simple jobs with a couple dozen tasks at most. However, when you need to manage dependencies across tens of tasks or even a hundred tasks, these tools are no longer optimal. In fact, in ADF, the maximum number of activities in a pipeline is currently 40. This significantly limits the dependency management between individual tasks when you need to split them in separate pipelines.

#### Airflow

Orchestration is one of the main purposes of Airflow. Shortcomings with the previous tools can be overcome by using Airflow, since you can integrate various technologies and build complex orchestration workflows or DAGs as they are called in Airflow. (Actually, jobs running in dependency mode in Biflow are DAGs too.) You define DAGs by writing Python, making the leveraging of metadata possible for managing dependencies between a large number of tasks. Extensibility is a major advantage of Airflow.

But this flexibility with Airflow comes at a cost. Give a business-oriented user access to Airflow and ask them to author a new DAG to orchestrate some ADF pipelines and reports that they might be familiar with. Writing Python to define DAGs and working with Airflow in general requires technical and technological know-how, making it very hard for business users to do anything other than launch predefined DAGs and monitor their execution.

#### Conclusion

Using metadata to define and manage dependencies between tasks makes it possible to author large and complex jobs so that all dependencies can be listed. There is no hard limit to the number of steps or dependencies you can have in a single job in Biflow. Having over a hundred steps in a single job is still very much manageable. This means that the execution of jobs can be optimized to a very high degree and steps towards the end of the job do not lose sight of what may have occurred in the earliest steps of the job. All steps can be executed immediately when they can or skipped if the dependency requirements are not met.

Including an intuitive browser based graphical user interface makes it possible for even non-technical users to author orchestration jobs in Biflow. And even though dependencies are metadata based, they can be visualized to easily see and understand the dependencies of complex jobs. It is also easy to only include selected steps in a manual on-demand execution of jobs. Something that is significantly more involved in SSIS and ADF.

The creation and development of Biflow has largely been inspired by my own experiences working with data platforms full-time since 2016. The methods and features in Biflow are informed by the real-life frustrations I've faced using some of the tools mentioned here. Biflow supports orchestrating data platforms in a way that I see being useful, smart and optimal. That also means tight integration with the ETL and data platform technologies I use most often (see supported step types).

## 1.2. Technical requirements

Some requirements apply depending on whether Biflow is configured to run either on-premise or in Azure but some requirements are common.

### Common
- SQL Server or Azure SQL Database to host the application database
    - SQL Server 2012 or newer
        - Edition: Express or above
    - Azure SQL Database or Managed Instance
        - An S1 (or maybe even S0) tier Azure SQL Database is already sufficient for small scale production use.
- Email notifications
    - An email account is needed to send email notifications from the application

### On-premise
- Windows Server
    - ASP.NET 8 Hosting Bundle installed
- Windows account
    - Biflow can operate using either Windows Authentication or SQL Server Authentication
    - SSIS compatibility can be achieved using either method, but Windows Authentication is simpler and recommended.
    - AD account vs. local Windows account
        - If the SQL Server instance is located on a separate server from the application server, then an Active Directory (AD) account is recommended.
        - If the SQL Server instance is located on the same server, then a local Windows account can be used.
    - The Windows account used to run Biflow should have Logon as Service rights on the application server.

### Azure
- Azure App Service (Linux)
    - Minimum B2 or B3 level is recommended

## 1.3. Authentication providers

Four methods of authentication are supported:
- Built-in
  - User management and authentication is done using a built in identity provider
  - MFA is not supported
  - Supports remote access
- Windows
  - Authentication is done at the OS level using Windows accounts
  - No login page is presented to the user to access the UI
  - Does not support remote access
- LDAP
  - An LDAP server (e.g. Active Directory) can be used as the identity provider to authenticate users
  - Supports remote access
- Entra ID (formerly Azure Active Directory)
  - Users are authenticated using their Microsoft organizational accounts
  - Requires an app registration to be created in the host tenant's Entra ID
  - Requires internet access
  - Supports remote access

**Note:** In all cases the list of authorized users and their access role is managed in the application.

## 1.4. Architecture options

There are three recommended ways to configure Biflow from an architecture point of view: on-premise, Azure (monolithic) and Azure (modular). More details about the different architecture options and related setup are given also in the installation section.

### On-premise

The on-premise option takes advantage of OS level features such as Windows Services to host certain components of the app (the scheduler/executor services). Internet Information Services (IIS) can also be used to host e.g. the user interface app. This makes it easier to configure SSL certificates for accessing the user interface via HTTPS.

### Azure (monolithic)

The Azure (monolithic) architecture has all the necessary components and services hosted inside one monolithic application. The application is running in an Azure App Service (Linux) as a Web App. This allows for efficient cost minimization through the use of lower tier App Service Plans (B1-B3).

**Note:** The single-app monolithic architecture is recommended only for development and testing purposes.

### Azure (modular)

The Azure (modular) approach closely resembles the on-premise architecture. From the two Azure architectures, this offers significantly more control over updates to different components of the application. All services deployed to Azure can still share the same Linux App Service for cost optimization. Note, that a lightweight Linux virtual machine might also be required for deployment and configuration tasks depending on your Azure networking setup.

# 2. Terminology and features

To better understand the documentation and some of the main features of Biflow, here is some important terminology.

**Jobs** are workflows that define the **steps** that need to be taken to update some data. Jobs can contain any number of steps: ranging from a handful of steps to update a simple report to even hundreds of steps to fully load an entire Enterprise Data Warehouse.

An **execution** is a copy (snapshot in time) of a job created when the job needs to be executed. When the execution is created, it becomes independent from the job it was based on – changes to a job do not affect executions that have already been created.

The order in which steps are executed is determined by two factors: the job's **execution mode** and the **dependencies** between steps. There are three different execution modes to choose from: execution phase mode (1), dependency mode (2) and hybrid mode (3).

#### Execution phase mode

Steps are executed in order based on their execution phase. Steps in the same execution phase can be started at the same time. Execution phases are started sequentially from the lowest value up. Step dependencies have no effect when using execution phase mode.

#### Dependency mode

Jobs using dependency mode are essentially Directed Acyclic Graphs (DAGs). Steps are executed in order based on their dependencies. Steps that have no dependencies are started first and steps that have no dependencies between them can be executed in parallel at the same time. Steps that have dependencies are executed when preceding steps have been completed and the dependency type criteria is met. The execution phase attribute of steps is used to denote the execution priority of otherwise equal steps (lower value = higher priority).

#### Hybrid mode

Steps are executed in order based on their execution phase (same as execution phase mode). Additionally, step dependencies are also checked after execution phase conditions are met (dependency mode).

### Features

Steps can have any number of dependencies, even to steps in other jobs. There are three types of dependencies.
- On completed (loose dependency) – the dependency must complete/finish
- On succeeded (strict dependency) – the dependency must complete *successfully*
- On failed – the depency must *fail*

If the dependency conditions of a step are not met, the step will be skipped. The execution of a step can also be skipped if its **execution condition** evaluates to false. These are optional boolean expressions written in C# that are evaluated during execution to run additional dynamic checks (based on date and time, for example).

Jobs and steps can have any number of **parameters** (variables) that can be used to share values between steps during execution. Job parameters can be defined dynamically with C# expressions or they can be set by SQL steps during execution by fetching values from a database. This allows the decoupling of individual ETL components. Instead, it is the orchestration framework that injects the needed parameter values. This pattern is also called Inversion of Control.

Steps can be categorized by using **tags** – simple labels that can be effective when filtering steps based on a data source, business function etc.

Jobs can have any number of **schedules**, which are triggers that invoke an execution of the job on a given time. Schedules use Cron expressions to define when a schedule should fire. A single Cron expression can be used to create a complex schedule, such as "every 2 hours between 10 am and 5 pm on days 1 to 7 of every month". Schedules can also define **tag filters** to limit which steps are to be included in that specific schedule's execution. This way we can avoid creating copies of jobs just to have a subset of its steps on a different schedule.

Users can **subscribe** to email notifications of executions they are interested in. Subscriptions can be made on a job, step or tag or any combination of the three.

Advanced features include defining the **sources** and **targets** for steps. These are **data objects** that the step either consumes or produces. They help to create a data lineage and are also useful when defining the dependencies of a step, since dependencies can be automatically inferred based on sources and targets. (A step producing a data object can be considered a dependency for a step consuming the same object.) Wide use of data objects can help manage the dependencies optimally even in a very large job.

Outside of data orchestration, Biflow also supports **data tables**. These are simple control tables (or Master Data tables) that can be maintained via the web user interface by business users. Data tables point and write to SQL tables, allowing the use of primary keys, foreign keys, data types and various other constraints to ensure data quality directly on data input. Common use cases include maintaining some simple report specific data that would otherwise be read in from a flat file or Excel file. This also allows business users to maintain these Master Data in the same platform where they execute their data jobs. **Lookups** can be used to create foreign key references between data tables, making it possible for users to create and manage even hierarchical data.

# 3. Documentation

## 3.1. Source code solution and projects

|Project|Description|
|-|-|
|Core|Core library project containining common models, entities, interfaces and extensions.|
|DataAccess|Data access layer project responsible for ORM (Object-relational mapping). Contains the model configuration for the Entity Framework Core database context and some data management related services.|
|Executor.Core|Core library for the executor and orchestration services|
|Executor.WebApp|Web App front-end project for the executor and orchestration application|
|Scheduler.Core|Core library for the scheduler services|
|Scheduler.WebApp|Web App front-end project for the scheduler service|
|Ui.Icons|Library for common icon related models|
|Ui.SourceGeneration|Source generator for the component library project|
|Ui.TableEditor|Library for models and extensions related to data table interaction|
|Ui.SqlMetadataExtensions|Library for models and extensions related to retrieving metadata from MS SQL and Snowflake databases|
|Ui.Core|Common UI services that can be shared between different UI versions/editions|
|Ui.Components|Razor component library for the UI project. Project also contains generated icon classes generated by the separate source generation project.|
|Ui|Blazor Server UI application. Can be configured to host the executor and scheduler services internally.|

## 3.2. Execution statuses

### 3.2.1 Possible job execution statuses

|Status|Description|
|-|-|
|NotStarted |The executions has not yet been started by the executor|
|Running    |One or more steps included in the execution have not yet been completed.|
|Succeeded  |All steps included in the execution succeeded.|
|Failed     |One or more steps failed for the execution.|
|Warning    |There were duplicate steps in the execution or some steps were retried.|
|Stopped    |The execution was manually stopped.|
|Suspended  |There were unstarted steps remaining in the execution when the execution was finished.|

### 3.2.2 Possible step execution statuses

|Status|Description|
|-|-|
|NotStarted |This is the initial status of all steps after the execution has been created. The step has not been started but is awaiting evaluation or execution. All dependencies may not have yet been completed. Job executions that have been termiated unexpectedly may have steps left with this status.|
|DependenciesFailed|One or more of the step's strict dependencies failed and the step's execution was skipped.|
|Queued|The step's dependencies succeeded and the step is waiting for a parallel execution slot to open up.|
|Skipped|The step's execution condition was not met and the step was skipped.|
|Duplicate|The step was skipped with a status of 'Duplicate'. This happens if the same step is running under a different execution at the same time and the step's duplicate behaviour is defined as 'Fail'.|
|Running|The step is currently executing|
|Succeeded|The step was completed successfully|
|Warning|The step succeeded with warnings|
|Failed|The step encountered an exception and failed or the step reached its timeout limit|
|Retry|The step failed but has retry attempts left|
|AwaitingRetry|The step is currently waiting for the specified retry interval before it is executed again|
|Stopped|A user has manually stopped the execution of the entire job or of this specific step.|

### 3.2.3 Step execution lifecycle

The flowchart below describes the lifecycle and states of a step execution in dependency mode. During the `NotStarted`, `Queued`, `Running` and `AwaitingRetry` states it is possible for a user to cancel/stop the execution of a step. If a stop request is received, the step execution is canceled and the final step execution status will be `Stopped`. Remaining retries will not be attempted after the execution has been stopped. Note however, that if the step is stopped during the `NotStarted` state, the step is stopped and its status updated only after it reaches the `Queued` state.

![The lifecycle and states of a step execution](/Images/StepExecutionLifecycle.png)*The lifecycle and states of a step execution in dependency mode*

## 3.3. User roles

### Primary roles

Users can have one and only one primary role assigned to them. Adding users without a primary role is not supported.

#### Viewer

Viewer users can
- view
    - job details
    - step details
    - schedules
    - executions
- subscribe to jobs and steps for execution notifications
#### Operator
In addition to the same rights as the viewer role, operators can
- manage (create, edit & delete) schedules
- execute jobs manually on demand
- edit job parameters

#### Editor
In addition to the same rights as the operator role, editors can
- manage (create, edit & delete) all jobs and steps
- view stack traces of failed and cancelled steps (when available)

#### Admin
In addition to the same rights as the editor role, admins can
- manage users and global settings
- manage other users' subscriptions

### Secondary roles

Secondary roles can be assigned to non-admin users to extend their user rights.

#### SettingsEditor
- Allows users to manage endpoint settings
    - SQL, Snowflake and tabular connections
    - Data Factory instances
    - Function Apps
    - Qlik Cloud endpoints
    - Storage account endpoints

#### DataTableMaintainer
- Allows users to maintain and edit all data tables

## 3.4. Encryption

Data saved and processed by Biflow is not encrypted by default on the database level. If you want to implement database level encryption of sensitive data, this can be achieved using the Always Encrypted feature of SQL Server and Azure SQL Database.

More information about Always Encrypted can be found in <a href="https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-ver15">Microsoft’s documentation.</a>

If you want to implement Always Encrypted, these columns are good candidates for encryption:
- [app].[AccessToken].[Token]
- [app].[ApiKey].[Value]
- [app].[AppRegistration].[ClientSecret]
- [app].[Connection].[ConnectionString]
- [app].[Credential].[Password]
- [app].[FunctionApp].[FunctionAppKey]
- [app].[Step].[FunctionKey]
- [app].[QlikCloudClient].[ApiToken]
- [app].[BlobStorageClient].[ConnectionString]
- [app].[BlobStorageClient].[StorageAccountUrl]

If Always Encrypted is utilized, this should be reflected in the connection strings set in the application settings (AppDbContext). Always Encrypted is enabled with the following connection string property: `Column Encryption Setting=enabled`

# 4. Installation

There are three different installation alternatives: on-premise, Azure (monolithic) and Azure (modular).

## 4.1. On-premise

### System database

1. Create a Windows/AD account to run the Biflow applications. See the requirements section for more details on whether an AD account is needed or useful or if a local Windows account is sufficient.
2. Create a new empty database or choose an existing database for Biflow installation. It is recommended that the database use the instance level collation.
    - Execute the installation script (BiflowDatabaseScript.sql) in the database of your choosing.
    - The script will generate the necessary objects (schema, tables and stored procedures).
3. Create a login that is used to connect to the installation database using **either** of these methods.
    - Windows Authentication - Add the account created in step 1 to the `db_owner` role in the installation database
    - SQL Server Authentication
        - Create a new SQL Server login or select an existing one to use to connect to the installation database
        - Add the login to the `db_owner` role in the installation database

### ASP.NET 8 Hosting Bundle

1. On machines where any of the application components (UI, executor, scheduler) are installed, also install the ASP.NET 8 Hosting Bundle. Follow the instructions on the <a href="https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.0-windows-hosting-bundle-installer">.NET download page</a>.
    - NOTE! If the Hosting Bundle is installed before IIS, the bundle installation must be repaired. Run the Hosting Bundle installer again after installing IIS.

### Service account

- It is recommended to use either a local or an AD account as the service account for all Biflow services. Make sure the service account has full control rights to the installation folders. Otherwise you might experience unexpected behaviour, such as log files not being generated.

### Executor web application

1. Extract the BiflowExecutor.zip file to the C: root on the application server (C:\Biflow\BiflowExecutor).
2. Update the appsettings.json file with the correct settings for your environment.

|Setting|Description|
|-|-|
|ConnectionStrings:AppDbContext|Connection string used to connect to the Biflow system database based on steps taken in the database section of this guide. **Note:** the connection string must have `MultipleActiveResultSets=true` enabled.|
|Authentication:ApiKey|Any arbitrary string value used to authenticate incoming requests to the executor app API. You can use various API key generators online to create a secure value. The UI and scheduler app have corresponding app settings, the values of which need to exactly match this setting.|
|EmailSettings|Settings used to send email notifications.|
|PollingIntervalMs|Time interval in milliseconds between status polling operations (applies to some step types). Default value is `5000`.|
|Serilog:WriteTo:Args:Path|Path where application will write is log files. Default value is `C:\\Biflow\\BiflowExecutor\\log\\executor.log`.|
|Kestrel:Endpoints:Http:Url|The http url and port which the executor API should listen to, for example `http://localhost:4321`. If there are multiple installations/environments of the executor service on the same server, the executor applications should listen to different ports.|

3. Open the Windows command terminal in **administrator mode**.
    - Run the following command: `sc.exe create BiflowExecutor binpath= C:\Biflow\BiflowExecutor\BiflowExecutor.exe start= auto displayname= "Biflow Executor"`
4. Open Windows Services, navigate to the service "Biflow Executor", right click and select Properties.
    - Add the login information for the service account used to run the service and scheduled executions. If Windows Authentication is used to connect to the database, then this account’s credentials are used to connect.
    - Start the service.
5. Test the executor application by sending a GET request to the executor API to test the system database connection. This can be done with PowerShell using the following command. Replace the URL with the one used when configuring the app.
    - `Invoke-WebRequest http://localhost:4321/connection/test -Headers @{ 'x-api-key' = '<api_key_value>' }`

### Scheduler service

1. Extract the BiflowScheduler.zip file to the C: root on the application server (C:\Biflow\BiflowScheduler).
2. Update the appsettings.json file with the correct settings

|Setting|Description|
|-|-|
|ConnectionStrings:AppDbContext|Connection string used to connect to the Biflow database based on steps taken in the database section of this guide. **Note:** The connection string must have `MultipleActiveResultSets=true` enabled.|
|Authentication:ApiKey|Any arbitrary string value used to authenticate incoming requests to the scheduler app API. You can use various API key generators online to create a secure value or choose the same value as with the executor app's API. The UI has a corresponding app setting, the value of which needs to exactly match this setting.|
|Executor:Type|`[ WebApp \| SelfHosted ]`<br/>Whether the executor service is installed as a web app or is running self-hosted inside the scheduler application. **Note:** The SelfHosted executor should only be used for development and testing.|
|Executor:WebApp:Url|Url to the executor web app|
|Executor:WebApp:ApiKey|The executor app's API key used to authenticate requests sent to the executor API.|
|Authorization:Windows:AllowedUsers|Array of Windows users who are authorized to issue requests to the scheduler API, e.g. `[ "DOMAIN\\BiflowService", "DOMAIN\\AdminUser" ]`. If no authorization is required, remove the `Authorization` section. Only applies to on-premise Windows environments.|
|Kestrel:Endpoints:Http:Url|The http url and port which the scheduler API should listen to, for example `http://localhost:5432`. If there are multiple installations/environments of the scheduler service on the same server, the scheduler applications should listen to different ports.|
|Serilog:WriteTo:Args:path|Path where the application will write its log files. Default value is `C:\\Biflow\\BiflowScheduler\\log\\scheduler.log`|

3. Open the Windows command terminal in **administrator mode**.
    - Run the following command: `sc.exe create BiflowScheduler binpath= C:\Biflow\BiflowScheduler\BiflowScheduler.exe start= delayed-auto displayname= "Biflow Scheduler"`
4. Open Windows Services, navigate to the service "Biflow Scheduler", right click and select Properties.
    - Add the login information for the service account used to run the service and scheduled executions. If Windows Authentication is used to connect to the database, then this account’s credentials are used to connect.
    - Start the service.
5. Navigate to `C:\Biflow\BiflowScheduler\log` and open the log text file. There should be no error reports if the scheduler was able to connect to the database and load schedules into the service.
6. Test the scheduler API by sending a GET request. This can be done with PowerShell using the following command. Replace the URL with the one used when configuring the app.
    - `Invoke-WebRequest http://localhost:5432/status -Headers @{ 'x-api-key' = '<api_key_value>' }`

### User interface web application

1. If Entra ID is used for authentication, an app registration needs to be created in the hosting tenant's Entra ID.
    - Navigate to the target tenant’s Azure portal in portal.azure.com.
    - Go to Entra ID => App registration => New registration.
    - Register the app and save the client id and client secret someplace. You will not be able to access the client secret after it has been created.
    - Add a redirect URL for the app registration under Manage => Authentication => Add a platform
        - Select **Web**
        - **Redirect URI**: This should be the URL where the UI application can be reached appended with `/signin-oidc`. For example, if the application can be reached at `https://contoso.azurewebsites.net` then the redirect URI should be `https://contoso.azurewebsites.net/signin-oidc`
        - **Front-channel logout URL**: Similarly, the logout URL is the UI app's URL appended with `/signout-oidc`. With the base URL of the example above, the logout URL would be `https://contoso.azurewebsites.net/signout-oidc`
        - **Implicit grant and hybrid flows**: Select **ID tokens**
        - **Supported account types**: In most cases, the correct option will be *Accounts in this organizational directory only*. However, if you need to be able to authorize users outside your organization to access the UI, select *Accounts in any organizational directory*.
2. Extract the BiflowUi.zip file to the C: root on the application server (C:\Biflow\BiflowUi).
    - The account created in step 1 should have read/write access to this folder.
3. In the folder where you extracted the installation zip file, locate the file appsettings.production.json. Update the file with the correct settings.

|Setting|Description|
|-|-|
|EnvironmentName|Name of the installation environment to be shown in the UI (e.g. Production, Test, Dev etc.)|
|ConnectionStrings:AppDbContext|Connection string used to connect to the Biflow database based on steps taken in the database section of this guide. **Note:** The connection string must have `MultipleActiveResultSets=true` enabled.|
|Authentication|`[ BuiltIn \| Windows \| AzureAd \| Ldap ]`|
||`BuiltIn`: Users accounts and passwords are managed in Biflow. Users are application specific.
||`Windows`: Authentication is done using Active Directory. User roles and access are defined in the Biflow users management. The user does not need to log in but instead their workstation Windows account is used for authentication.|
||`AzureAd`: Authentication is done using Entra ID. User roles and access are defined in the Biflow users management.|
||`Ldap`: LDAP connection is used to authenticate users. This also supports Active Directory. User roles and access are defined in the Biflow users management. User matching is done using the LDAP `userPrincipalName` attribute.|
|AdminUser|When this section is defined, the UI application will ensure at startup that an admin user with the credentials from this configuration section exists in the database. This section can be used to create the first admin user to be able to log in via the UI.|
|AdminUser:Username|Username for the admin user|
|AdminUser:Password|Password for the admin user. Only used when `Authentication` is set to `BuiltIn`.|
|AzureAd|This section needs to be defined only if `Authentication` is set to `AzureAd`|
|AzureAd:Instance|`https://login.microsoftonline.com/`|
|AzureAd:Domain|Your organization domain, e.g. `contoso.com`|
|AzureAd:TenantId|If the app registration supports *Accounts in this organizational directory only*, set the value to the directory (tenant) ID (a GUID) of your organization. If the registration supports *Accounts in any organizational directory*, set the value to `organizations`. If the registration supports *All Microsoft account users*, set the value to `common`.|
|AzureAd:ClientId|The application (client) ID of the application that you registered in the Azure portal.|
|AzureAd:ClientSecret|The client secret for the app registration.|
|AzureAd:CallbackPath|`/signin-oidc`|
|Ldap|This section needs to be defined only if `Authentication` is set to `Ldap`|
|Ldap:Server|The LDAP server to connect to for authentication|
|Ldap:Port|The port to use for the LDAP server connection |
|Ldap:UseSsl|Boolean value: `true` to use SSL for the connection, `false` if not|
|Ldap:UserStoreDistinguishedName|The DN (distinguished name) for the LDAP container which to query for users|
|Executor:Type|`[ WebApp \| SelfHosted ]`|
||Whether the executor service is installed as a web app or is running self-hosted inside the UI application. **Note:** The SelfHosted executor should only be used for development and testing.|
|Executor:WebApp:Url|Needed only when `Executor:Type` is set to `WebApp`. Url to the executor web app API|
|Executor:WebApp:ApiKey|Needed only when `Executor:Type` is set to `WebApp`. API key used to authenticate requests sent to the executor app's API.|
|Executor:SelfHosted|This section needs to be defined only if `Executor:Type` is set to `SelfHosted`. Refer to the executor web application's settings section to set the values in this section.|
|Scheduler:Type|`[ WebApp \| SelfHosted ]`|
||Whether the scheduler service is installed as a web app or is running self-hosted inside the UI application. If `Executor:Type` is set to `SelfHosted` then this settings must also be set to `SelfHosted`.  **Note:** The SelfHosted scheduler should only be used for development and testing.|
|Scheduler:WebApp:Url|Needed only when `Scheduler:Type` is set to `WebApp`. Url to the scheduler service web app API|
|Scheduler:WebApp:ApiKey|Needed only when `Scheduler:Type` is set to `WebApp`. API key used to authenticate requests sent to the scheduler app's API.|
|Kestrel:Endpoints:Https:Url|The https url and port which the UI application should listen to, for example https://localhost. If there are multiple installations on the same server, the UI applications should listen to different ports. Applies only to on-premise installations.|
|Serilog:WriteTo:Args:path|Folder path where the application will write its log files. Applies only to on-premise installations. Default value is `C:\\Biflow\\BiflowUi\\log\\ui.log`|

4. Open the Windows command terminal in **administrator mode**.
    - Run the following command: `sc.exe create BiflowUi binpath= C:\Biflow\BiflowUi\BiflowUi.exe start= delayed-auto displayname= "Biflow User Interface"`
5. Open Windows Services, navigate to the service "Biflow Scheduler", right click and select Properties.
    - Add the login information for the service account used to run the service and scheduled executions. If Windows Authentication is used to connect to the database, then this account’s credentials are used to connect.
    - Start the service.
    - Navigate to `C:\Biflow\BiflowScheduler\log` and open the log text file. There should be no error reports if the scheduler was able to connect to the database and load schedules into the service.
6. Hosting the UI application as a Windows Service uses the Kestrel web server built into the ASP.NET Core runtime. To configure the HTTP and HTTPS endpoints for the UI, refer to these guides:
    - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#configure-endpoints-in-appsettingsjson
    - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#configure-https-in-appsettingsjson
7. Alternatively you can host the UI application using IIS (Internet Information Services).

## 4.2. Azure (monolithic)

- Create a new App Service Plan (Linux)
    - Recommended pricing tier B2 or B3
- Create a new Web App and set the following settings
    - Publish: Code
    - Runtime stack: .NET 8 (LTS)
    - Operating System: Linux
    - Linux Plan: Previously created App Service Plan
- When the Web App has been created, go to the resource and its Configuration settings.
    - General Settings => Websocket => On
    - General Settings => Always on => On
- Set the application settings in Configuration => Application settings.
    - __Note that Linux Web Apps do not recognize colon as a configuration section separator.__ Instead double underscores are used.
    - Application settings
        - Authentication = See the on-premise section for configuring authentication
        - EnvironmentName = NameOfYourEnvironment
        - Executor__Type = __SelfHosted__
        - Scheduler__Type = __SelfHosted__
        - Executor__SelfHosted__PollingIntervalMs = 5000
        - ConnectionStrings__AppDbContext = Connection string to the Biflow system database
        - WEBSITE_TIME_ZONE = Time zone for the application (defaults to UTC), e.g. `Europe/Helsinki`
            - On Linux, use the TZ identifier from the <a href="https://en.wikipedia.org/wiki/List_of_tz_database_time_zones">tz database</a>.
- Deploy the UI application code (`Biflow.Ui`) as a zip file to the target Web App. Before deploying remove all other configuration sections from the appsettings.json file except the `Logging` section. This way there are no unwanted settings that are applied via the appsettings file.
- Using System Assigned Managed Identities for authentication to the system database is recommended to avoid having to save sensitive information inside connection strings.
- Recommended: Apply desired access restrictions to the Web App to allow inbound traffic only from trusted IP addresses or networks.

## 4.3. Azure (modular)

- Create the Azure App Service and UI Web App following the same steps as in the monolithic approach until the configuration stage.
- Also create two additional Web Apps in the same App Service, one for the scheduler service and the other for the executor service.
- Make sure websockets are enabled for the UI application and that "Always on" is enabled for the scheduler and executor applications.
- Create a virtual network resource.
- Create a lightweight Linux virtual machine resource (B1s is sufficient).
    - This VM is used to configure and deploy the application files to the Web App resources.
    - Attach the virtual machine to the default subnet of the virtual network created in the previous step.
    - Allow SSH traffic from your desired IP addresses or networks to the virtual machine.
- Apply access restrictions to the UI app to allow inbound traffic only from trusted IP addresses or networks (e.g. company VPN or local network).
- Create a new subnet in the virtual network (e.g. biflow-subnet).
    - If the default subnet has IPV4 range of 10.0.0.0/24, the new subnet may have a range of 10.0.1.0/24.
    - Delegated to: Microsoft.Web/serverFarms
    - Configure the UI, executor and scheduler applications' outbound traffic to route through the previously created virtual network subnet (biflow-subnet). This network can then be used to allow traffic to your Azure SQL Database hosting the application database.
- Configure private endpoints for the inbound traffic of the scheduler and executor applications.
    - Create the private endpoints in the default subnet of the virtual network.
    - Disable public network access.
- Add service endpoints for the following services to the virtual network (subnet biflow-subnet):
    - Microsoft.AzureActiveDirectory
    - Microsoft.Sql
    - Microsoft.Web

These steps isolate the executor and scheduler application endpoints from the internet and only exposes them to the UI application. Traffic from the UI and scheduler applications is routed through the virtual network and private endpoint to the executor service. Also traffic from the UI application is routed to the scheduler service using its respective private endpoint.

Add application configurations for each app based on the table below. __Note that Linux Web Apps do not recognize colon as a configuration section separator.__ Instead double underscores are used.

|Setting|Value|
|-|-|
|__UI__||
|ConnectionStrings__AppDbContext|Connection string to the system database|
|Authentication|See the on-premise section for configuring authentication|
|EnvironmentName|`NameOfYourEnvironment`|
|Executor__Type|`WebApp`|
|Scheduler__Type|`WebApp`|
|Executor__WebApp__Url|Executor web app URL, e.g. `https://biflow-executor.azurewebsites.net`|
|Executor__WebApp__ApiKey|Executor web app API key|
|Scheduler__WebApp__Url|Scheduler web app URL, e.g. `https://biflow-scheduler.azurewebsites.net`|
|Scheduler__WebApp__ApiKey|Scheduler web app API key|
|WEBSITE_TIME_ZONE|Time zone, e.g. `Europe/Helsinki`|
|__Executor__||
|ConnectionStrings__AppDbContext|Connection string to the system database|
|Authentication__ApiKey|API key used to authenticate incoming requests to the executor app API|
|PollingIntervalMs|`5000` (default)|
|WEBSITE_TIME_ZONE|Time zone, e.g. `Europe/Helsinki`|
|__Scheduler__||
|ConnectionStrings__AppDbContext|Connection string to the system database|
|Authentication__ApiKey|API key used to authenticate incoming requests to the scheduler app API|
|Executor__Type|`WebApp`|
|Executor__WebApp__Url|Executor web app URL, e.g. `https://biflow-executor.azurewebsites.net`|
|Executor__WebApp__ApiKey|Executor web app API key|
|WEBSITE_TIME_ZONE|Time zone, e.g. `Europe/Helsinki`|

Deploying the application code can be done via the Linux virtual machine.
- Copy the zip files for the three applications to the virtual machine (`Biflow.Ui`, `Biflow.Executor.WebApp` and `Biflow.Scheduler.WebApp`). Make sure to delete the configuration sections from the appsettings files except for the `Logging` section.
- Connect remotely to the VM via SSH using e.g. PowerShell.
- <a href="https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt">Install the Azure CLI</a> on the Linux VM.
- Deploy the zip files to the respective Web Apps from the Linux VM.

The following commands act as a reference for how to achieve the above steps. They assume the Azure CLI has already been installed on the VM.

On your local machine, run the following PowerShell commands to copy files to the VM. Replace the values inside the angle brackets (including the brackets) with your corresponding values.

```
> scp .\BiflowExecutor.zip <admin_user>@<vm_public_ip>:/home/<admin_user_>
> scp .\BiflowScheduler.zip <admin_user>@<vm_public_ip>:/home/<admin_user_>
> scp .\BiflowUi.zip <admin_user>@<vm_public_ip>:/home/<admin_user_>
```

Then launch a remote session from PowerShell to the VM using ssh and deply the application code.

```
> ssh <admin_user>@<vm_public_ip>
#Enter Linux VM credentials

admin@biflow-vm:~$ az login
#Complete authentication flow

admin@biflow-vm:~$ az account set --subscription <azure_subscription_id>

admin@biflow-vm:~$ az webapp deploy --resource-group <resource_group_name> --name <executor_web_app_name> --src-path ./Biflow.Executor.WebApp.zip --type zip
#Repeat for the scheduler and UI applications

#Test connections
admin@biflow-vm:~$ curl --header "x-api-key: <executor_api_key_value>" https://<executor_web_app_name>.azurewebsites.net/connection/test

admin@biflow-vm:~$ curl --header "x-api-key: <scheduler_api_key_value>" https://<scheduler_web_app_name>.azurewebsites.net/status
```

The log streams for the UI application should be available from the Azure portal since network traffic to the app should be allowed from your location. The executor and scheduler applications' logs can be viewed from the VM using the following command templates. The commands shown here are for the executor application.

```
admin@biflow-vm:~$ username=$(az webapp deployment list-publishing-credentials --name <executor_web_app_name> --resource-group <resource_group_name> --query publishingUserName -o tsv)

admin@biflow-vm:~$ password=$(az webapp deployment list-publishing-credentials --name <executor_web_app_name> --resource-group <resource_group_name> --query publishingPassword -o tsv)

admin@biflow-vm:~$ curl -u "$username:$password" https://<executor_web_app_name>.scm.azurewebsites.net/api/logstream
```

## 4.4. First use and configuration

Some administrative tasks need to be done before the applications are ready for normal operation.

### Admin user

In order to be able to log in via the UI, an initial admin user needs to be added to the database. This can be achieved using the `AdminUser` configuration section of the UI application's settings. Make sure that credentials are provided in the application settings before starting the UI application service. The admin user is added during app startup.

#### Built-in authentication

When built-in authentication is enabled, a password must be provided for the admin user in the app settings. The username can be arbitrary, such as 'admin'.
```
"AdminUser": {
    "Username": "admin",
    "Password": "my!SECURE#passWord$9000"
}
```

#### Windows authentication
With Windows authentication, no password is required. Authentication happens at the OS level. The username must be a valid Active Directory or local user.
```
"AdminUser": {
    "Username": "DOMAIN\BiflowService"
}
```
#### Azure AD authentication
With Azure AD authentication, no password is required. Authentication happens using Microsoft Identity. The username must be a valid Azure AD account.
```
"AdminUser": {
    "Username": "admin@mycompany.com"
}
```
#### LDAP authentication
With LDAP authentication, no password is required. Authentication happens using the LDAP server. The `userPrincipalName` name attribute should be used to add users to Biflow user management.
```
"AdminUser": {
    "Username": "admin@mycompany.com"
}
```
Navigate to the Biflow UI website. You should be able to log in using the account specified above. With Windows authentication, the user is automatically logged in to Biflow using the account they are currently logged in as on their computer.

### Connections

- In order to create SQL, SSIS package, Agent job and tabular model execution steps, connections need to be defined in the UI application's settings (Settings => Connections).
    - SQL steps can reference MS SQL or Snowflake connections.
    - SSIS package, Agent job and tabular model steps can only reference MS SQL connections.

#### MS SQL (SQL Server)

- Both Windows Authentication (Integrated Security=SSPI) and SQL Server Authentication are supported for SQL Server connections.
    - If Windows Authentication is used, the account used to run the UI and scheduler applications is used to connect to the database.
- Use **either** of these two approaches based on the authentication method to set the necessary database level privileges on the target database.
    - Windows Authentication
        - Sufficient roles required by the ETL processes (usually `db_owner`)
        - `db_ssisadmin` role in the SSISDB database (only if SSIS packages are used)
    - SQL Server Authentication
        - No SSIS compatibility needed
            - sufficient roles required by the ETL processes (usually `db_owner`)
        - SSIS compatibility
            - In addition add `db_ssisadmin` role in the SSISDB database
            - Add a new login using a Windows account which also has `db_ssisadmin` role in the SSISDB database
            - Execute the following T-SQL command in the `master` database: `GRANT IMPERSONATE ON LOGIN::[WindowsLoginName] TO [SqlServerLoginName]`

#### MS SQL (Azure SQL)

- SQL Server Authentication and service principal authentication are supported. When the applications are hosted as Azure Web Apps, managed identities can be used to authenticate to Azure databases when they are located in the same Azure tenant.

#### Snowflake

- For the best compatibility, Snowflake connection strings should include the default database used with the connection:

```
ACCOUNT=ACCOUNT_NAME;HOST=xxxxx-accountname.snowflakecomputing.com;ROLE=user_role;WAREHOUSE=COMPUTE_WH;USER=my_user;PASSWORD=my_Secure_Password9000;DB=DATAWAREHOUSE;SCHEMA=PUBLIC;
```

### App registrations

1. In order to add Data Factories, Function Apps or to create Power BI dataset refresh steps, Azure app registrations need to be created and added.
2. Navigate to the target tenant’s Azure portal in portal.azure.com.
3. Go to Entra ID => App registration => New registration.
4. Register the app and save the client id and client secret someplace.
5. Add a new app registration in Biflow using these key information.

### Data Factory setup

1. Navigate to the target Data Factory resource and go to Access control (IAM).
2. Add the application created in the App registrations section as a Data Factory Contributor to the Data Factory resource.
3. In Biflow, navigate to Settings > Pipeline clients and add a new Data Factory instance with the corresponding information.

### Synapse Workspace setup

1. Navigate to the target Synapse Studio and go to Credential Management.
2. Add the application created in the App registrations sections with the following workspace roles:
    - Synapse Artifact User
    - Synapse Credential User
    - Synapse User
3. In Biflow, navigate to Settings > Pipeline clients and add a new Synapse instance with the corresponding information.

### Power BI Service setup

1. Create a security group in Azure Active Directory and add the application (service principal) created in the App registrations section as a member to that security group.
    - Azure Active Directory => Groups => New group
    - Select the newly created group and go to Members => Add members
2. Go to the target Power BI Service Admin portal.
    - Tenant settings => Developer settings => Allow service principals to use Power BI APIs
    - Enable this setting and add the security group created in the previous step to the list of specific security groups allowed to access Power BI APIs.
3. Give the service principal created in the App registrations section at least contributor access to the target workspaces in Power BI Service.
4. After these steps the Power BI connection can be tested on the App registration edit dialog.

### Azure Function App setup

1. Navigate to the Azure Function App resource and go to Access control (IAM).
2. Add the application created in the App registrations section as a Contributor to the Azure Function App resource.
3. In Biflow, navigate to Settings > Azure Function Apps and add a new Function App with the corresponding information. Note: It can take several minutes for the role changes in Azure to take effect.

# 5. Operation and administrative tasks

This section provides some useful information regarding normal operation and also administrative tasks.

## 5.1. Executions

When job executions are started (either scheduled or manual), the executor service first runs a series of validations on the execution. The executor checks for:
1. Circular job dependencies caused by job step references (i.e. steps starting another job's execution)
    - These can cause infinite execution loops and they are considered a user error (incorrect job & step definitions).
2. Circular step dependencies
    - These can cause infinite waits when step dependencies are being checked after the execution has started.
3. Illegal hybrid mode steps
    - In hybrid execution mode, steps in lower execution phases cannot depend on steps in higher execution phases, as this too would cause infinite wait.

If any of the checks fail, the entire execution and all its steps are marked as failed and aborted.

## 5.2. Services

When starting Biflow services, it is recommended to start them in the following order.
1. Executor service
2. Scheduler service
3. User interface service

The executor service does not depend on the scheduler or the UI, so it should be started first. The scheduler depends on the executor service and should be started next. The UI depends on both the executor and scheduler services and should be started last.

The scheduler service can operate even if the executor service is not running, but scheduled executions cannot be started until the executor service is running. The UI can also operate even if both services are down, but executions cannot be started manually and schedules cannot be managed.

When shutting down services, the recommended order is reversed.
1. User interface
2. Scheduler
3. Executor

### Executor service

The executor service does not run any major startup tasks. It does validate the executor settings (polling interval etc.) defined in `appsettings.json` or in app configurations in Azure. If the settings do not pass validation, the service will not start.

When the executor service is shut down, it will immediately send cancel commands to all steps currently being managed. If all steps are successfully canceled in 20 seconds, the service will shut down gracefully. After 20 seconds the service will forcefully shut down and abandon any steps that may have been left running. This may leave some steps and execution logs in an undefined state. Usually though 20 seconds should be enough for a graceful shutdown, if the polling interval is not too long.

### Scheduler service

On startup, the scheduler service reads all schedules from the app database and adds them to the internal scheduler. Disabled schedules are added as paused. If reading the schedules fails, the service will reattempt every 15 minutes until schedules are read successfully. The app database should thus be available at least soon after the scheduler service is started.

For various maintenance reasons (data platform service break, software updates etc.), all schedules may need to be disabled temporarily to prevent new executions from being started. This can achieved easily, efficiently and securely by shutting down the scheduler service. This guarantees that no new executions are started when the scheduler service is not running while also allowing the executor service to complete running executions.

### User interface service

The user interface service runs the admin user check on startup (see the AdminUser section of the user interface app settings).