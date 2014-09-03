using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recipes.Provider
{
    public class TranslationEventArgs : EventArgs
    {
        public TranslationEventArgs(string p)
        {
            this.TranslatedText = p;
        }

        public string TranslatedText { get; set; }
    }
}
