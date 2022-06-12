namespace Biflow.Ui.Shared.Settings;

public interface IPipelineClientEditModal
{
    public Task ShowAsync(Guid pipelineClientId);
}
