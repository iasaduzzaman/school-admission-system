namespace SchoolAdmission.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}
