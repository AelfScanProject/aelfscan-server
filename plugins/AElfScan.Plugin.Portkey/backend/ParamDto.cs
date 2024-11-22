namespace Portkey.backend;

public class ManagerApproved
{
    public string Spender { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
}

public class ManagerTransfer
{
    public string To { get; set; }
    public string Memo { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
}


public class ManagerTransferFrom
{
    public string From { get; set; }
    public string To { get; set; }
    public string Memo { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
}

