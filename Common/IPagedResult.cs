namespace ExamInvigilationManagement.Common
{
    public interface IPagedResult
    {
        int Page { get; }
        int TotalPages { get; }
    }
}
