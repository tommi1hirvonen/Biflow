using System.Threading.Tasks;

namespace Biflow.Ui.Shared.StepEditModal;

interface IStepEditModal
{
    public Task ShowAsync(StepEditModalView startView = StepEditModalView.Settings);
}
