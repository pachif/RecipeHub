using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace RecipeHubApp
{
    public static class DataStorage
    {

        public static void WriteHistoryFile(Stack<ItemViewModel> stack)
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("RecipeHub.xml", FileMode.Create, myIsolatedStorage))
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    using (XmlWriter writer = XmlWriter.Create(isoStream, settings))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Recipes");

                        while (stack.Count > 0)
                        {
                            var itemVM = stack.Pop();

                            writer.WriteStartElement("Recipe");

                            writer.WriteElementString("Title", itemVM.Title);
                            writer.WriteElementString("Author", itemVM.Author);
                            writer.WriteElementString("Link", itemVM.RecipeLink);
                            writer.WriteElementString("ImageLink", itemVM.ImageRecipeLink);

                            // Ends Recipe Element
                            writer.WriteEndElement();
                        }
                        //- Ends Recipes
                        writer.WriteEndElement();
                        // Ends the document
                        writer.WriteEndDocument();
                        // Write the XML to the file.
                        writer.Flush();
                    }
                }
            }
        }

        public static List<ItemViewModel> ReadHistoryFile()
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    IsolatedStorageFileStream isoFileStream = myIsolatedStorage.OpenFile("RecipeHub.xml", FileMode.Open);
                    using (StreamReader reader = new StreamReader(isoFileStream))
                    {
                        string filestring = reader.ReadToEnd();
                        var list = new List<ItemViewModel>();
                        var recetas = ObtainRecipes(filestring);
                        foreach (XElement xElement in recetas)
                        {
                            string title = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("Title")).Value;
                            string author = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("Author")).Value;
                            string image = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("ImageLink")).Value;
                            string link = xElement.Elements().SingleOrDefault(x => x.Name.LocalName.Equals("Link")).Value;
                            var viewModel = new ItemViewModel
                            {
                                Author = author,
                                Title = title,
                                RecipeLink = link
                            };
                            viewModel.SetImageRecipeFrom(image);
                            list.Add(viewModel);
                        }

                        return list;
                    }
                }
                catch (IsolatedStorageException)
                {
                    // has no file is the first time
                    return null;
                }
            }

        }

        public static void WriteBackgroundSetting(string currentBackground)
        {
            const string fileName = "RecipeHub.txt";
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists(fileName))
                    myIsolatedStorage.DeleteFile(fileName);
                var stream = myIsolatedStorage.CreateFile(fileName);
                using (StreamWriter isoStream = new StreamWriter(stream))
                {
                    isoStream.WriteLine(currentBackground);
                }
            }
        }

        public static void WriteSettingsFile(Dictionary<string,string> currentSettings)
        {
            const string fileName = "RecipeHub.txt";
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(fileName))
                    myIsolatedStorage.DeleteFile(fileName);
                var stream = myIsolatedStorage.CreateFile(fileName);
                using (StreamWriter isoStream = new StreamWriter(stream))
                {
                    foreach (var item in currentSettings)
                    {
                        isoStream.WriteLine("{0}={1}",item.Key,item.Value);
                    }
                }
            }
        }

        public static string ReadBackgroundSetting()
        {
            return ReadSetting("background");
        }

        public static string ReadAlarmDetectionSetting()
        {
            return ReadSetting("detection");
        }

        private static string ReadSetting(string settingKey)
        {
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    IsolatedStorageFileStream isoFileStream = myIsolatedStorage.OpenFile("RecipeHub.txt", FileMode.Open);
                    using (StreamReader reader = new StreamReader(isoFileStream))
                    {
                        string background = null;
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (line.Contains(settingKey))
                            {
                                background = line.Split('=')[1];
                                break;
                            }
                        }
                        return background;
                    }
                }
                catch (IsolatedStorageException)
                {
                    return null;
                }
            }
        }

        private static List<XElement> ObtainRecipes(string htmldoc)
        {
            XElement doc = XElement.Parse(htmldoc);
            return doc.Elements().ToList();
        }

    }
}
