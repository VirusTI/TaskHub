using System;

namespace TaskHub
{
    // Абстрактный базовый класс задач
    public abstract class BaseTask
    {
        // статический счётчик для id
        private static int counter = 0;

        // lock для потокобезопасного увеличения счётчика
        private static readonly object idLock = new object();

        public int Id { get; private set; }

        protected BaseTask()
        {
            lock (idLock)
            {
                counter++;
                this.Id = counter;
            }
        }

        public abstract string GetInfo();

        //чтобы при загрузке из файла восстановить старый id
        public void RestoreId(int id)
        {
            this.Id = id;
            lock (idLock)
            {
                if (id > counter)
                {
                    counter = id;
                }
            }
        }
    }

    // класс задачи
    public class TaskItem : BaseTask
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Priority Priority { get; set; }
        public Status Status { get; set; }
        public DateTime Deadline { get; set; }

        public TaskItem()
        {
            this.Title = "";
            this.Description = "";
            this.Priority = Priority.Low;
            this.Status = Status.New;
            this.Deadline = DateTime.Now;
        }

        // основной конструктор
        public TaskItem(string title, string description, Priority priority,
                        Status status, DateTime deadline)
        {
            this.Title = title;
            this.Description = description;
            this.Priority = priority;
            this.Status = status;
            this.Deadline = deadline;
        }

        //проверка просрочена ли задача
        public bool IsOverdue()
        {
            if (this.Status != Status.Done && this.Deadline < DateTime.Now)
            {
                return true;
            }
            return false;
        }

        public override string GetInfo()
        {
            string overdueMark = IsOverdue() ? "  [Просрочена!]" : "";
            return string.Format(
                "#{0} | {1} | Приоритет: {2} | Статус: {3} | Дедлайн: {4}{5}{6}     Описание: {7}",
                Id, Title, Priority, Status,
                Deadline.ToString("dd.MM.yyyy HH:mm"), overdueMark,
                Environment.NewLine, Description);
        }

        // сохранение в файл с разделителем ;
        public string ToFileLine()
        {
            return Id + ";" + Title + ";" + Description + ";" +
                   (int)Priority + ";" + (int)Status + ";" + Deadline.Ticks;
        }

        // создание задачи из строки файла
        public static TaskItem FromFileLine(string line)
        {
            string[] parts = line.Split(';');
            if (parts.Length < 6)
            {
                throw new FormatException("Строка файла повреждена: " + line);
            }

            TaskItem t = new TaskItem();
            t.RestoreId(int.Parse(parts[0]));
            t.Title = parts[1];
            t.Description = parts[2];
            t.Priority = (Priority)int.Parse(parts[3]);
            t.Status = (Status)int.Parse(parts[4]);
            t.Deadline = new DateTime(long.Parse(parts[5]));
            return t;
        }
    }
}