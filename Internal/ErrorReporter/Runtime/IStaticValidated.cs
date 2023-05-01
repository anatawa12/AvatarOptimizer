using System.Collections.Generic;

namespace Anatawa12.AvatarOptimizer.ErrorReporting
{
    /// <summary>
    /// The marker interface for components to be validated.
    /// If the class implements <see cref="ISelfStaticValidated"/>, The validation is performed by the class itself and
    /// if not, you must register validator to <c>ComponentValidation</c>.
    /// </summary>
    public interface IStaticValidated
    {
    }

    /// <summary>
    /// The interface for components to be self-validated.
    /// </summary>
    public interface ISelfStaticValidated : IStaticValidated
    {
        IEnumerable<ErrorLog> CheckComponent();
    }
}
