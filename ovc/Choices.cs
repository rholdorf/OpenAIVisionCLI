namespace ovc;

public record Choices(
    int index,
    Message message,
    object logprobs,
    string finish_reason
);