namespace Xdxd.DotNet.Shared;

// /// <summary>
// /// Simple result type, uses generic T for the value and string[] for the errors.
// /// Defines a bunch of constructor methods for convenience.
// /// </summary>
// public class Result
// {
//     public bool IsOk => this.ErrorMessages == default || this.ErrorMessages.Length == 0;
//
//     public string[] ErrorMessages { get; set; }
//
//     public static Result Ok()
//     {
//         return new Result();
//     }
//
//     public static Result<T> Ok<T>(T payload)
//     {
//         return new Result<T> { Payload = payload };
//     }
//
//     public static Result<T> Ok<T>()
//     {
//         return new Result<T> { Payload = default };
//     }
//
//     public static Result<T> Error<T>(string message)
//     {
//         return new Result<T> { ErrorMessages = new[] { message } };
//     }
//
//     public static Result<T> Error<T>(string[] errorMessages)
//     {
//         return new Result<T> { ErrorMessages = errorMessages };
//     }
//
//     public static Result Error(string message)
//     {
//         return new Result { ErrorMessages = new[] { message } };
//     }
//
//     public static Result Error(string[] errorMessages)
//     {
//         return new Result { ErrorMessages = errorMessages };
//     }
//
//     public virtual Result<object> ToGeneralForm()
//     {
//         return new Result<object>
//         {
//             ErrorMessages = this.ErrorMessages,
//             Payload = null,
//         };
//     }
// }

public abstract class ResultBase
{
    public abstract Result<object, object> ToGeneralForm();

    public static Result<TPayload, object> Ok<TPayload>(TPayload payload)
    {
        return new Result<TPayload, object>(true, payload, default);
    }
}

public class Result<TPayload, TError> : ResultBase
{
    public Result(bool isOk, TPayload payload, TError error)
    {
        this.IsOk = isOk;
        this.Payload = payload;
        this.Error = error;
    }

    public bool IsOk { get; }

    public TError Error { get; }

    public TPayload Payload { get; }

    public override Result<object, object> ToGeneralForm()
    {
        return new Result<object, object>(this.IsOk, this.Payload, this.Error);
    }

    // public static Result<TPayload, TError> Ok()
    // {
    //     return new() { IsOk = true, Payload = default, Error = default };
    // }
    //
    // public static Result<TPayload, TError> Ok(TPayload payload)
    // {
    //     return new() { IsOk = true, Payload = payload, Error = default };
    // }
    //
    // public static Result<TPayload, TError> Fail(TError error)
    // {
    //     return new() { IsOk = false, Payload = default, Error = error };
    // }
}

// public static implicit operator Result<TPayload, TError>(TPayload payload)
// {
//     return Ok(x);
// }
//
// public static implicit operator Result<T>(string errorMessage)
// {
//     return Error<T>(errorMessage);
// }
//
// public static implicit operator Result<T>(string[] errorMessages)
// {
//     return Error<T>(errorMessages);
// }
//
// public Result<object, object> ToGeneralForm()
// {
//     return new Result<object, object>
//     {
//         Payload = this.Payload,
//         Error = this.Error,
//     };
// }
