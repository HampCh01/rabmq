using System;
using System.Collections.Generic;
using System.Globalization;

namespace TestMessageSender.Models;

public class LTRecordModel
{
    public string LTRecordID { get; set; }
    public string ImportDate { get; set; }
    public string LastUpdated { get; set; }
    public string UpdatedBy { get; set; }
    public string StatusID { get; set; }
    public string ProductID { get; set; }
    public string DatasetID { get; set; }
    public string DatasetName { get; set; }
    public string IsRushProcess { get; set; }
    public ReqContentModel ReqContent { get; set; }
}

public class ReqContentModel
{
    public string AgencyID { get; set; }
    public string Order { get; set; }
    public string RecordType { get; set; }
    public string RequestingSystem { get; set; }
    public string FulfillingSystem { get; set; }
    public string SupplierID { get; set; }
    public string Agency { get; set; }
    public string RptType { get; set; }
    public string Desc { get; set; }
    public string DOL {get; set;}
    public string Street { get; set; }
    public string CrossStreet { get; set; }
    public string City { get; set; }
    public string ST { get; set; }
    public string County { get; set; }
    public string RptNo {get; set;}
    public string Precinct { get; set; }
    public string FName1 { get; set; }
    public string MName1 { get; set; }
    public string LName1 { get; set; }
    public string DOB1 {get; set;}
    public string DLNo { get; set; }
    public string DLST { get; set; }
    public string FName2 { get; set; }
    public string MName2 { get; set; }
    public string LName2 { get; set; }
    public string FName3 { get; set; }
    public string MName3 { get; set; }
    public string LName3 { get; set; }
    public string VIN { get; set; }
    public string Yr { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public string TagNo { get; set; }
    public string TagST { get; set; }
    public string InternetOnly { get; set; }
}