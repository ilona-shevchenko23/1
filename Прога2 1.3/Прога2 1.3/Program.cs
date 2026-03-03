using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Прога2_1._3
{
    class Program
    {
        static void Main()
        {
            
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                List<int> list = new List<int> { 2, 5, -2, 8, -3 };
                Console.WriteLine("Ilona");

                Console.WriteLine("Початковий список: " + string.Join(", ", list));

                if (list.Where(x => x < 0).Count() == 0)
                {
                    Console.WriteLine("Результат: 0");
                }
                else
                {
                    int minNegative = list.Where(x => x < 0).Min();
                    Console.WriteLine("Найменше від'ємне число: " + minNegative);
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Сталася помилка: " + ex.Message);
            }
            finally 
            {
                Console.WriteLine("\nЗавершення роботи програми.");
                Console.ReadLine(); 
            }
        }
    }
}