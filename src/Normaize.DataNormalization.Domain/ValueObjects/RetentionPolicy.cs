namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing data retention policy for datasets.
/// </summary>
public sealed class RetentionPolicy
{
    public int RetentionDays { get; }
    public DateTime ExpiryDate { get; }
    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    public TimeSpan RemainingTime => ExpiryDate - DateTime.UtcNow;

    private RetentionPolicy(int retentionDays, DateTime expiryDate)
    {
        RetentionDays = retentionDays;
        ExpiryDate = expiryDate;
    }

    public static RetentionPolicy Create(int retentionDays)
    {
        if (retentionDays <= 0)
            throw new ArgumentException("Retention days must be positive", nameof(retentionDays));

        var expiryDate = DateTime.UtcNow.AddDays(retentionDays);
        return new RetentionPolicy(retentionDays, expiryDate);
    }

    public static RetentionPolicy CreateWithExpiryDate(DateTime expiryDate)
    {
        if (expiryDate <= DateTime.UtcNow)
            throw new ArgumentException("Expiry date must be in the future", nameof(expiryDate));

        var retentionDays = (int)(expiryDate - DateTime.UtcNow).TotalDays;
        return new RetentionPolicy(retentionDays, expiryDate);
    }

    public static RetentionPolicy Default() => Create(365); // 1 year default

    public RetentionPolicy Extend(int additionalDays)
    {
        if (additionalDays <= 0)
            throw new ArgumentException("Additional days must be positive", nameof(additionalDays));

        var newExpiryDate = ExpiryDate.AddDays(additionalDays);
        var newRetentionDays = RetentionDays + additionalDays;
        return new RetentionPolicy(newRetentionDays, newExpiryDate);
    }

    public RetentionPolicy Update(int newRetentionDays)
    {
        return Create(newRetentionDays);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not RetentionPolicy other)
            return false;

        return RetentionDays == other.RetentionDays &&
               ExpiryDate == other.ExpiryDate;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RetentionDays, ExpiryDate);
    }
}
