using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerUtils
{
    public class Result
    {
        public bool Success { get; }
        public string ErrorMessage { get; private set; }
        
        protected Result(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static Result Failure(string errorMessage)
        {
            return new Result(false, errorMessage);
        }

        public static Result<T> Failure<T>(string errorMessage)
        {
            return new Result<T>(default, false, errorMessage);
        }

        public static Result Ok()
        {
            return new Result(true, string.Empty);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, string.Empty);
        }
    }

    public class Result<T> : Result
    {
        public T Value { get; set; }

        protected internal Result(T value, bool success, string errorMessage)
            : base(success, errorMessage)
        {
            Value = value;
        }
    }
}
