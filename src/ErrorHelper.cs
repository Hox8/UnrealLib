using System;

namespace UnrealLib
{
    // @TODO starting to move away from this idea. Cumbersome and clunky. Should look into throwing exceptions instead

    // Really struggling with C#'s "No multiple base class inheritance" approach...
    // This can't be used with classes already inheriting from a class, such as UnrealArchive or UObject.
    // Interface approach is God-awful so I'm just going to leave this here as-is.

    // Generic enums MUST use 0 for its "None" value
    public abstract class ErrorHelper<T> where T : Enum
    {
        public T ErrorType { get; private set; } = default(T);     // Ensure defaults to 0, which is what all enums using this helper class should base as its "None" value
        public string? ErrorContext { get; private set; }

        // Allocatey.
        public bool HasError => !ErrorType.Equals(default(T));

        public void SetError(T type, string? context = null)
        {
            ErrorType = type;
            ErrorContext = context;
        }

        public abstract string GetErrorString();
    }
}
