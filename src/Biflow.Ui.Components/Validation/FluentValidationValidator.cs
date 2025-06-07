using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Biflow.Ui.Components;

public class FluentValidationValidator : ComponentBase
{
    [CascadingParameter] private EditContext? EditContext { get; set; }

    [Parameter, EditorRequired] public IValidator? Validator { get; set; }

    private ValidationMessageStore? _validationMessageStore;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // Keep a reference to the original values so we can check if they have changed
        var previousEditContext = EditContext;

        await base.SetParametersAsync(parameters);

        if (EditContext is null)
            throw new NullReferenceException($"{nameof(FluentValidationValidator)} must be placed within an {nameof(EditForm)}");

        if (Validator is null)
            throw new NullReferenceException($"{nameof(Validator)} must be specified.");

        // If the EditForm.Model changes then we get a new EditContext
        // and need to hook it up
        if (EditContext != previousEditContext)
        {
            _validationMessageStore = new ValidationMessageStore(EditContext);
            // Hook up edit context events
            EditContext.OnValidationRequested += ValidationRequested;
            EditContext.OnFieldChanged += FieldChanged;
        }
    }

    public async Task ValidateAsync()
    {
        ArgumentNullException.ThrowIfNull(EditContext);
        ArgumentNullException.ThrowIfNull(Validator);
        ArgumentNullException.ThrowIfNull(_validationMessageStore);

        _validationMessageStore.Clear();
        var validationContext = new ValidationContext<object>(EditContext.Model);
        var result = await Validator.ValidateAsync(validationContext);
        AddValidationResult(EditContext.Model, result);
    }

    private async void ValidationRequested(object? sender, ValidationRequestedEventArgs args) => await ValidateAsync();

    private async void FieldChanged(object? sender, FieldChangedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(Validator);
        ArgumentNullException.ThrowIfNull(_validationMessageStore);

        var fieldIdentifier = args.FieldIdentifier;
        _validationMessageStore.Clear(fieldIdentifier);
        
        if (!Validator.CanValidateInstancesOfType(fieldIdentifier.Model.GetType()))
        {
            return;
        }

        var propertiesToValidate = new[] { fieldIdentifier.FieldName };
        var fluentValidationContext =
            new ValidationContext<object>(
                instanceToValidate: fieldIdentifier.Model,
                propertyChain: new FluentValidation.Internal.PropertyChain(),
                validatorSelector: new FluentValidation.Internal.MemberNameValidatorSelector(propertiesToValidate)
            );

        var result = await Validator.ValidateAsync(fluentValidationContext);

        AddValidationResult(fieldIdentifier.Model, result);
    }

    private void AddValidationResult(object model, ValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(EditContext);
        ArgumentNullException.ThrowIfNull(_validationMessageStore);

        foreach (var error in validationResult.Errors)
        {
            var fieldIdentifier = new FieldIdentifier(model, error.PropertyName);
            _validationMessageStore.Add(fieldIdentifier, error.ErrorMessage);
        }
        EditContext.NotifyValidationStateChanged();
    }
}
