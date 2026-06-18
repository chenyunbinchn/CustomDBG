namespace enums
{
    public enum EnumActionStatus
    {
        None = 0,
        WaitingForExecution,
        Executing,
        GatheringPlayerChoice,
        ReadyToResumeExecuting,
        Finished,
        Canceled
    }
}