G&W HLL Discord Voting Tool Version 0.27.0a
For HLL Update 17 Release "Tobruk"
Made by Github @iamajaegerdev for Esprit De Corps' Garrisons & Whiskey server. Released to the public for free use.

~~install~~
Drag the .exe into unzipped Public GW Discord Voting Tool folder. Configure your appsettings.json to correctly target role IDs. You will need Developer mode enabled on Discord to do this.

Use Discord provided install link

Required Scopes:
bot
applications.commands

Required Bot Permissions:
General Permissions/Administrator

~~configuration~~
see appsettings.json for configurable settings
custom command triggers configured in appsettings.json is currently disabled.

bot can only be triggered by the bot owner.

oauth2 functionality is hardcoded to off to prevent accidental use of the feature.
oauth2 is not required for the bot to function.

~~help~~
current command trigger syntax is:

must be in guildid (no dms)
tag the bot or use ! for prefix trigger

ping
	pong (for testing)

installslashinteractions
	install slash commands for the bot

mapvote {remainder}
	if appsettings.json AppendRemainderToChannel is true, the remainder will be appended to the channel name
	    @botname mapvote test
	        creates channel named "map-vote-test"

	if appsettings.json AppendRemainderToChannel is false, the remainder will be ignored
	    @botname mapvote test
	        creates channel named "map-vote"

	if appsettings.json AutoAppendDateToChannel is true, the two weeks date will be appended to the channel name. AppendRemainderToChannel must be true for this to work.
	    @botname mapvote
			creates channel named "map-vote-03-25-to-04-08"
        @botname mapvote test
		    creates channel named "map-vote-test-03-25-to-04-08"

votetally
	use in the map-vote channel