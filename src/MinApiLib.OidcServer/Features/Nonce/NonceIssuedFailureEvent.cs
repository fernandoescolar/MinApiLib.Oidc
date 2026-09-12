namespace MinApiLib.OidcServer.Features.Nonce;

public class NonceIssuedFailureEvent : Event
{
    protected NonceIssuedFailureEvent()
        : base(EventCategories.Nonce,
            "Nonce Issued Failure",
            EventTypes.Failure,
            EventIds.NonceIssuedFailure)
    {
    }

    public NonceIssuedFailureEvent(NonceRequestValidationResult result)
        : this()
    {
        if (result != null)
        {
            ClientId = result.TokenValidationResult?.Client?.ClientId;
            ClientName = result.TokenValidationResult?.Client?.ClientName;
            SubjectId = result.Subject?.GetSubjectId();
            SessionId = result.SessionId;
        }
    }

    public string? ClientId { get; private set; }
    public string? ClientName { get; private set; }
    public string? SubjectId { get; private set; }
    public string? SessionId { get; private set; }
}
