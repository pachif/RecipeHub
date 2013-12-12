using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace Recipes.BusinessObjects
{
    public class Recipe
    {
        public string Author { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Ingridients { get; set; }
        public string Procedure { get; set; }
        public string Title { get; set; }
        public string LinkUrl { get; set; }
        public string MainIngredient { get; set; }
        public int Portions { get; set; }
        public List<Alarm> Alarms { get; set; }
    }
}
