#  Biflow

Powerful Business Intelligence workflow orchestration

## Requirements

Some requirements apply when running Biflow either on-premise or in Azure but some requirements are common.

### Common
- SQL Server database to host the system database
    - SQL Server 2012 or newer (tested on 2017 and 2019)
        - Edition: Express or above
    - Azure SQL Database or Managed Instance
- Email notifications
    - An email account is needed to send email notifications from the application

### On-premise
- Windows Server
    - ASP.NET 7 Hosting Bundle installed
- Windows account
    - Biflow can operate using either Windows Authentication or SQL Server Authentication
    - SSIS compatibility can be achieved using either method, but Windows Authentication is simpler and recommended.
    - AD account vs. local Windows account
        - If the SQL Server instance is located on a separate server from the application server, then an Active Directory (AD) account is recommended.
        - If the SQL Server instance is located on the same server, then a local Windows account can be used.
    - The Windows account used to run Biflow should have Logon as Service rights on the application server.

### Azure
- Azure App Service (Linux)
    - Minimum B2/B3 level is recommended
    - For advanced networking, minimum S1 level is required for the App Service

## Authentication

Three methods of authentication are supported:
- Built-in
    - User management and authentication is done using a built in identity provider.
    - MFA is not supported.
- Windows (Active Directory)
    - Authentication is done at the OS level using Active Directory accounts.
- Azure Active Directory
    - Users are authenticated using their Microsoft organizational accounts.
    - Requires an app registration to be created in the host tenant's Active Directory.
    - Requires internet access

In all cases the list of authorized users and their access is managed in the application.

## Architecture

There are three ways to configure Biflow from an architecture point of view: on-premise, Azure (monolithic) and Azure (microservices).

### On-premise

The on-premise option takes advantage of OS level features such as Windows Services (used for the scheduler service), console applications and running separate processes for different executions. If an on-premise installation is possible, this is the option that allows for most control of the setup.

### Azure (monolithic)

The Azure (monolithic) architecture has all the necessary components and services hosted inside one monolithic application. The application is running in an Azure App Service (Linux) as a Web App. This allows for efficient cost minimization through the use of lower tier App Service Plans (B1, B2 and B3).

### Azure (microservices)

The Azure (microservices) approach closely resembles the on-premise architecture. However, the executor console app is now replaced with an executor service running as an Azure Web App. From the two Azure architectures, this offers significantly more control over upgrades to different components of the application.

# Documentation

## Source code solution and projects

|Project|Description|
|-|-|
|Database|SQL Server database project containing definitions for all table structures and stored procedures|
|DataAccess|Data access layer project responsible for ORM (Object-relational mapping). Contains all model definitions as well as the definitions for the Entity Framework Core database context.|
|Executor.Core|Core library for the executor application|
|Executor.ConsoleApp|Console front-end project for the executor application|
|Executor.WebApp|Web App front-end project for the executor application|
|Scheduler.Core|Core library for the scheduler service|
|Scheduler.WebApp|Web App front-end project for the scheduler service|
|Ui|Blazor Server UI application. Can be configured to host the executor and scheduler services internally.|
|Ui.Core|Common UI services that can be shared between different UI versions/editions|
|Utilities|Common utilities library project|

## Execution statuses

### Possible job execution statuses

|Status|Description|
|-|-|
|NotStarted |The executions has not yet been started by the executor|
|Running    |One or more steps included in the execution have not yet been completed.|
|Succeeded  |All steps included in the execution succeeded.|
|Failed     |One or more steps failed for the execution.|
|Warning    |There were duplicate steps in the execution or some steps were retried.|
|Stopped    |The execution was manually stopped.|
|Suspended  |There were unstarted steps remaining in the execution when the execution was finished.|

### Possible step execution statuses

|Status|Description|
|-|-|
|NotStarted |The step has not been started but is awaiting execution. All dependencies may not have yet been completed or there may be too many steps running in parallel. Job executions that have been termiated unexpectedly may have steps left with this status.|
|Running    |The step is currently executing|
|Succeeded  |The step was completed successfully|
|Failed     |The step encountered an exception and failed|
|Skipped    |Some strict dependencies defined for the step failed or the step's execution condition was not met. Thus the step was skipped.|
|AwaitRetry |The step has failed. A retry attempt will be made after the specified interval.|
|Stopped    |User has manually stopped the execution of the entire job or of this specific step.|
|Duplicate  |A different job execution instance with the same step was found running at the same time as this was step was due for execution. The execution of this step was skipped.|

## User Roles

- Viewer
    - Can view
        - Job details
        - Step details
        - Schedules
        - Executions
    - Can subscribe for notifications
- Operator
    - In addition to the same rights as the Viewer role
        - Can manage schedules
        - Can execute jobs manually
        - Can edit job parameters and concurrency

- Editor
    - In addition to the same rights as the Operator role
        - Can edit jobs and steps
- Admin
    - In addition to the same rights as the Editor role
        - Can manage users and global settings
        - Can manage other users' subscriptions

## Encryption

Data saved and processed by Biflow is not encrypted by default on the database level. If you want to implement database level encryption of sensitive data, this can be achieved using the Always Encrypted feature of SQL Server and Azure SQL Database.

More information about Always Encrypted can be found in <a href="https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/always-encrypted-database-engine?view=sql-server-ver15">Microsoft’s documentation.</a>

If you want to implement Always Encrypted, these columns are good candidates for encryption:
- biflow.AccessToken.Token
- biflow.AppRegistration.ClientSecret
- biflow.Connection.ConnectionString
- biflow.FunctionApp.FunctionAppKey
- biflow.Step.FunctionKey

If Always Encrypted is utilized, this should be reflected in the connection strings set in the application settings (BiflowContext). Always Encrypted is enabled with the following connection string property: `Column Encryption Setting=enabled`

# Installation

There are three different installation alternatives: on-premise, Azure (monolithic) and Azure (microservices).

## On-premise

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

### ASP.NET 7 Hosting Bundle

1. On the machine where the UI application should be installed, install the ASP.NET 7 Hosting Bundle. Follow the instructions on the <a href="https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-7.0.0-windows-hosting-bundle-installer">.NET download page</a>.

### Executor console application

1. Extract the BiflowExecutor.zip file to the C: root on the application server (C:\Biflow\BiflowExecutor).
2. Update the appsettings.json file with the correct settings for your environment.

|Setting|Description|
|-|-|
|ConnectionStrings:BiflowContext|Connection string used to connect to the Biflow system database based on steps taken in the database section of this guide. **Note:** the connection string must have `MultipleActiveResultSets=true` enabled.|
|EmailSettings|Settings used to send email notifications.|
|PollingIntervalMs|Time interval in milliseconds between status polling operations (applies to some step types). Default value is `5000`.|
|MaximumParallelSteps|Maximum number of parallel steps allowed during execution. Default value is `10`.|
|Serilog:WriteTo:Args:Path|Path where application will write is log files. Default value is `C:\\Biflow\\BiflowExecutor\\log\\executor.log`.|

3. Test the executor application by opening the command prompt and typing in
    - `C:\Biflow\BiflowExecutor\BiflowExecutor.exe get-commit`
    - This should run without errors and return the commit SHA for the current version of the application.
4. Test the executor application's connection to the system database by running the following command
    - `C:\Biflow\BiflowExecutor\BiflowExecutor.exe test-connection`

### Scheduler service

1. Extract the BiflowScheduler.zip file to the C: root on the application server (C:\Biflow\BiflowScheduler).
2. Update the appsettings.json file with the correct settings

|Setting|Description|
|-|-|
|ConnectionStrings:BiflowContext|Connection string used to connect to the Biflow database based on steps taken in the database section of this guide. **Note:** The connection string must have `MultipleActiveResultSets=true` enabled.|
|Executor:Type|`[ ConsoleApp \| WebApp ]`|
||Whether the executor is installed as a console app or web app|
|Executor:ConsoleApp:BiflowExecutorPath|Needed only when `Executor:Type` is set to `ConsoleApp`. Path to the executor executable. Default value is `C:\\Biflow\\BiflowExecutor\\BiflowExecutor.exe`|
|Executor:WebApp:Url|Needed only when `Executor:Type` is set to `WebApp`. Url to the executor web app|
|Authorization:Windows:AllowedUsers|Array of Windows users who are authorized to issue requests to the scheduler API, e.g. `[ "DOMAIN\\BiflowService", "DOMAIN\\AdminUser" ]`. If no authorization is required, remove the `Authorization:Windows` section.|
|Kestrel:Endpoints:Http:Url|The http url and port which the scheduler API should listen to, for example `http://localhost:5432`. If there are multiple installations on the same server, the scheduler applications should listen to different ports.|
|Serilog:WriteTo:Args:path|Path where the application will write its log files. Default value is `C:\\Biflow\\BiflowScheduler\\log\\scheduler.log`|

3. Open the Windows command terminal in **administrator mode**.
    - Run the following command: `sc.exe create BiflowScheduler binpath= C:\Biflow\BiflowScheduler\BiflowScheduler.exe start= delayed-auto displayname= "Biflow Scheduler"`
4. Open Windows Services, navigate to the service "Biflow Scheduler", right click and select Properties.
    - Add the login information for the service account used to run the service and scheduled executions. If Windows Authentication is used to connect to the database, then this account’s credentials are used to connect.
    - Start the service.
    - Navigate to `C:\Biflow\BiflowScheduler\log` and open the log text file. There should be no error reports if the scheduler was able to connect to the database and load schedules into the service.

### User interface web application

1. If Azure AD is used for authentication, an app registration needs to be created in the hosting tenant's Azure Active Directory.
    - Navigate to the target tenant’s Azure portal in portal.azure.com.
    - Go to Azure Active Directory => App registration => New registration.
    - Register the app and save the client id and client secret someplace. You will not be able to access the client secret after it has been created.
    - Add a redirect URL for the app registration under Manage => Authentication => Add a platform
        - Select **Web**
        - **Redirect URI**: This should be the URL where the UI application can be reached appended with `/signin-oidc`. For example, if the application can be reached at `https://contoso.azurewebsites.net` then the redirect URI should be `https://contoso.azurewebsites.net/signin-oidc`
        - **Front-channel logout URL**: Similarly, the logout URL is the UI app's URL appended with `/signout-oidc`. With the base URL of the example above, the logout URL would be `https://contoso.azurewebsites.net/signout-oidc`
        - **Implicit grant and hybrid flows**: Select **ID tokens**
        - **Supported account types**: In most cases, the correct option will be *Accounts in this organizational directory only*. However, if you need to be able to authorize users outside your organization to access the UI, select *Accounts in any organizational directory*.
2. Extract the BiflowUi.zip file to the C: root on the application server (C:\Biflow\BiflowUi)).
    - The account created in step 1 should have read/write access to this folder.
3. In the folder where you extracted the installation zip file, locate the file appsettings.production.json. Update the file with the correct settings.

|Setting|Description|
|-|-|
|EnvironmentName|Name of the installation environment to be shown in the UI (e.g. Production, Test, Dev etc.)|
|ConnectionStrings:BiflowContext|Connection string used to connect to the Biflow database based on steps taken in the database section of this guide. **Note:** The connection string must have `MultipleActiveResultSets=true` enabled.|
|Authentication|`[ BuiltIn \| Windows \| AzureAd ]`|
||`BuiltIn`: Users accounts and passwords are managed in Biflow. Users are application specific.
||`Windows`: Authentication is done using Active Directory. User roles and access are defined in the Biflow users management.|
||`AzureAd`: Authentication is done using Azure Active Directory.  User roles and access are defined in the Biflow users management.|
|AzureAd|This section needs to be defined only if `Authentication` is set to `AzureAd`|
|AzureAd:Instance|`https://login.microsoftonline.com/`|
|AzureAd:Domain|Your organization domain, e.g. `contoso.com`|
|AzureAd:TenantId|If the app registration supports *Accounts in this organizational directory only*, set the value to the directory (tenant) ID (a GUID) of your organization. If the registration supports *Accounts in any organizational directory*, set the value to `organizations`. If the registration supports *All Microsoft account users*, set the value to `common`.|
|AzureAd:ClientId|The application (client) ID of the application that you registered in the Azure portal.|
|AzureAd:ClientSecret|The client secret for the app registration.|
|AzureAd:CallbackPath|`/signin-oidc`|
|Executor:Type|`[ ConsoleApp \| WebApp \| SelfHosted ]`|
||Whether the executor service is installed as a console application or web app or is running self-hosted inside the UI application|
|Executor:WebApp:Url|Needed only when `Executor:Type` is set to `WebApp`. Url to the executor web app API|
|Executor:ConsoleApp:BiflowExecutorPath|Needed only when `Executor:Type` is set to `ConsoleApp`. Path to the executor executable. Default value is `C:\\Biflow\\BiflowExecutor\\BiflowExecutor.exe`|
|Executor:SelfHosted|This section needs to be defined only if `Executor:Type` is set to `SelfHosted`. Refer to the executor console application's settings section to set the values in this section.|
|Scheduler:Type|`[ WebApp \| SelfHosted ]`|
||Whether the scheduler service is installed as a web app or is running self-hosted inside the UI application. If `Executor:Type` is set to `SelfHosted` then this settings must also be set to `SelfHosted`|
|Scheduler:WebApp:Url|Needed only when `Scheduler:Type` is set to `WebApp`. Url to the scheduler service web app API|
|Kestrel:Endpoints:Https:Url|The https url and port which the UI application should listen to, for example https://localhost. If there are multiple installations on the same server, the UI applications should listen to different ports. Applies only on-premise installations.|
|Serilog:WriteTo:Args:path|Folder path where the application will write its log files. Applies only to on-premise installations. Default value is `C:\\Biflow\\BiflowUi\\log\\ui.log`|

4. Open the Windows command terminal in **administrator mode**.
    - Run the following command: `sc.exe create BiflowUi binpath= C:\Biflow\BiflowUi\BiflowUi.exe start= delayed-auto displayname= "Biflow User Interface"`
5. Open Windows Services, navigate to the service "Biflow Scheduler", right click and select Properties.
    - Add the login information for the service account used to run the service and scheduled executions. If Windows Authentication is used to connect to the database, then this account’s credentials are used to connect.
    - Start the service.
    - Navigate to `C:\Biflow\BiflowScheduler\log` and open the log text file. There should be no error reports if the scheduler was able to connect to the database and load schedules into the service.

## Azure (monolithic)

- Create a new App Service Plan (Linux)
    - Recommended pricing tier B1-B3
- Create a new Web App and set the following settings
    - Publish: Code
    - Runtime stack: .NET 7 (STS)
    - Operating System: Linux
    - Linux Plan: Previously created App Service Plan
- When the Web App has been created, go to the resource and its Configuration settings.
    - General Settings => Websocket => On
    - General Settings => Always on => On
- Set the application settings in Configuration => Application settings.
    - Application settings
        - EnvironmentName = NameOfYourEnvironment
        - Executor__SelfHosted__MaximumParallelSteps = 5
        - Executor__SelfHosted__PollingIntervalMs = 5000
        - Executor__Type = SelfHosted
        - Scheduler__Type = SelfHosted
    - Connection string
        - BiflowContext
            - Connection string to the Biflow system database
- Using System Assigned Managed Identities for authentication to the system database is recommended to avoid having to save sensitive information inside connection strings.

## Azure (microservices)

TODO

## First use

Some administrative tasks need to be done before the applications are ready for normal operation.

### Admin user

Create an admin user in the Biflow database using the stored procedure `biflow.UserAdd`. How the user is created depends on the selected authentication method.

#### Built-in authentication
```
exec biflow.UserAdd
    @Username = 'admin',
    @Password = 'adminpassword',
    @Role = 'Admin',
    @Email = 'admin@mycompany.com'
```
#### Windows authentication
With Windows authentication, no password is required. Authentication happens at the OS level.
```
exec biflow.UserAdd
    @Username = 'DOMAIN\BiflowService',
    @Password = null,
    @Role = 'Admin',
    @Email = 'admin@mycompany.com'
```
#### Azure AD authentication
With Azure AD authentication, no password is required. Authentication happens using Microsoft Identity.
```
exec biflow.UserAdd
    @Username = 'admin@mycompany.com',
    @Password = null,
    @Role = 'Admin',
    @Email = 'admin@mycompany.com'
```
Navigate to the Biflow UI website. You should be able to log in using the account specified above. With Windows authentication, the user is automatically logged in to Biflow using the account they are currently logged in as on their computer.

### Connections

1. In order to create SQL, SSIS package or tabular model execution steps, connections need to be defined in the UI application's settings (Settings => Connections).
2. Both Windows Authentication (Integrated Security=SSPI) and SQL Server Authentication are supported.
    - If Windows Authentication is used, the account used to run the UI and scheduler applications is used to connect to the database.
3. Use **either** of these two approaches based on the authentication method to set the necessary database level privileges on the target database.
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

### App registrations

1. In order to add Data Factories, Function Apps or to create Power BI dataset refresh steps, Azure app registrations need to be created and added.
2. Navigate to the target tenant’s Azure portal in portal.azure.com.
3. Go to Azure Active Directory => App registration => New registration.
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
3. Give the service principal created in the App registrations section at least member access to the target workspaces in Power BI Service.
4. After these steps the Power BI connection can be tested on the App registration edit dialog.

### Azure Function App setup

1. Navigate to the Azure Function App resource and go to Access control (IAM).
2. Add the application created in the App registrations section as a Contributor to the Azure Function App resource.
3. In Biflow, navigate to Settings > Azure Function Apps and add a new Function App with the corresponding information. Note: It can take several minutes for the role changes in Azure to take effect.