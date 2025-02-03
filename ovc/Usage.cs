namespace ovc;

public record Usage(
    int prompt_tokens,
    int completion_tokens,
    int total_tokens,
    Prompt_tokens_details prompt_tokens_details,
    Completion_tokens_details completion_tokens_details
);