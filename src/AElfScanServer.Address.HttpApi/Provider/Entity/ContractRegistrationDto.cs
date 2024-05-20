namespace AElfScanServer.Address.HttpApi.Provider.Entity;

public class ContractRegistrationDto : GraphQLDto
{
    public string CodeHash { get; set; }
    public string Code { get; set; }
    public string ProposedContractInputHash { get; set; }
    public int ContractCategory { get; set; }
    public string ContractType { get; set; }
}