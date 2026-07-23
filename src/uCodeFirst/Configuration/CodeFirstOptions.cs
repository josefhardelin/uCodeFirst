namespace uCodeFirst.Configuration;

internal sealed class CodeFirstOptions
{
    public bool Enabled { get; set; } = true;

    public CodeFirstStrategy Strategy { get; set; } = CodeFirstStrategy.NonDestructive;
}

internal enum CodeFirstStrategy
{
    NonDestructive,
    Destructive
}
