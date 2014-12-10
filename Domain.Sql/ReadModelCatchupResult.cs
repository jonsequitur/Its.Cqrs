namespace Microsoft.Its.Domain.Sql
{
    public enum ReadModelCatchupResult
    {
        CatchupAlreadyInProgress = 0,
        CatchupRanButNoNewEvents = 1,
        CatchupRanAndHandledNewEvents = 2
    }
}