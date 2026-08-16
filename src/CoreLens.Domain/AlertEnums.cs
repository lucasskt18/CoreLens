namespace CoreLens.Domain;

public enum AlertSeverity
{
    Warning = 1,
    Critical = 2
}

public enum AlertOperator
{
    GreaterThan = 1,
    LessThan = 2,
    GreaterOrEqual = 3,
    LessOrEqual = 4
}
