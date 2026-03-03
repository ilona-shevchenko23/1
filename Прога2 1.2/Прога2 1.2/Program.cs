using System;
using System.Collections.Generic;
using System.IO;
using System.Text; 
using Newtonsoft.Json;

namespace Прога2_1._2
{
    class Program
    {
        static void Main()
        {
            
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
              
                List<string> list1 = new List<string> { "Apple", "Banana", "Cherry" };
                List<int> list2 = new List<int> { 10, 20, 30 };


                // (Ключ - текст із першого списку, Значення - цілий список чисел)
                Dictionary<string, List<int>> myDict = new Dictionary<string, List<int>>();

                // Перетворити перший список у набір ключів, а другий список додати до кожного ключа у вигляді списку
                foreach (string key in list1)
                {
                    myDict.Add(key, list2);
                }

                // Збереження у JSON файл
                string jsonText = JsonConvert.SerializeObject(myDict);

                
                Console.WriteLine("Результат роботи програми (JSON):");
                Console.WriteLine(jsonText);
                Console.WriteLine("-----------------------------------");

                
                File.WriteAllText("result.json", jsonText, Encoding.UTF8);

                Console.WriteLine("Готово! Словник створено і збережено у result.json");
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Сталася помилка: " + ex.Message);
            }
            finally 
            {
            
                Console.ReadLine();
            }
        }
    }
}