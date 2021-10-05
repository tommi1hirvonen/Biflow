using System.Threading.Tasks;

namespace EtlManagerUi.Shared.JobDetails.StepEdit.StepEditModal
{
    interface IStepEditModal
    {
        public Task ShowAsync(bool showDependencies = false);
    }
}
