namespace Biflow.Ui.Shared.StepEditModal;

interface IStepEditModal
{
    public Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings);
}
