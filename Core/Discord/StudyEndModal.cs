namespace LouisStudyBot.Core.Discord;

public sealed class StudyEndModal : IModal
{
    public string Title => "End study session";

    [InputLabel("What did you work on?")]
    [ModalTextInput("summary", TextInputStyle.Paragraph, "Example: Finished vectors worksheet and reviewed mistakes.", maxLength: 1000)]
    public string Summary { get; set; } = string.Empty;

    [InputLabel("Subject tag")]
    [ModalTextInput("tag", TextInputStyle.Short, "Example: Maths", maxLength: 50)]
    public string Tag { get; set; } = string.Empty;
}
