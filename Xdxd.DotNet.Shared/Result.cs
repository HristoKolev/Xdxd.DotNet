namespace Xdxd.DotNet.Shared;

using System;
using System.ComponentModel;

public abstract class Result
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract Result<object, object> ToGeneralForm();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract Type GetPayloadType();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract Type GetErrorType();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract bool GetIsOk();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract object GetPayload();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract object GetError();

    public static Result<TPayload, TError> Ok<TPayload, TError>()
    {
        return new(true, default, default);
    }

    public static Result<TPayload, TError> Ok<TPayload, TError>(TPayload payload)
    {
        return new(true, payload, default);
    }

    public static Result<TPayload, string[]> Ok<TPayload>(TPayload payload)
    {
        return new(true, payload, default);
    }

    public static Result<TPayload, string[]> Ok<TPayload>()
    {
        return new(true, default, default);
    }

    public static Result<object, object> Ok()
    {
        return new(true, default, default);
    }

    public static Result<TPayload, TError> Fail<TPayload, TError>(TError error)
    {
        return new(false, default, error);
    }
}

public class Result<TPayload, TError> : Result
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override Result<object, object> ToGeneralForm()
    {
        return new(this.IsOk, this.GetPayload(), this.GetError());
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override Type GetPayloadType()
    {
        return typeof(TPayload);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override Type GetErrorType()
    {
        return typeof(TError);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool GetIsOk()
    {
        return this.IsOk;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override object GetPayload()
    {
        return this.Payload;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override object GetError()
    {
        return this.Error;
    }
}
