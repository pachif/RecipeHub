
using RecipeHubApp.Resources;

namespace RecipeHubApp
{
    public class ApplicationResx
    {
        public ApplicationResx()
        {
        }

        private static AppResx mylocalizedresx = new AppResx();

        public AppResx LocalizedStrings
        {
            get { return mylocalizedresx; }
        }
    }
}
