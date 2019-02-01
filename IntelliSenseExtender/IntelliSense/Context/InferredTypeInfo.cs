using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.IntelliSense.Context
{
    public class InferredTypeInfo
    {
        public TypeInferredFrom From { get; set; }
        public ITypeSymbol Type { get; set; }

        // Only applicable if From == MethodArgument
        public IParameterSymbol ParameterSymbol { get; set; }
    }
}
