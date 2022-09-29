using Discord;

namespace Develeon64.RoboSushi.Util;

public class DiscordEmbedBuilder : EmbedBuilder {
	public DiscordEmbedBuilder (IUser? author = null) {
		if (author != null) this.WithAuthor(author);
		this.WithColorBlue();
		this.WithCurrentTimestamp();
		this.WithFooter("Develeon64", "https://cdn.discordapp.com/attachments/344276509567090689/975281528173121606/Chrishi_Blurple-Dark.png");
	}

	public DiscordEmbedBuilder AddBlankField (bool inline = false) {
		this.AddField("\u200d", "\u200d", inline);
		return this;
	}

	public DiscordEmbedBuilder WithColorBlue () {
		this.WithColor(63, 127, 191);
		return this;
	}

	public DiscordEmbedBuilder WithColorLime () {
		this.WithColor(63, 191, 127);
		return this;
	}

	public DiscordEmbedBuilder WithColorPurple () {
		this.WithColor(127, 63, 191);
		return this;
	}

	public DiscordEmbedBuilder WithColorPink () {
		this.WithColor(191, 63, 127);
		return this;
	}

	public DiscordEmbedBuilder WithColorGreen () {
		this.WithColor(127, 191, 63);
		return this;
	}

	public DiscordEmbedBuilder WithColorYellow () {
		this.WithColor(191, 127, 63);
		return this;
	}

	public DiscordEmbedBuilder WithColorDark () {
		this.WithColor(63, 63, 63);
		return this;
	}

	public DiscordEmbedBuilder WithColorGrey () {
		this.WithColor(127, 127, 127);
		return this;
	}

	public DiscordEmbedBuilder WithColorLight () {
		this.WithColor(191, 191, 191);
		return this;
	}
}
