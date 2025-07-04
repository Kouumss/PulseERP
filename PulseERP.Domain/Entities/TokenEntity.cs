namespace PulseERP.Domain.Entities;

using Enums.Token;
using Events.ProductEvents;
using Events.RefreshTokenEvents;
using Interfaces;

/// <summary>
/// Represents a refresh token entity for authentication. Acts as an aggregate root.
/// </summary>
public sealed class TokenEntity : BaseEntity
{
    #region Properties

    /// <summary>
    /// The token string value.
    /// </summary>
    public string Token { get; private set; } = null!;

    /// <summary>
    /// Identifier of the associated user.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Expiry timestamp.
    /// </summary>
    public DateTime Expires { get; private set; }

    /// <summary>
    /// The type of token.
    /// </summary>
    public TokenType TokenType { get; private set; }

    /// <summary>
    /// Timestamp when the token was revoked, if any.
    /// </summary>
    public DateTime? Revoked { get; private set; }

    /// <summary>
    /// IP address where the token was created.
    /// </summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>
    /// User agent string where the token was created.
    /// </summary>
    public string? CreatedByUserAgent { get; private set; }

    /// <summary>
    /// Indicates if the token is currently active (not revoked and not expired).
    /// </summary>
    public bool IsCurrentlyValid => !IsExpired() && !IsRevoked();

    #endregion

    #region Fields

    private readonly IDateTimeProvider _dateTimeProvider = null!;

    #endregion

    #region Constructors

    /// <summary>
    /// Protected constructor for EF Core.
    /// </summary>
    private TokenEntity() { }

    #endregion

    #region Factory

    /// <summary>
    /// Creates a new refresh token with provided IP and user agent. Expires after <paramref name="expiresAt"/>.
    /// </summary>
    public static TokenEntity Create(
        IDateTimeProvider dateTimeProvider,
        Guid userId,
        string token,
        TokenType tokenType,
        DateTime expiresAt,
        string? createdByIp = null,
        string? createdByUserAgent = null
    )
    {
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (expiresAt <= dateTimeProvider.UtcNow)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        var tokenInstance = new TokenEntity(
            dateTimeProvider,
            userId,
            token,
            tokenType,
            expiresAt,
            createdByIp,
            createdByUserAgent
        );
        tokenInstance.SetIsActive(true);
        tokenInstance.AddDomainEvent(new RefreshTokenCreatedEvent(tokenInstance.Id));

        return tokenInstance;
    }

    #endregion

    #region Private Constructors

    private TokenEntity(
        IDateTimeProvider dateTimeProvider,
        Guid userId,
        string token,
        TokenType tokenType,
        DateTime expiresAt,
        string? createdByIp,
        string? createdByUserAgent
    )
    {
        _dateTimeProvider = dateTimeProvider;
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        TokenType = tokenType;
        Expires = expiresAt;
        CreatedByIp = createdByIp;
        CreatedByUserAgent = createdByUserAgent;
    }

    #endregion

    #region Methods

    public void Revoke(DateTime revokedAt)
    {
        if (IsRevoked())
            return;

        Revoked = revokedAt;
        MarkAsDeleted();
        AddDomainEvent(new RefreshTokenRevokedEvent(Id, revokedAt));
        MarkAsUpdated();
    }

    private bool IsRevoked() => Revoked is not null;

    private bool IsExpired() => Expires <= (_dateTimeProvider?.UtcNow ?? DateTime.UtcNow);

    #endregion
}
