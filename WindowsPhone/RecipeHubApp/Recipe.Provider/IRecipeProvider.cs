using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recipes.Provider
{
    public interface IRecipeProvider
    {
        string ProviderName { get; }
        void ObtainRecipeById(string id);
        void SearchRecipeByName(string name);
        void SearchRecipeByName(string name, int page);
        void ObtainMostRecents();

        event EventHandler<ResultEventArgs> ProcessEnded;
    }
}
