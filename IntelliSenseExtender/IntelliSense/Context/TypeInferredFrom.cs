namespace IntelliSenseExtender.IntelliSense.Context
{
    public enum TypeInferredFrom
    {
        None,
        VariableDeclaration,
        Assignment,
        MethodArgument,
        ReturnValue,
        PropertyInilialyzer,
        ExpressionBody,
        BinaryExpression
    }
}
