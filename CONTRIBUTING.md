# Development

## Setting up / development environment

Visual Studio 2022 or JetBrains Rider is recommended as the IDE for development.

<a href="https://marketplace.visualstudio.com/items?itemName=Failwyn.WebCompiler64">Web Compiler 2022+</a> Visual Studio extension is used to compile the Scss code in `Biflow.Ui/wwwroot/scss/bootstrap.custom.scss`. The configuration file for compiling the Scss code is located in `Biflow.Ui/compilerconfig.json`.

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
