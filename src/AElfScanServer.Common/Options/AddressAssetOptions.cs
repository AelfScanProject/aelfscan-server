using AElfScanServer.Common.Enums;


namespace AElfScanServer.Common.Options;

public class AddressAssetOptions
{
    public int DailyExpireSeconds { get; set; } = 7 * 24 * 3600;
    
    public int CurrentExpireSeconds { get; set; } = 5 * 60;


    public int GetExpireSeconds(AddressAssetType type)
    {
        if (type == AddressAssetType.Current)
        {
            return CurrentExpireSeconds;
        }
        return DailyExpireSeconds;
    }
}