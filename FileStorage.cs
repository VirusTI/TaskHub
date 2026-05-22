using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TaskHub
{
    // интерфейс хранилища
    public interface IStorage : IDisposable
    {
        Task SaveAsync(List<TaskItem> tasks);
        Task<List<TaskItem>> LoadAsync();
    }

    public class FileStorage : IStorage
    {
        private readonly string filePath;
        private bool disposed = false;

        public FileStorage(string path)
        {
            this.filePath = path;
        }

        //асинхроное сохранение
        public async Task SaveAsync(List<TaskItem> tasks)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("FileStorage");
            }

            StringBuilder sb = new StringBuilder();
            foreach (TaskItem t in tasks)
            {
                sb.AppendLine(t.ToFileLine());
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        // асинхронная загрузка
        public async Task<List<TaskItem>> LoadAsync()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("FileStorage");
            }

            List<TaskItem> result = new List<TaskItem>();

            if (!File.Exists(filePath))
            {
                return result;
            }

            string content = await File.ReadAllTextAsync(filePath);
            string[] lines = content.Split(new char[] { '\n', '\r' },
                                           StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                try
                {
                    TaskItem t = TaskItem.FromFileLine(line);
                    result.Add(t);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не удалось прочитать строку: " + ex.Message);
                }
            }

            return result;
        }

        // паттерн dispose
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Console.WriteLine("[FileStorage] ресурсы освобождены.");
            }
            GC.SuppressFinalize(this);
        }
    }
}