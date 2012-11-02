using System.Diagnostics;

namespace Common.interfaces
{
    /// <summary>
    /// Interface defining methods to give information about an instance
    /// Can be used with the <see cref="DebuggerDisplayAttribute"/> as in [DebuggerDisplay("{ToInfo()}")]
    /// </summary>
    public interface IInfo
    {
        /// <summary>
        /// Should call <see cref="ToInfo(bool)"/>  with false as argument
        /// </summary>
        /// <returns>A string containing long information about the instance</returns>
        string ToInfo();

        /// <summary>
        /// Generates a short or long info string of the instance.
        /// Can be used on the Console or when logging
        /// </summary>
        /// <param name="shortInfo">Generate a short info string if set to <c>true</c>, a long if <c>false</c>.</param>
        /// <returns>A string containing long or short information about the instance</returns>
        string ToInfo(bool shortInfo);

        /// <summary>
        /// Should call <see cref="ToInfo(bool)"/> with true as argument on the argument IInfo instance if the argument is not null
        /// or return a corresponding string if the instance is null.
        /// </summary>
        /// <param name="info">The IInfo instance to handle</param>
        /// <returns>A string containing short information about the argument instance</returns>
        string ToInfo(IInfo info);
    }
}
