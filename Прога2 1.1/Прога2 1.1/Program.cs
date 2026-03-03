using System;
using System.Collections.Generic;
using System.IO;
using System.Text; 

namespace Прога2_1._1
{
    // Клас для збереження даних про адресу
    public class Address
    {
        public string Street { get; set; }
        public int Index { get; set; }

        public Address(string street, int index)
        {
            Street = street;
            Index = index;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string filePath = "addresses.txt";
            List<Address> C1 = new List<Address>();

            // Обробки винятків
            try
            {
                // Читання даних з файлу і заповнення списку C1
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(',');

                    if (parts.Length == 2)
                    {
                        string street = parts[0].Trim();
                        // Якщо тут замість числа будуть букви, виникне помилка формату
                        int index = int.Parse(parts[1].Trim());

                        Address newAddress = new Address(street, index);
                        C1.Add(newAddress);
                    }
                }

                // Сортування списку C1 за індексом (впорядкування за зростанням)
                for (int i = 0; i < C1.Count - 1; i++)
                {
                    for (int j = 0; j < C1.Count - i - 1; j++)
                    {
                        if (C1[j].Index > C1[j + 1].Index)
                        {
                            (C1[j], C1[j + 1]) = (C1[j + 1], C1[j]);
                        }
                    }
                }

                Console.WriteLine("Список C1 до стиснення (впорядкований за індексом):");
                foreach (Address addr in C1)
                {
                    Console.WriteLine(addr.Street + " - " + addr.Index);
                }

                // Стиснення списку C1 шляхом видалення дублікатів вулиць
                List<Address> compressedC1 = new List<Address>();

                foreach (Address addr in C1)
                {
                    bool isDuplicate = false;

                    foreach (Address compressedAddr in compressedC1)
                    {
                        if (compressedAddr.Street == addr.Street)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (isDuplicate == false)
                    {
                        compressedC1.Add(addr);
                    }
                }

                C1 = compressedC1;

                Console.WriteLine("\nСписок C1 після стиснення:");
                foreach (Address addr in C1)
                {
                    Console.WriteLine(addr.Street + " - " + addr.Index);
                }
            }
            catch (FileNotFoundException)
            {

                Console.WriteLine("Помилка: Файл 'addresses.txt' не знайдено!");
            }
            catch (FormatException)
            {

                Console.WriteLine("Помилка: Неправильний формат даних у файлі. Індекс має бути числом.");
            }
            catch (Exception ex)
            {
           
                Console.WriteLine("Сталася невідома помилка: " + ex.Message);
            }
            finally
            {
                
                Console.WriteLine("\nЗавершення роботи програми.");
                Console.ReadLine();
            }
        }
    }
}