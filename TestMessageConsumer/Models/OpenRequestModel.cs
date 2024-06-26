namespace TestMessageConsumer.Models;
class OpenRequestModel{
    public int LeadListRequestID { get; set; }
    public int RequestID { get; set; }
    public int CourtID { get; set; }
    public string? FileTypeID { get; set; }
    public string? ReqContent { get; set; }
}