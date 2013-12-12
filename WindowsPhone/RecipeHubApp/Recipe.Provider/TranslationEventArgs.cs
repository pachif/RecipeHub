using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recipes.Provider
{
    public class TranslationEventArgs : EventArgs
    {
        public string TranslatedText { get; set; }
    }
}
