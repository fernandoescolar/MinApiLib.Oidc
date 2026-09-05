namespace IdentityServer.Events;

public class NonceIssuedSuccessEvent : Event
{
    protected NonceIssuedSuccessEvent()
      : base(EventCategories.Nonce,
          "Nonce Issued Success",
          EventTypes.Success,
          EventIds.NonceIssuedSuccess)
    {
    }

    public NonceIssuedSuccessEvent(NonceResponse nonceResponse, NonceRequestValidationResult result)
        : this()
    {
        if (result != null)
        {
            ClientId = result.TokenValidationResult?.Client?.ClientId;
            ClientName = result.TokenValidationResult?.Client?.ClientName;
            SubjectId = result.Subject?.GetSubjectId();
            SessionId = result.SessionId;
        }

        if (nonceResponse != null)
        {
            Nonce = nonceResponse.Nonce;
            ExpiresIn = nonceResponse.Lifetime;
        }
    }

    public string? ClientId { get; private set; }
    public string? ClientName { get; private set; }
    public string? SubjectId { get; private set; }
    public string? SessionId { get; private set; }
    public string? Nonce { get; private set; }
    public int ExpiresIn { get; private set; }
}
