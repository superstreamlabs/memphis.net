namespace ProtoBufEval;

internal class ProtoBufEvalMissingDependencyException : Exception
{
    public ProtoBufEvalMissingDependencyException(string message) : base(message)
    {
    }
}