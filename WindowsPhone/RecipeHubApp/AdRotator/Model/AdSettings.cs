using System.Collections.Generic;

namespace AdRotator.Model
{
    /// <summary>
    /// Class storing the list of <see cref="AdCultureDescriptor"/>s.
    /// </summary>
    public class AdSettings
    {
        /// <summary>
        /// String to identify the default culture
        /// </summary>
        public const string DEFAULT_CULTURE = "default";

        /// <summary>
        /// The list of the culture descriptors
        /// </summary>
        public List<AdCultureDescriptor> CultureDescriptors { get; set; }
    }
}
