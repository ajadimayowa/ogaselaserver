namespace Ogasela.Domain.Payments;

/// <summary>One per seller, created lazily the first time it's needed (funding, a plan purchase, or a balance check).</summary>
public class Wallet
{
    private Wallet()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public decimal BalanceKobo { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Wallet Create(Guid sellerId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        SellerId = sellerId,
        BalanceKobo = 0m,
        UpdatedAt = now
    };

    public void Credit(decimal amountKobo, DateTime now)
    {
        BalanceKobo += amountKobo;
        UpdatedAt = now;
    }

    /// <summary>Throws if the wallet doesn't have enough balance - callers should check <see cref="HasSufficientBalance"/> first.</summary>
    public void Debit(decimal amountKobo, DateTime now)
    {
        if (!HasSufficientBalance(amountKobo))
        {
            throw new InvalidOperationException("Insufficient wallet balance.");
        }

        BalanceKobo -= amountKobo;
        UpdatedAt = now;
    }

    public bool HasSufficientBalance(decimal amountKobo) => BalanceKobo >= amountKobo;
}
