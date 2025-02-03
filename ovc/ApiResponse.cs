namespace ovc;

public record ApiResponse(
    string id,
    string @object,
    int created,
    string model,
    Choices[] choices,
    Usage usage,
    string service_tier,
    string system_fingerprint
);