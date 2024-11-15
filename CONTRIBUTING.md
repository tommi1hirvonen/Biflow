# Development

## Setting up / development environment

Visual Studio 2022 is recommended as the IDE for development.

<a href="https://marketplace.visualstudio.com/items?itemName=Failwyn.WebCompiler64">Web Compiler 2022+</a> Visual Studio extension is used to compile the Scss code in `Biflow.Ui/wwwroot/scss/bootstrap.custom.scss`. The configuration file for compiling the Scss code is located in `Biflow.Ui/compilerconfig.json`.

## Adding new step types

The following new or existing files/classes need to be added or edited when adding support for new step types. Other files and classes may also need to be updated or created if there are references from the new step to other classes or resources (e.g. app registrations, pipeline clients, function apps etc.).

### Biflow.Core

**Add new classes**

- FooStep
  - Inherits Step
  - Implements IHasConnection<> if the step relies on a SQL connection
  - Implementes IHasTimeout if the step supports timing out
  - Implements IHasStepParameters<> if the step supports parameters
  - Implement and override the abstract base methods `Copy()` and `ToStepExecution()`
- FooStepParameter (only if the step supports parameters)
  - Inherits StepParameterBase
- FooStepExecution
  - Inherits StepExecution
  - Implements IHasTimeout if the step execution supports timing out
  - Implements IHasStepExecutionparameters<> if the step execution supports parameters
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
  - Inherits StepExecutor<FooStepExecution,FooStepExecutionAttempt>

**Update existing classes**

- JobExecutorFactory
  - Add possible new navigation property include statements to the initial execution load command.

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

### Biflow.Ui.Core

**Update existing classes**

- VersionRevert
  - Include potential new endpoint collections when handling environment version snapshot reverts.