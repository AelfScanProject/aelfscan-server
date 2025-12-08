using System;
using System.Collections.Generic;
using System.Text;
using AElfScanServer.Domain.Shared.Localization;
using Volo.Abp.Application.Services;

namespace AElfScanServer;

/* Inherit your application services from this class.
 */
public abstract class AElfScanServerAppService : ApplicationService
{
    protected AElfScanServerAppService()
    {
        LocalizationResource = typeof(AElfScanServerResource);
    }
}
