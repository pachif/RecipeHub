using System.Collections.Generic;
using System.Xml.Serialization;

namespace AdRotator.Model
{
    public class AdCultureDescriptor
    {
        /// <summary>
        /// The name of the culture, e.g. en-US
        /// </summary>
        [XmlAttribute("CultureName")]
        public string CultureName { get; set; }

        /// <summary>
        /// Listing of the probabilities for ads
        /// </summary>
        [XmlElement("Probabilities")]
        public List<AdProbability> AdProbabilities { get; set; }
    }
}
