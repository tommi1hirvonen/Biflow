namespace EtlManagerExecutor
{
    interface ISchedulesExecutor
    {
        void Run(int hours, int minutes);
    }
}