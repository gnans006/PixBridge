namespace EventPhoto.Domain.Exceptions;

/// <summary>
/// Thrown when a business rule is violated.
/// </summary>
public class BusinessRuleException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="rule">The violated business rule name.</param>
    /// <param name="message">The violation message.</param>
    public BusinessRuleException(string rule, string message)
        : base(message)
    {
        Rule = rule;
    }

    /// <summary>
    /// Gets the name of the violated business rule.
    /// </summary>
    public string Rule { get; }
}
