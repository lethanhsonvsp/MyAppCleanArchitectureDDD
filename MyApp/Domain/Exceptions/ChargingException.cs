namespace MyApp.Domain.Exceptions;

public class ChargingException : Exception
{
    public ChargingException(string message) : base(message) { }
}

public class InvalidParametersException : ChargingException
{
    public InvalidParametersException(string message) : base(message) { }
}

public class FaultActiveException : ChargingException
{
    public FaultActiveException(string message) : base(message) { }
}