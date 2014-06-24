using System.ComponentModel;
using System.Xml.Serialization;
using System.Xml;

namespace AdRotator.Model
{
    /// <summary>
    /// Describes the probability of and ad with AdType appearing.
    /// </summary>
    public class AdProbability:INotifyPropertyChanged
    {
        private double _probabilityValue;

        private double _totalProbabilityValues;

        private AdType _adType;

        private string _appIDvalue;

        private string _secondaryIDvalue;

        private bool _isTest;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The probability to show the ad. This can be any double, though for simplicity
        ///     reasons it's advised that all Probability values in <see cref='AdCultureDescriptor'/> 
        ///     add up to 100.
        /// </summary>
        [XmlAttribute("Probability")]
        public double ProbabilityValue
        {
            get
            {
                return _probabilityValue;
            }
            set
            {
                var oldValue = _probabilityValue;
                _probabilityValue = value;
                if (_probabilityValue != oldValue)
                {
                    OnPropertyChanged("Probability");
                }
            }
        }

        [XmlIgnore]
        public double ProbabilityPercentage
        {
            get
            {
                if (TotalProbabilityValues == 0)
                {
                    return 0;
                }
                return (ProbabilityValue / TotalProbabilityValues) * 100;
            }
        }

        /// <summary>
        /// The probability values of all the ads that this ad might appear together with.
        /// </summary>
        [XmlIgnore]
        public double TotalProbabilityValues
        {
            get
            {
                return _totalProbabilityValues;
            }
            set
            {
                var oldValue = _totalProbabilityValues;
                _totalProbabilityValues = value;
                if (_totalProbabilityValues != oldValue)
                {
                    OnPropertyChanged("TotalProbabilityValues");
                }
            }
        }

        /// <summary>
        /// The type of the ad that <see cref='ProbabilityValue'/> is associated with.
        /// </summary>
        [XmlAttribute("AdType")]
        public AdType AdType
        {
            get
            {
                return _adType;
            }
            set
            {
                var oldValue = _adType;
                _adType = value;
                if (_adType != oldValue)
                {
                    OnPropertyChanged("AdType");
                }
            }
        }

        /// <summary>
        /// The probability to show the ad. This can be any double, though for simplicity
        ///     reasons it's advised that all Probability values in <see cref='AdCultureDescriptor'/> 
        ///     add up to 100.
        /// </summary>
        [XmlAttribute("AppID")]
        public string AppID
        {
            get
            {
                return _appIDvalue;
            }
            set
            {
                var oldValue = _appIDvalue;
                _appIDvalue = value;
                if (_appIDvalue != oldValue)
                {
                    OnPropertyChanged("AppID");
                }
            }
        }

        /// <summary>
        /// The probability to show the ad. This can be any double, though for simplicity
        ///     reasons it's advised that all Probability values in <see cref='AdCultureDescriptor'/> 
        ///     add up to 100.
        /// </summary>
        [XmlAttribute("SecondaryID")]
        public string SecondaryID
        {
            get
            {
                return _secondaryIDvalue;
            }
            set
            {
                var oldValue = _secondaryIDvalue;
                _secondaryIDvalue = value;
                if (_secondaryIDvalue != oldValue)
                {
                    OnPropertyChanged("SecondaryID");
                }
            }
        }


        /// <summary>
        /// The probability to show the ad. This can be any double, though for simplicity
        ///     reasons it's advised that all Probability values in <see cref='AdCultureDescriptor'/> 
        ///     add up to 100.
        /// </summary>
        [XmlIgnore]
        public bool IsTest
        {
            get
            {
                return _isTest;
            }
            set
            {
                var oldValue = _isTest;
                _isTest = value;
                if (_isTest != oldValue)
                {
                    OnPropertyChanged("IsTest");
                }
            }
        }

        /// <summary>Get a value purely for serialization purposes</summary>
        [XmlAttribute("IsTest")]
        public string IsTestSerialize
        {
            get { return this.IsTest ? "1" : "0"; }
            set { this.IsTest = XmlConvert.ToBoolean(value.ToLowerInvariant()); }
        }

        public AdProbability()
        {
        }

        public AdProbability(AdType adType, int probability)
        {
            AdType = adType;
            ProbabilityValue = probability;
        }

        public AdProbability(AdType adType, int probability, string appID, string secondaryID, bool isTest)
        {
            AdType = adType;
            ProbabilityValue = probability;
            AppID = appID;
            SecondaryID = secondaryID;
            IsTest = IsTest;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
