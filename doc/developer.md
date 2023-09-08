# Developers / Architecture

Thread and process diagram:

![Thread and Process Diagram](./diagrams/vilark.drawio.png)

Developers who are interested in creating a C# terminal app (including
WSL2/Windows Terminal) might find this project useful as a starting point.

## Special Keys

See https://devblogs.microsoft.com/dotnet/console-readkey-improvements-in-net-7/

* (4) CTRL-C -> SIGINT

* (8) CTRL-H -> CTRL+BACKSPACE
* (0xa) CTRL-J -> CTRL+ENTER
* (0xd) CTRL-M -> CTRL+ENTER
* (0x1B) CTRL-[ and CTRL-3 -> ESCAPE

* CTRL-Q/CTRL-S are thankfully not appearing to stop the terminal (XOFF)

* CTRL-\ -> We get SIGQUIT, annoying but we override the signal

* CTRL-Z -> We get SIGTSTP (terminal stop), which we could override

## .Net / C# Annoyances

* Too much UTF-16

* Hard/unsafe to call execve() (overwrite process image).  Seriously, this can't be
  the only CLI launcher program that wants to exec a replacement (and free up
  memory instantly?)

* GetEnvironmentVariables shouldn't need a SO post to explain how to sort it.  It's
  old, non-generic klunkiness.

* Directory class is mostly worthless and not specific enough about directory naming

* Trying to gracefully handle Permission denied exceptions while using an
  iterator (yield return)?  Ugh...

Overall, however, this was a very productive experience, and it should be
extremely maintainable and hackable going forward!

