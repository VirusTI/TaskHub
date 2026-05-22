using System;

namespace TaskHub
{
    //приоритет задачи
    public enum Priority
    {
        Low,
        Medium,
        High
    }

    //статус задачи
    public enum Status
    {
        New,
        InProgress,
        Done
    }
}