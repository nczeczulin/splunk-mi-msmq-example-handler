Example Message Handler for the Splunk MSMQ Modular Input
=========================================================


### Requirements

* Visual Studio 2012


After placing the compiled dll in the $SPLUNK_HOME/etc/apps/SA-ModularInput-MSMQ/bin/MessageHandlers directory,
it can be referenced in the configuration as "ExampleHandler.TextBodyHandler"

The example handler accepts two optional "handler_args":

* encoding
  * e.g., utf-8, utf-16
* escape_mode
  * None
      * Body text will not be altered.
  * AutoEscaped
      * See KV_MODE = auto_escaped (http://docs.splunk.com/Documentation/Splunk/latest/admin/Propsconf)


