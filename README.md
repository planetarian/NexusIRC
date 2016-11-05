# NexusIRC
Experimental modular/pluggable C# IRC client

This is an experiment involving creating a plugin system for C# applications
which uses remoting to run individual plugins within their own sandboxed contexts.

Plugins and the application itself are not tied together by version, such that
a new version of the Nexus core will not require recompilation of its plugins --
as long as the method signatures all match, it assumes compatibility. Any methods
with non-matching signatures are simply discarded and a warning is generated.

Plugins can be loaded and unloaded on the fly, and unhandled exceptions should cause
the individual plugin to be unloaded rather than taking down the entire application.
Communication between plugins is handled via a simple messaging system.

This was never quite fully realized, as exceptions that occur in threads
created by plugin code can often remain uncaught and in some cases can
cause the application to crash.

It remains here as a proof of concept and further work may be done in the future.

This particular implementation of the Nexus plugin system is built around an IRC bot.
It has multiple plugins for various utility and toy features, and can be transformed
into a full-fledged IRC client by simply creating a UI plugin.
