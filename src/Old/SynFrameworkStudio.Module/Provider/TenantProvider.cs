using DevExpress.ExpressApp.MultiTenancy;
using DevExpress.ExpressApp.MultiTenancy.Internal;
using Microsoft.Extensions.DependencyInjection;

public class TenantProvider : ITenantProvider
{
    private IServiceProvider serviceProvider;
    public TenantProvider(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    public Guid? TenantId { get; set; }
    public string TenantName
    {
        get
        {
            Guid? tenantId = TenantId;
            if (tenantId == null)
            {
                return null;
            }
            var tenantNameHelper = serviceProvider.GetRequiredService<ITenantNameHelper>();
            return tenantNameHelper.GetTenantNameById(tenantId.Value);
        }
    }
    public object TenantObject
    {
        get
        {
            Guid? tenantId = TenantId;
            if (tenantId == null)
            {
                return null;
            }
            var tenantNameHelper = serviceProvider.GetRequiredService<ITenantNameHelper>();
            return tenantNameHelper.GetTenantById(tenantId.Value);
        }
    }
}