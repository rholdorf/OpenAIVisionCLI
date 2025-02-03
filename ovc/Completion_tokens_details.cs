namespace ovc;

public record Completion_tokens_details(
    int reasoning_tokens,
    int audio_tokens,
    int accepted_prediction_tokens,
    int rejected_prediction_tokens
);