using System;
using System.Collections.Generic;

namespace TaskHub
{
    // Класс который держит все задачи и умеет с ними работать
    public class TaskManager
    {
        private List<TaskItem> tasks;

        // число задач каждого приоретета
        private Dictionary<Priority, int> priorityCount;

        // замок для потокобезопасного доступа (фоновый поток тоже читает список)
        private readonly object dataLock = new object();

        public TaskManager()
        {
            tasks = new List<TaskItem>();
            priorityCount = new Dictionary<Priority, int>();
        }

        public void AddTask(TaskItem task)
        {
            lock (dataLock)
            {
                tasks.Add(task);
            }
        }

        public List<TaskItem> GetAll()
        {
            lock (dataLock)
            {
                return new List<TaskItem>(tasks);
            }
        }

        // универсальный фильтр задач
        public List<TaskItem> Filter(Predicate<TaskItem> condition)
        {
            List<TaskItem> result = new List<TaskItem>();
            lock (dataLock)
            {
                foreach (TaskItem t in tasks)
                {
                    if (condition(t))
                    {
                        result.Add(t);
                    }
                }
            }
            return result;
        }

        public List<TaskItem> GetDone()
        {
            return Filter(t => t.Status == Status.Done);
        }

        public List<TaskItem> GetNotDone()
        {
            return Filter(t => t.Status != Status.Done);
        }

        public List<TaskItem> GetHighPriority()
        {
            return Filter(t => t.Priority == Priority.High);
        }

        public List<TaskItem> GetOverdueTasks()
        {
            return Filter(t => t.IsOverdue());
        }

        public TaskItem FindById(int id)
        {
            lock (dataLock)
            {
                foreach (TaskItem t in tasks)
                {
                    if (t.Id == id)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        // поиски
        public List<TaskItem> SearchByTitle(string title)
        {
            string lower = title.ToLower();
            return Filter(t => t.Title.ToLower().Contains(lower));
        }

        public List<TaskItem> SearchByStatus(Status status)
        {
            return Filter(t => t.Status == status);
        }

        public List<TaskItem> SearchByPriority(Priority priority)
        {
            return Filter(t => t.Priority == priority);
        }

   
        public bool RemoveTask(int id)
        {
            lock (dataLock)
            {
                TaskItem found = null;
                foreach (TaskItem t in tasks)
                {
                    if (t.Id == id)
                    {
                        found = t;
                        break;
                    }
                }
                if (found != null)
                {
                    tasks.Remove(found);
                    return true;
                }
            }
            return false;
        }

        // статистика
        public TaskStats GetStats()
        {
            lock (dataLock)
            {
                TaskStats stats = new TaskStats();
                stats.Total = tasks.Count;

                priorityCount.Clear();
                priorityCount[Priority.Low] = 0;
                priorityCount[Priority.Medium] = 0;
                priorityCount[Priority.High] = 0;

                foreach (TaskItem t in tasks)
                {
                    if (t.Status == Status.Done)
                    {
                        stats.Done++;
                    }
                    if (t.IsOverdue())
                    {
                        stats.Overdue++;
                    }
                    priorityCount[t.Priority]++;
                }

                stats.LowCount = priorityCount[Priority.Low];
                stats.MediumCount = priorityCount[Priority.Medium];
                stats.HighCount = priorityCount[Priority.High];

                return stats;
            }
        }

        // при загрузке из файла - заменить весь список
        public void ReplaceAll(List<TaskItem> newTasks)
        {
            lock (dataLock)
            {
                tasks = newTasks;
            }
        }
    }

    // Структура для статистики.
    public struct TaskStats
    {
        public int Total;
        public int Done;
        public int Overdue;
        public int LowCount;
        public int MediumCount;
        public int HighCount;
    }
}