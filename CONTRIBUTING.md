# Development

## Setting up / development environment

Visual Studio 2022 is recommended as the IDE for development.

<a href="https://marketplace.visualstudio.com/items?itemName=Failwyn.WebCompiler64">Web Compiler 2022+</a> Visual Studio extension is used to compile the Scss code in `Biflow.Ui/wwwroot/scss/bootstrap.custom.scss`.

## Adding new step types

The following new or existing files/classes need to be added or edited when adding support for new step types. Other files and classes may also need to be updated or created if there are dependencies from the new step to other classes or resources (e.g. app registrations, pipeline clients, function apps etc.).

### Biflow.DataAccess

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

- StepType
  - Add a new enum value for the new step type
- SqlConnectionInfo
  - If the new step type relies on a SQL connection, add navigation property from SqlConnectionInfo to a list of steps of the new type
- AppDbContext
  - Add DbSet<> property for FooStep
  - Add discriminator mapping for FooStepExecution
  - Add discriminator mapping for FooStepExecutionAttempt
  - Add discriminator mapping for FooStep
  - If FooStep supports parameters
    - Add discriminator mapping for FooStepParameter
    - Add navigation property mapping between FooStep and FooStepParameter
    - Add discriminator mapping for FooStepExecutionParameter
    - Add navigation property mapping between FooStepExecution and FooStepExecutionParameter
- DuplicatorExtensions
    - Add potential new navigation paths that should be considered when copying steps and jobs.

### Biflow.Database

**Update existing tables**

- Step
  - Add new step type identifier to the step type check constraint
  - Add new columns required by the new step type (based on FooStep class definition)
- StepParameter (only if step supports parameters)
  - Add new step type identifier to the parameter type check constraint
  - Add possible new columns required by the new step type (based on FooStepParameter class definition)
- ExecutionStep
  - Add new step type identifier to the step type check constraint
  - Add new columns required by the new step type (sames as in Step table)
- ExecutionStepAttempt
  - Add new step type identifier to the step type check constraint
  - Add new columns required by the new step type (based on FooStepExecutionAttempt class)
- ExecutionStepParameter (only if step supports parameters)
  - Add new step type identifier to the parameter type check constraint
  - Add possible new columns required by the new step type (same as in StepParameter table)

### Biflow.Executor.Core

**Add new classes**

- FooStepExecutor
  - Implements IStepExecutor<,>

**Update existing classes**

- JobExecutorFactory
  - Add possible new navigation property include statements to the initial execution load command.
- Extensions
  - Register new StepOrchestrator service using the new step executor class.
- StepOrchestratorProvider
  - Add mapping from FooStepExecution and FooStepExecutionAttempt to the appropriate step orchestrator and executor.

### Biflow.Ui

**Add new classes**

- FooStepEditModal
  - Inherits StepEditModal<>

**Update existing classes**

- StepTypeIconComponent
  - Add case and icon implementation for FooStep step type
- StepDetailsModal
  - Add properties for the new step type
- StepExecutionDetailsComponent
  - Add properties for the new step type
- StepsComponent
  - Add new step type to IsStepTypeDisabled()
  - Add FooStepEditModal to the end of the component
- DependenciesComponent
  - Add FooStepEditModal to the end of the component