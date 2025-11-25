namespace Coordix
{
    /// <summary>
    /// Defines the mode used for handler resolution and execution.
    /// </summary>
    public enum HandlerResolutionMode
    {
        /// <summary>
        /// Uses reflection-based handler resolution and execution.
        /// This is the default mode and provides good performance with cached delegates.
        /// </summary>
        Reflection = 0,

        /// <summary>
        /// Uses code generation for handler resolution and execution.
        /// This mode requires the Coordix.CodeGen package to be installed and its registration
        /// method to be called. The CodeGen package will provide a code-generated implementation
        /// of IHandlerExecutor that replaces reflection with compile-time generated code.
        /// </summary>
        CodeGenPreferred = 1
    }
}

