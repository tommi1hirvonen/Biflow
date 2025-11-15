# Development

## Setting up / development environment

Visual Studio 2026 or JetBrains Rider is recommended as the IDE for development.

## Running locally

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
2. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) and make sure it is running.
   - Docker is used to host the SQL Server container for the app database.
   - Aspire will automatically create this container on the first launch.
3. Clone the repository locally.
4. In a terminal, navigate to the `Biflow.Aspire.SqlAppHost` folder.
5. Run `dotnet run`
   - Alternatively you can run the project in Visual Studio or Rider. 
   - This will start the Aspire app host, which in turn will start and orchestrate the app database container, backend API and fronted projects.
   - The first launch may take a while as Aspire creates and spins up the SQL Server container and applies migrations.
6. Using a browser, navigate to the Aspire dashboard, which can be found at the URL printed in the console.
   - `Login to the dashboard at https://localhost:<port>/login?t=<token>`
   - Especially on the first launch, resources may take a while to start. You can monitor the status of the resources on the dashboard until all are running. Aspire will first create and spin up the SQL container, then apply migrations and only then start the API and frontend resources.
7. Navigate to the UI by following one of the `frontend` resource URLs (https or http) found on the dashboard
8. You can login using the admin credentials found in the UI project app settings file (`Biflow.Ui/appsettings.json`). Look for the `AdminUser` section.

## Compiling the UI CSS

The UI uses the Bootstrap frontend toolkit (CSS, JavaScript and icons) and the Havit.Blazor.Components.Web.Bootstrap NuGet package for reusable Blazor components. The app CSS is generated from the Bootstrap SCSS files using SASS. This allows for easier customization of the Bootstrap theme. The following guide describes how to compile the CSS using SASS.

#### Install SASS

1. Install [Node.js](https://nodejs.org/en/download)
2. Install [SASS](https://sass-lang.com/install) using npm
  - `npm install -g sass`

#### Compile the CSS

1. In a terminal, navigate to the `Biflow.Ui/wwwroot/css` folder
2. Run `sass bootstrap.custom.scss:bootstrap.custom.css`

## Adding new step types

The following new classes need to be added and existing files/classes need to be edited when adding support for new step types. Other files and classes may also need to be updated or created if there are references from the new step to other classes or resources (e.g. Azure credentials, pipeline clients, function apps etc.).

### Biflow.Core

**Add new classes**

- FooStep
  - Inherits Step
  - Implements IHasConnection<> if the step relies on a SQL connection
  - Implements IHasTimeout if the step supports timing out
  - Implements IHasStepParameters<> if the step supports parameters
  - Implement and override the abstract base methods `Copy()` and `ToStepExecution()`
- FooStepParameter (only if the step supports parameters)
  - Inherits StepParameterBase
- FooStepExecution
  - Inherits StepExecution
  - Implements IHasTimeout if the step execution supports timing out
  - Implements IHasStepExecutionParameters<> if the step execution supports parameters
- FooStepExecutionParameter (only if the step execution supports parameters)
  - Inherits StepExecutionParameterBase
- FooStepExecutionAttempt
  - Inherits StepExecutionAttempt

**Update existing classes**

- EnvironmentSnapshot
  - Add any potential new endpoint collections to the environment snapshot. Take care not to serialize any sensitive data (use the JsonSensitive attribute for sensitive properties).
- StepType
  - Add enum value for new step type
  - Apply the `[Category]` and `[Description]` attributes to the new enum value.
- Step
  - Add JsonDerivedType attribute for the new step type
- StepExecution
  - Add JsonDerivedType attribute for the new step type
- StepExecutionAttempt
  - Add JsonDerivedType attribute for the new step type
- ParameterType

### Biflow.DataAccess

**Add new classes**

- FooStepEntityTypeConfiguration
- FooStepExecutionEntityTypeConfiguration
- If FooStep supports parameters
  - FooStepParameterEntityTypeConfiguration
  - FooStepExecutionParameterEntityTypeConfiguration

**Update existing classes**

- AppDbContext
  - Add DbSet<> property for FooStep
  - Add DbSet<> property for any potential new endpoints
- StepEntityTypeConfiguration
- StepExecutionEntityTypeConfiguration
- StepExecutionAttemptEntityTypeConfiguration
- If FooStep supports parameters
  - StepParameterEntityTypeConfiguration
  - StepExecutionParameterEntityTypeConfiguration
- DuplicatorExtensions
    - Add potential new navigation paths that should be considered when copying steps and jobs.
- EnvironmentSnapshotBuilder
  - Initialize potential new endpoint collections
- Extensions
  - Include new step type in execution graph query

### Biflow.Executor.Core

**Add new classes**

- FooStepExecutor
  - Implements IStepExecutor

**Update existing classes**

- StepExecutorProvider
  - Add mapping from FooStepExecution and FooStepExecutionAttempt to FooStepExecutor
- JobExecutorFactory
  - Add possible new navigation property include statements to the initial execution load command

### Biflow.Ui.Core

**Add new classes**

- CreateFooStep
  - Command handler for creating new FooStep instance in the mediator pattern
- UpdateFooStep
  - Command handler for updating and existing FooStep in the mediator pattern

**Update existing classes**

- StepValidator
  - Add potential new validation logic for the new step type.
- VersionRevert
  - Include potential new endpoint collections when handling environment version snapshot reverts.

### Biflow.Ui

**Add new classes**

- FooStepEditModal
  - Inherits StepEditModal<>

**Update existing classes**

- StepTypeIcon
  - Add case and icon implementation for FooStep step type
- StepDetailsModal
  - Add properties for the new step type
- StepExecutionDetails
  - Add properties for the new step type
- StepsList
  - Add new step type to IsStepTypeDisabled()
  - Add FooStepEditModal to the end of the component
- DependenciesGraph
  - Add FooStepEditModal to the end of the component

### Biflow.Ui.Api

**Add new classes**

- FooStepDto
  - Record type used for endpoint inputs

**Update existing classes**

- StepsCreateEndpoints
  - Add endpoint for FooStep
- StepsUpdateEndpoints
  - Add endpoint for FooStep
