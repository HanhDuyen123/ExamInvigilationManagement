using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ExamInvigilationManagement.Common.Helpers
{
    public static class TempDataExtensions
    {
        public static void SetNotification(this ITempDataDictionary tempData, string type, string message)
        {
            tempData["Notify.Type"] = type;
            tempData["Notify.Message"] = message;
        }
    }
}
