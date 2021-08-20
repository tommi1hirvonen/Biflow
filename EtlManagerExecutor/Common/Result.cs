namespace EtlManagerExecutor
{
    public abstract class Result
    {
        public string? InfoMessage { get; }

        protected Result(string? infoMessage)
        {
            InfoMessage = infoMessage;
        }

        public static Success Success(string? infoMessage = null) => new(infoMessage);

        public static Failure Failure(string errorMessage, string? infoMessage = null) => new(errorMessage, infoMessage);
        
    }

    public class Success : Result
    {
        internal Success(string? infoMessage = null) : base(infoMessage)
        {
        }
    }

    public class Failure : Result
    {
        public string ErrorMessage { get; }

        internal Failure(string errorMessage, string? infoMessage) : base(infoMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}