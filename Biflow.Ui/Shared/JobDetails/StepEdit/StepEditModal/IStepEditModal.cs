using System.Threading.Tasks;

namespace Biflow.Ui.Shared.JobDetails.StepEdit.StepEditModal
{
    interface IStepEditModal
    {
        public Task ShowAsync(StepEditModalView startView = StepEditModalView.Settings);
    }
}
