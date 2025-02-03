namespace ovc;

public record Message(
    string role,
    string content,
    object refusal
);