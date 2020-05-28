**Beat Saber Discord Event Plugin**

This is a Plugin/Server combo project which handles a Discord Bot for the purpose of reporting info about a Beat Saber competition or event. This system has been used for: "Beat Saber Discord Server"'s Weekly Event, Asia VR Community's Monthly Event, TeamSaber, Beat the Hub Season 1, Beat Saber World Cup, and a heavily modified version was used to record votes for the BSMG's Christmas Event in 2018.

I am continually working on minor improvements for this system, which in the past have included: moving away from Protobuf in favor of serialized classes and generalizing the plugin and its references so that it may be easily repurposed for different events/servers.

There is much to do, much yet to be done, and much that will likely be left undone.
At the time of writing, this repo is not intended to be public, but it may become public at a later date.

Update, 5/27/20:
I now believe this system has become so monolithic and specialized that it needs a rewrite from the ground up. I'm making this public now, and will probably continue to reimplement the features this project provides inside of TournamentAssistant.
