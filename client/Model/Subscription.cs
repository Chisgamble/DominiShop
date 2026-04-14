using System;
using System.Collections.Generic;

namespace DominiShop.Model;

public partial class Subscription
{
    public long Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public string Claims { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? ActionFilter { get; set; }
}
