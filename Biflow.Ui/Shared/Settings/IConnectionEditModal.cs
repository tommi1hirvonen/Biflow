namespace Biflow.Ui.Shared.Settings;

interface IConnectionEditModal
{
    public Task ShowAsync(Guid connectionId);
}
