namespace Marco.ExceptionHandling.Abstractions
{
    public class ForExceptionBehavior
    {
        public int StatusCode { get; set; }
        public ExceptionHandlerBehavior Behavior { get; set; }
    }
}