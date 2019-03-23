using System;

namespace Marco.ExceptionHandling.Abstractions
{
    public interface IExceptionHandlerEvent
    {
        bool IsElegible(int statusCode, Exception exception);
        (int statusCode, Exception exception, ExceptionHandlerBehavior behavior) Intercept(int statusCode, Exception exception);
    }
}