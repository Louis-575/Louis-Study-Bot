namespace LouisStudyBot.Core.Discord;

public sealed class SubjectAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services)
    {
        if (context.Guild is null)
        {
            return AutocompletionResult.FromSuccess([]);
        }

        string input = autocompleteInteraction.Data.Current.Value as string ?? string.Empty;
        IStudySessionStore store = services.GetRequiredService<IStudySessionStore>();
        IReadOnlyList<string> tags = await store.GetTagsAsync(context.Guild.Id, 25);

        IEnumerable<string> filtered = tags;
        if (!string.IsNullOrWhiteSpace(input))
        {
            filtered = filtered.Where(tag => tag.Contains(input, StringComparison.OrdinalIgnoreCase));
        }

        return AutocompletionResult.FromSuccess(
            filtered
                .Take(25)
                .Select(tag => new AutocompleteResult(tag, tag)));
    }
}
