using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TaskHub
{
    // Фоновый наблюдатель за дедлайнами.
    // запускает отдельную задачу которая раз в несколько секунд проверяет просроченные задачи
    public class DeadlineWatcher : IDisposable
    {
        private readonly TaskManager manager;
        private readonly int intervalSeconds;

        // токен для аккуратной остановки потока
        private CancellationTokenSource cts;
        private Task workerTask;

        // lock чтобы не печатать уведомление одновременно с основным меню
        private static readonly object consoleLock = new object();

        private bool disposed = false;

        public DeadlineWatcher(TaskManager manager, int intervalSeconds)
        {
            this.manager = manager;
            this.intervalSeconds = intervalSeconds;
            this.cts = new CancellationTokenSource();
        }

        public void Start()
        {
            // запускаем фоновую задачу
            workerTask = Task.Run(() => Loop(cts.Token));
        }

        private async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(intervalSeconds * 1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                // получаем просроченные задачи у менеджера
                List<TaskItem> overdue = manager.GetOverdueTasks();

                if (overdue.Count > 0)
                {
                    lock (consoleLock)
                    {
                        ConsoleColor old = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine();
                        Console.WriteLine("=== [Фоновая проверка] Просроченные задачи: " +
                                          overdue.Count + " ===");
                        foreach (TaskItem t in overdue)
                        {
                            Console.WriteLine("   #" + t.Id + " " + t.Title +
                                              " (дедлайн был " +
                                              t.Deadline.ToString("dd.MM.yyyy HH:mm") + ")");
                        }
                        Console.WriteLine("====================================================");
                        Console.ForegroundColor = old;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                cts.Cancel();
                try
                {
                    workerTask?.Wait(2000);
                }
                catch (Exception)
                {
                    // глушим, нам важно просто закрыться
                }
                cts.Dispose();
                Console.WriteLine("[DeadlineWatcher] остановлен.");
            }
            GC.SuppressFinalize(this);
        }
    }
}