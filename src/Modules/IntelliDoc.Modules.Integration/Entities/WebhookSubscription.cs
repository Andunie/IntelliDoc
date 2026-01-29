using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Integration.Entities;

public class WebhookSubscription
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public string EndpointUrl { get; set; } // Örn: https://hooks.slack.com/...
    public bool IsActive { get; set; } = true;
    public string Secret { get; set; }
}