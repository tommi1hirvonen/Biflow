namespace Biflow.Ui.Components.Shared.StepEditModal;

interface IStepEditModal
{
    public Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings);
}
