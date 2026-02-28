---
apply: always
---

## AI Instructions: Add Support for a New Step Type

Use the following instructions when handling requests to add a new step type.
Also reference the `CONTRIBUTING.md` file in the project root.

### 1) Required Clarifications (ask first if missing)

Before generating implementation steps, confirm all of the following with the user:

1. **Step type name** (e.g., `Foo`)
2. **What the step does** (execution behavior and target system)
3. **Whether new integrations/endpoints are needed** in the application data model
4. **Whether the step supports step parameters**

If any of these are missing, **pause and ask follow-up questions** before proceeding.

---

### 2) Follow-up Prompt Template (use when info is incomplete)

Use this exact style:

```markdown
Before I generate the implementation plan, please confirm:

1. What is the exact name of the new step type?
2. What should this step do at runtime?
3. Does it require any new integrations/endpoints in the data model (e.g., credentials, clients, connection objects)?
4. Should this step support step parameters?

Once you confirm these, I’ll provide a project-by-project implementation checklist.
```


---

### 3) Implementation Checklist Scope (after clarifications are provided)

After required details are confirmed, provide a **project-by-project plan** covering:

- **Core model updates** for the new step, execution, and attempt types
- **Optional parameter model updates** if parameter support is enabled
- **Enum/discriminator updates** so type registration/serialization includes the new step type
- **Data access updates** (entity configurations, context registration, graph/query updates, duplication/versioning paths)
- **Executor updates** (executor implementation + provider/factory wiring)
- **UI Core updates** (create/update handlers, validation, version revert handling)
- **UI updates** (edit modal, details views, icon/type mappings, step type availability checks)
- **API updates** (DTO + create/update endpoints)
- **Integration-related model changes** only if the user says new integrations are required

---

### 4) Rules for Response Quality

When producing the plan:

- Be explicit about **what is always required** vs **conditional** (e.g., parameters/integrations).
- Use the user’s chosen step name consistently (`<StepName>Step`, `<StepName>StepExecution`, `<StepName>StepExecutionAttempt`, etc.).
- Include validation and wiring checkpoints to avoid partial implementation.
- Keep output concise, actionable, and ordered by project.

---

### 5) Output Format for Final Plan

When all required clarification is present, structure output as:

1. **Assumptions confirmed**
2. **Per-project required changes**
3. **Conditional changes**
4. **Verification checklist** (build, migrations if applicable, UI visibility, execution path, API behavior)

---
