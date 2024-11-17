using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Biflow.Ui.Components;

public class ValidationStateChangeListener : ComponentBase, IDisposable
{
    [CascadingParameter] public EditContext? CurrentEditContext { get; set; }

    [Parameter] public Action<IEnumerable<string>>? OnValidationStateChanged { get; set; }

    private Action<IEnumerable<string>>? _previousOnValidationStateChanged;
    private EditContext? _previousEditContext;
    private EventHandler<ValidationStateChangedEventArgs>? _validationStateChangedHandler;

    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(ValidationStateChangeListener)} requires a cascading parameter " +
                $"of type {nameof(EditContext)}. For example, you can use {nameof(ValidationStateChangeListener)} inside " +
                $"an {nameof(EditForm)}.");
        }

        if (CurrentEditContext != _previousEditContext)
        {
            DetachValidationStateChangedListener();
            _previousEditContext = CurrentEditContext;
        }

        if (OnValidationStateChanged == _previousOnValidationStateChanged)
        {
            return;
        }
        
        _validationStateChangedHandler = (_, _) => OnValidationStateChanged?.Invoke(CurrentEditContext.GetValidationMessages());
        CurrentEditContext.OnValidationStateChanged += _validationStateChangedHandler;
        _previousOnValidationStateChanged = OnValidationStateChanged;
    }

    public void Dispose()
    {
        DetachValidationStateChangedListener();
    }

    private void DetachValidationStateChangedListener()
    {
        if (_previousEditContext is not null)
        {
            _previousEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }
    }
}
