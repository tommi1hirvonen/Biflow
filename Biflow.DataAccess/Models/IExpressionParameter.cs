namespace Biflow.DataAccess.Models;

public interface IExpressionParameter<TJobParameter> where TJobParameter : ParameterBase
{
    public string ParameterName { get; set; }

    public TJobParameter InheritFromJobParameter { get; set; }

    public Guid InheritFromJobParameterId { get; set; }
}
