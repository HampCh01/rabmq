using TestMessageSender.Models;

namespace TestMessageSender.Models;
public class OpenRequestModel{
    public int LeadListRequestID { get; set; }
    public int RequestID { get; set; }
    public int CourtID { get; set; }
    public string FileTypeID { get; set; }
    public LTRecordModel ReqContent { get; set; }
}