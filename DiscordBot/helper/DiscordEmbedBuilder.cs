using Discord;

namespace Dietze.helper;

public class DiscordEmbedBuilder : EmbedBuilder
{
    public DiscordEmbedBuilder(IUser? author = null)
    {
        if (author != null) this.WithAuthor(author);
        WithColorBlue();
        WithCurrentTimestamp();
        WithFooter("Dietze_",
            "https://www.pngmart.com/files/21/Funny-PNG-Isolated-HD-Pictures.png");
    }

    public static string BlankChar { get; } = "\u200b";

    public DiscordEmbedBuilder AddBlankField(bool inline = false)
    {
        AddField(BlankChar, BlankChar, inline);
        return this;
    }

    public DiscordEmbedBuilder WithColorBlue()
    {
        this.WithColor(63, 127, 191);
        return this;
    }

    public DiscordEmbedBuilder WithColorLime()
    {
        this.WithColor(63, 191, 127);
        return this;
    }

    public DiscordEmbedBuilder WithColorPurple()
    {
        this.WithColor(127, 63, 191);
        return this;
    }

    public DiscordEmbedBuilder WithColorPink()
    {
        this.WithColor(191, 63, 127);
        return this;
    }

    public DiscordEmbedBuilder WithColorGreen()
    {
        this.WithColor(127, 191, 63);
        return this;
    }

    public DiscordEmbedBuilder WithColorYellow()
    {
        this.WithColor(191, 127, 63);
        return this;
    }

    public DiscordEmbedBuilder WithColorDark()
    {
        this.WithColor(63, 63, 63);
        return this;
    }

    public DiscordEmbedBuilder WithColorGrey()
    {
        this.WithColor(127, 127, 127);
        return this;
    }

    public DiscordEmbedBuilder WithColorLight()
    {
        this.WithColor(191, 191, 191);
        return this;
    }
}