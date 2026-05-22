using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskHub
{
//статический вспомогательный класс для ввода/вывода
    public static class ConsoleHelper
    {
        public static void PrintTasks(List<TaskItem> list)
        {
            if (list.Count == 0)
            {
                Console.WriteLine("(задач нет)");
                return;
            }
            foreach (TaskItem t in list)
            {
                Console.WriteLine(t.GetInfo());
                Console.WriteLine("------------------------------------------");
            }
        }

        public static string ReadLine(string prompt)
        {
            Console.Write(prompt);
            string s = Console.ReadLine();
            if (s == null)
            {
                s = "";
            }
            return s;
        }

        public static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = Console.ReadLine();
                try
                {
                    return int.Parse(s);
                }
                catch (Exception)
                {
                    Console.WriteLine("Это не число, попробуйте ещё раз.");
                }
            }
        }
    }

    public class Program
    {
        private const string FileName = "tasks.txt";

        // Main async чтобы использовать await
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            TaskManager manager = new TaskManager();

            // создаём хранилище и сразу пробуем загрузить старые задачи
            IStorage storage = new FileStorage(FileName);
            try
            {
                List<TaskItem> loaded = await storage.LoadAsync();
                manager.ReplaceAll(loaded);
                Console.WriteLine("Загружено задач: " + loaded.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при загрузке: " + ex.Message);
            }

            // запускаем фоновую проверку дедлайнов раз в 10 секунд
            DeadlineWatcher watcher = new DeadlineWatcher(manager, 10);
            watcher.Start();

            bool running = true;
            while (running)
            {
                PrintMenu();
                string choice = ConsoleHelper.ReadLine("Выберите пункт меню: ");

                try
                {
                    switch (choice)
                    {
                        case "1":
                            CreateTask(manager);
                            break;
                        case "2":
                            ViewTasksMenu(manager);
                            break;
                        case "3":
                            EditTask(manager);
                            break;
                        case "4":
                            DeleteTask(manager);
                            break;
                        case "5":
                            SearchMenu(manager);
                            break;
                        case "6":
                            ShowStats(manager);
                            break;
                        case "7":
                            await SaveTasks(manager, storage);
                            break;
                        case "8":
                            await LoadTasks(manager, storage);
                            break;
                        case "0":
                            running = false;
                            break;
                        default:
                            Console.WriteLine("Нет такого пункта меню.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка: " + ex.Message);
                }

                Console.WriteLine();
            }

            await SaveTasks(manager, storage);

            watcher.Dispose();
            storage.Dispose();

            Console.WriteLine("Выход. Пока!");
        }

        private static void PrintMenu()
        {
            Console.WriteLine("===== TaskHub =====");
            Console.WriteLine("1. Создать задачу");
            Console.WriteLine("2. Просмотр задач");
            Console.WriteLine("3. Редактировать задачу");
            Console.WriteLine("4. Удалить задачу");
            Console.WriteLine("5. Поиск задач");
            Console.WriteLine("6. Статистика");
            Console.WriteLine("7. Сохранить в файл");
            Console.WriteLine("8. Загрузить из файла");
            Console.WriteLine("0. Выход");
        }

        private static void CreateTask(TaskManager manager)
        {
            string title = ConsoleHelper.ReadLine("Название: ");
            string desc = ConsoleHelper.ReadLine("Описание: ");

            Priority priority = ReadPriority();
            Status status = ReadStatus();
            DateTime deadline = ReadDeadline();

            TaskItem task = new TaskItem(title, desc, priority, status, deadline);
            manager.AddTask(task);
            Console.WriteLine("Задача создана с Id = " + task.Id);
        }

        private static void ViewTasksMenu(TaskManager manager)
        {
            Console.WriteLine("1 - все, 2 - выполненные, 3 - невыполненные, 4 - высокий приоритет");
            string c = ConsoleHelper.ReadLine("Выбор: ");

            switch (c)
            {
                case "1":
                    ConsoleHelper.PrintTasks(manager.GetAll());
                    break;
                case "2":
                    ConsoleHelper.PrintTasks(manager.GetDone());
                    break;
                case "3":
                    ConsoleHelper.PrintTasks(manager.GetNotDone());
                    break;
                case "4":
                    ConsoleHelper.PrintTasks(manager.GetHighPriority());
                    break;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }

        private static void EditTask(TaskManager manager)
        {
            int id = ConsoleHelper.ReadInt("Id задачи для редактирования: ");
            TaskItem task = manager.FindById(id);
            if (task == null)
            {
                Console.WriteLine("Задача не найдена.");
                return;
            }

            Console.WriteLine("Текущая задача:");
            Console.WriteLine(task.GetInfo());

            string newTitle = ConsoleHelper.ReadLine("Новое название (Enter - оставить): ");
            if (newTitle != "")
            {
                task.Title = newTitle;
            }

            string newDesc = ConsoleHelper.ReadLine("Новое описание (Enter - оставить): ");
            if (newDesc != "")
            {
                task.Description = newDesc;
            }

            Console.WriteLine("Изменить приоритет? (y/n)");
            if (ConsoleHelper.ReadLine("> ") == "y")
            {
                task.Priority = ReadPriority();
            }

            Console.WriteLine("Изменить статус? (y/n)");
            if (ConsoleHelper.ReadLine("> ") == "y")
            {
                task.Status = ReadStatus();
            }

            Console.WriteLine("Задача обновлена.");
        }

        private static void DeleteTask(TaskManager manager)
        {
            int id = ConsoleHelper.ReadInt("Id задачи для удаления: ");
            bool ok = manager.RemoveTask(id);
            if (ok)
            {
                Console.WriteLine("Удалено.");
            }
            else
            {
                Console.WriteLine("Задача не найдена.");
            }
        }

        private static void SearchMenu(TaskManager manager)
        {
            Console.WriteLine("1 - по названию, 2 - по статусу, 3 - по приоритету");
            string c = ConsoleHelper.ReadLine("Выбор: ");

            switch (c)
            {
                case "1":
                    string title = ConsoleHelper.ReadLine("Введите часть названия: ");
                    ConsoleHelper.PrintTasks(manager.SearchByTitle(title));
                    break;
                case "2":
                    Status st = ReadStatus();
                    ConsoleHelper.PrintTasks(manager.SearchByStatus(st));
                    break;
                case "3":
                    Priority p = ReadPriority();
                    ConsoleHelper.PrintTasks(manager.SearchByPriority(p));
                    break;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }

        private static void ShowStats(TaskManager manager)
        {
            TaskStats stats = manager.GetStats();
            Console.WriteLine("Всего задач: " + stats.Total);
            Console.WriteLine("Выполнено: " + stats.Done);
            Console.WriteLine("Просрочено: " + stats.Overdue);
            Console.WriteLine("По приоритетам:");
            Console.WriteLine("   Low: " + stats.LowCount);
            Console.WriteLine("   Medium: " + stats.MediumCount);
            Console.WriteLine("   High: " + stats.HighCount);
        }

        private static async Task SaveTasks(TaskManager manager, IStorage storage)
        {
            try
            {
                await storage.SaveAsync(manager.GetAll());
                Console.WriteLine("Сохранено в файл.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения: " + ex.Message);
            }
        }

        private static async Task LoadTasks(TaskManager manager, IStorage storage)
        {
            try
            {
                List<TaskItem> loaded = await storage.LoadAsync();
                manager.ReplaceAll(loaded);
                Console.WriteLine("Загружено задач: " + loaded.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки: " + ex.Message);
            }
        }

        // вспомогательные методы ввода enum'ов 
        private static Priority ReadPriority()
        {
            while (true)
            {
                Console.WriteLine("Приоритет: 0 - Low, 1 - Medium, 2 - High");
                string s = ConsoleHelper.ReadLine("> ");
                if (s == "0") return Priority.Low;
                if (s == "1") return Priority.Medium;
                if (s == "2") return Priority.High;
                Console.WriteLine("Неверный ввод.");
            }
        }

        private static Status ReadStatus()
        {
            while (true)
            {
                Console.WriteLine("Статус: 0 - New, 1 - InProgress, 2 - Done");
                string s = ConsoleHelper.ReadLine("> ");
                if (s == "0") return Status.New;
                if (s == "1") return Status.InProgress;
                if (s == "2") return Status.Done;
                Console.WriteLine("Неверный ввод.");
            }
        }

        private static DateTime ReadDeadline()
        {
            while (true)
            {
                string s = ConsoleHelper.ReadLine("Дедлайн (дд.мм.гггг чч:мм): ");
                try
                {
                    return DateTime.ParseExact(s, "dd.MM.yyyy HH:mm",
                        System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    Console.WriteLine("Неверный формат даты. Пример: 31.12.2026 18:00");
                }
            }
        }
    }
}